using System;

using java.io;
using java.lang;
using java.math;
using java.net;
using java.nio;
using java.text;
using java.util;

using RSUtilities.Cryptography;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     A <seealso cref="ChecksumTable" /> stores checksums and versions of
    ///     <seealso cref="ReferenceTable" />s. When encoded in a <seealso cref="Container" /> and prepended
    ///     with the file type and id it is more commonly known as the client's
    ///     "update keys".
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public class ChecksumTable
    {
        /// <summary>
        ///     The entries in this table.
        /// </summary>
        private Entry[] entries;

        /// <summary>
        ///     Creates a new <seealso cref="ChecksumTable" /> with the specified size.
        /// </summary>
        /// <param name="size"> The number of entries in this table. </param>
        public ChecksumTable(int size)
        {
            entries = new Entry[size];
        }

        /// <summary>
        ///     Decodes the <seealso cref="ChecksumTable" /> in the specified
        ///     <seealso cref="ByteBuffer" />. Whirlpool digests are not read.
        /// </summary>
        /// <param name="buffer"> The <seealso cref="ByteBuffer" /> containing the table. </param>
        /// <returns> The decoded <seealso cref="ChecksumTable" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static ChecksumTable Decode(ByteBuffer buffer)
        {
            return Decode(buffer, false);
        }

        /// <summary>
        ///     Decodes the <seealso cref="ChecksumTable" /> in the specified
        ///     <seealso cref="ByteBuffer" />.
        /// </summary>
        /// <param name="buffer"> The <seealso cref="ByteBuffer" /> containing the table. </param>
        /// <param name="whirlpool"> If whirlpool digests should be read. </param>
        /// <returns> The decoded <seealso cref="ChecksumTable" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static ChecksumTable Decode(ByteBuffer buffer, bool whirlpool)
        {
            return Decode(buffer, whirlpool, null, null);
        }

        /// <summary>
        ///     Decodes the <seealso cref="ChecksumTable" /> in the specified
        ///     <seealso cref="ByteBuffer" /> and decrypts the final whirlpool hash.
        /// </summary>
        /// <param name="buffer"> The <seealso cref="ByteBuffer" /> containing the table. </param>
        /// <param name="whirlpool"> If whirlpool digests should be read. </param>
        /// <param name="modulus"> The modulus. </param>
        /// <param name="publicKey"> The public key. </param>
        /// <returns> The decoded <seealso cref="ChecksumTable" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static ChecksumTable Decode(ByteBuffer buffer, bool whirlpool, BigInteger modulus, BigInteger publicKey)
        {
            /* find out how many entries there are and allocate a new table */
            var size = whirlpool ? (buffer.limit() / 8) : (buffer.get() & 0xFF);
            var table = new ChecksumTable(size);

            /* calculate the whirlpool digest we expect to have at the end */
            byte[] masterDigest = null;
            if (whirlpool)
            {
                var temp = new byte[size * 72 + 1];
                buffer.position(0);
                buffer.get(temp);
                masterDigest = Whirlpool.Crypt(temp, 0, temp.Length);
            }

            /* read the entries */
            buffer.position(1);
            for (var i = 0; i < size; i++)
            {
                var crc = buffer.getInt();
                var version = buffer.getInt();
                var digest = new byte[64];
                if (whirlpool)
                {
                    buffer.get(digest);
                }
                table.entries[i] = new Entry(crc, version, digest);
            }

            /* read the trailing digest and check if it matches up */
            if (whirlpool)
            {
                var bytes = new byte[buffer.remaining()];
                buffer.get(bytes);
                var temp = ByteBuffer.wrap(bytes);

                if (modulus != null && publicKey != null)
                {
                    temp = Rsa.Crypt(buffer, modulus, publicKey);
                }

                if (temp.limit() != 66)
                {
                    throw new IOException("Decrypted data is not 66 bytes long");
                }

                for (var i = 0; i < 64; i++)
                {
                    if (temp.get(i + 1) != masterDigest[i])
                    {
                        throw new IOException("Whirlpool digest mismatch");
                    }
                }
            }

            /* if it looks good return the table */
            return table;
        }

        /// <summary>
        ///     Encodes this <seealso cref="ChecksumTable" />. Whirlpool digests are not encoded.
        /// </summary>
        /// <returns> The encoded <seealso cref="ByteBuffer" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public virtual ByteBuffer Encode()
        {
            return Encode(false);
        }

        /// <summary>
        ///     Encodes this <seealso cref="ChecksumTable" />.
        /// </summary>
        /// <param name="whirlpool"> If whirlpool digests should be encoded. </param>
        /// <returns> The encoded <seealso cref="ByteBuffer" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public virtual ByteBuffer Encode(bool whirlpool)
        {
            return Encode(whirlpool, null, null);
        }

        /// <summary>
        ///     Encodes this <seealso cref="ChecksumTable" /> and encrypts the final whirlpool hash.
        /// </summary>
        /// <param name="whirlpool"> If whirlpool digests should be encoded. </param>
        /// <param name="modulus"> The modulus. </param>
        /// <param name="privateKey"> The private key. </param>
        /// <returns> The encoded <seealso cref="ByteBuffer" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public virtual ByteBuffer Encode(bool whirlpool, BigInteger modulus, BigInteger privateKey)
        {
            var bout = new ByteArrayOutputStream();
            var os = new DataOutputStream(bout);
            try
            {
                /* as the new whirlpool format is more complicated we must write the number of entries */
                if (whirlpool)
                {
                    os.write(entries.Length);
                }

                /* encode the individual entries */
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    os.writeInt(entry.GetCrc());
                    os.writeInt(entry.GetVersion());
                    if (whirlpool)
                    {
                        os.write(entry.GetWhirlpool());
                    }
                }

                /* compute (and encrypt) the digest of the whole table */
                byte[] bytes;
                if (whirlpool)
                {
                    bytes = bout.toByteArray();
                    var temp = ByteBuffer.allocate(66);
                    temp.put(0);
                    temp.put(Whirlpool.Crypt(bytes, 5, bytes.Length - 5));
                    temp.put(0);
                    temp.flip();

                    if (modulus != null && privateKey != null)
                    {
                        temp = Rsa.Crypt(temp, modulus, privateKey);
                    }

                    bytes = new byte[temp.limit()];
                    temp.get(bytes);
                    os.write(bytes);
                }

                bytes = bout.toByteArray();
                return ByteBuffer.wrap(bytes);
            }
            finally
            {
                os.close();
            }
        }

        /// <summary>
        ///     Gets the size of this table.
        /// </summary>
        /// <returns> The size of this table. </returns>
        public virtual int GetSize()
        {
            return entries.Length;
        }

        /// <summary>
        ///     Sets an entry in this table.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <param name="entry"> The entry. </param>
        /// <exception cref="IndexOutOfBoundsException">
        ///     if the id is less than zero or greater
        ///     than or equal to the size of the table.
        /// </exception>
        public virtual void SetEntry(int id, Entry entry)
        {
            if (id < 0 || id >= entries.Length)
            {
                throw new IndexOutOfRangeException();
            }
            entries[id] = entry;
        }

        /// <summary>
        ///     Gets an entry from this table.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <returns> The entry. </returns>
        /// <exception cref="IndexOutOfBoundsException">
        ///     if the id is less than zero or greater
        ///     than or equal to the size of the table.
        /// </exception>
        public virtual Entry GetEntry(int id)
        {
            if (id < 0 || id >= entries.Length)
            {
                throw new IndexOutOfRangeException();
            }
            return entries[id];
        }

        /// <summary>
        ///     Represents a single entry in a <seealso cref="ChecksumTable" />. Each entry
        ///     contains a CRC32 checksum and version of the corresponding
        ///     <seealso cref="ReferenceTable" />.
        ///     @author Graham Edgecombe
        /// </summary>
        public class Entry
        {
            /// <summary>
            ///     The CRC32 checksum of the reference table.
            /// </summary>
            internal readonly int crc;

            /// <summary>
            ///     The version of the reference table.
            /// </summary>
            internal readonly int version;

            /// <summary>
            ///     The whirlpool digest of the reference table.
            /// </summary>
            internal readonly byte[] whirlpool;

            /// <summary>
            ///     Creates a new entry.
            /// </summary>
            /// <param name="crc"> The CRC32 checksum of the slave table. </param>
            /// <param name="version"> The version of the slave table. </param>
            /// <param name="whirlpool"> The whirlpool digest of the reference table. </param>
            public Entry(int crc, int version, byte[] whirlpool)
            {
                if (whirlpool.Length != 64)
                {
                    throw new ArgumentException();
                }

                this.crc = crc;
                this.version = version;
                this.whirlpool = whirlpool;
            }

            /// <summary>
            ///     Gets the CRC32 checksum of the reference table.
            /// </summary>
            /// <returns> The CRC32 checksum. </returns>
            public virtual int GetCrc()
            {
                return crc;
            }

            /// <summary>
            ///     Gets the version of the reference table.
            /// </summary>
            /// <returns> The version. </returns>
            public virtual int GetVersion()
            {
                return version;
            }

            /// <summary>
            ///     Gets the whirlpool digest of the reference table.
            /// </summary>
            /// <returns> The whirlpool digest. </returns>
            public virtual byte[] GetWhirlpool()
            {
                return whirlpool;
            }
        }
    }
}
