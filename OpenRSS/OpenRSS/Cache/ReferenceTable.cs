using System;
using System.Collections.Generic;
using System.Linq;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using net.openrs.util.crypto;

using OpenRSS.JavaAPI;

namespace net.openrs.cache
{
    /// <summary>
    ///     A <seealso cref="ReferenceTable" /> holds details for all the files with a single
    ///     type, such as checksums, versions and archive members. There are also
    ///     optional fields for identifier hashes and whirlpool digests.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public class ReferenceTable
    {
        /// <summary>
        ///     A flag which indicates this <seealso cref="ReferenceTable" /> contains
        ///     <seealso cref="Djb2" /> hashed identifiers.
        /// </summary>
        public const int FLAG_IDENTIFIERS = 0x01;

        /// <summary>
        ///     A flag which indicates this <seealso cref="ReferenceTable" /> contains
        ///     whirlpool digests for its entries.
        /// </summary>
        public const int FLAG_WHIRLPOOL = 0x02;

        /// <summary>
        ///     The entries in this table.
        /// </summary>
        private SortedDictionary<int?, Entry> entries = new SortedDictionary<int?, Entry>();

        /// <summary>
        ///     The flags of this table.
        /// </summary>
        private int flags;

        /// <summary>
        ///     The format of this table.
        /// </summary>
        private int format;

        /// <summary>
        ///     The version of this table.
        /// </summary>
        private int version;

        /// <summary>
        ///     Decodes the slave checksum table contained in the specified
        ///     <seealso cref="ByteBuffer" />.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <returns> The slave checksum table. </returns>
        public static ReferenceTable Decode(ByteBuffer buffer)
        {
            /* create a new table */
            var table = new ReferenceTable();

            /* read header */
            table.format = buffer.get() & 0xFF;
            if (table.format >= 6)
            {
                table.version = buffer.getInt();
            }
            table.flags = buffer.get() & 0xFF;

            /* read the ids */
            var ids = new int[buffer.getShort() & 0xFFFF];
            int accumulator = 0,
                size = -1;
            for (var i = 0; i < ids.Length; i++)
            {
                var delta = buffer.getShort() & 0xFFFF;
                ids[i] = accumulator += delta;
                if (ids[i] > size)
                {
                    size = ids[i];
                }
            }
            size++;

            /* and allocate specific entries within that array */
            foreach (var id in ids)
            {
                table.entries[id] = new Entry();
            }

            /* read the identifiers if present */
            if ((table.flags & FLAG_IDENTIFIERS) != 0)
            {
                foreach (var id in ids)
                {
                    table.entries[id]
                         .identifier = buffer.getInt();
                }
            }

            /* read the CRC32 checksums */
            foreach (var id in ids)
            {
                table.entries[id]
                     .crc = buffer.getInt();
            }

            /* read the whirlpool digests if present */
            if ((table.flags & FLAG_WHIRLPOOL) != 0)
            {
                foreach (var id in ids)
                {
                    buffer.get(table.entries[id]
                                    .whirlpool);
                }
            }

            /* read the version numbers */
            foreach (var id in ids)
            {
                table.entries[id]
                     .version = buffer.getInt();
            }

            /* read the child sizes */
            var members = new int[size][];
            foreach (var id in ids)
            {
                members[id] = new int[buffer.getShort() & 0xFFFF];
            }

            /* read the child ids */
            foreach (var id in ids)
            {
                /* reset the accumulator and size */
                accumulator = 0;
                size = -1;

                /* loop through the array of ids */
                for (var i = 0; i < members[id].Length; i++)
                {
                    var delta = buffer.getShort() & 0xFFFF;
                    members[id][i] = accumulator += delta;
                    if (members[id][i] > size)
                    {
                        size = members[id][i];
                    }
                }
                size++;

                /* and allocate specific entries within the array */
                foreach (var child in members[id])
                {
                    table.entries[id]
                         .entries.Add(child, new ChildEntry());
                }
            }

            /* read the child identifiers if present */
            if ((table.flags & FLAG_IDENTIFIERS) != 0)
            {
                foreach (var id in ids)
                {
                    foreach (var child in members[id])
                    {
                        table.entries[id]
                             .entries[child]
                             .identifier = buffer.getInt();
                    }
                }
            }

            /* return the table we constructed */
            return table;
        }

        /// <summary>
        ///     Gets the format of this table.
        /// </summary>
        /// <returns> The format. </returns>
        public virtual int GetFormat()
        {
            return format;
        }

        /// <summary>
        ///     Sets the format of this table.
        /// </summary>
        /// <param name="format"> The format. </param>
        public virtual void SetFormat(int format)
        {
            this.format = format;
        }

        /// <summary>
        ///     Gets the version of this table.
        /// </summary>
        /// <returns> The version of this table. </returns>
        public virtual int GetVersion()
        {
            return version;
        }

        /// <summary>
        ///     Sets the version of this table.
        /// </summary>
        /// <param name="version"> The version. </param>
        public virtual void SetVersion(int version)
        {
            this.version = version;
        }

        /// <summary>
        ///     Gets the flags of this table.
        /// </summary>
        /// <returns> The flags. </returns>
        public virtual int GetFlags()
        {
            return flags;
        }

        /// <summary>
        ///     Sets the flags of this table.
        /// </summary>
        /// <param name="flags"> The flags. </param>
        public virtual void SetFlags(int flags)
        {
            this.flags = flags;
        }

        /// <summary>
        ///     Gets the entry with the specified id, or {@code null} if it does not
        ///     exist.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <returns> The entry. </returns>
        public virtual Entry GetEntry(int id)
        {
            return entries[id];
        }

        /// <summary>
        ///     Gets the child entry with the specified id, or {@code null} if it does
        ///     not exist.
        /// </summary>
        /// <param name="id"> The parent id. </param>
        /// <param name="child"> The child id. </param>
        /// <returns> The entry. </returns>
        public virtual ChildEntry GetEntry(int id, int child)
        {
            Entry entry = entries[id];
            if (entry == null)
            {
                return null;
            }

            return entry.GetEntry(child);
        }

        /// <summary>
        ///     Replaces or inserts the entry with the specified id.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <param name="entry"> The entry. </param>
        public virtual void PutEntry(int id, Entry entry)
        {
            entries.Add(id, entry);
        }

        /// <summary>
        ///     Removes the entry with the specified id.
        /// </summary>
        /// <param name="id"> The id. </param>
        public virtual void RemoveEntry(int id)
        {
            entries.Remove(id);
        }

        /// <summary>
        ///     Gets the number of actual entries.
        /// </summary>
        /// <returns> The number of actual entries. </returns>
        public virtual int Size()
        {
            return entries.Count;
        }

        /// <summary>
        ///     Gets the maximum number of entries in this table.
        /// </summary>
        /// <returns> The maximum number of entries. </returns>
        public virtual int Capacity()
        {
            if (entries.Count == 0)
            {
                return 0;
            }

            return entries.Keys.Last().Value + 1;
        }

        /// <summary>
        ///     Encodes this <seealso cref="ReferenceTable" /> into a <seealso cref="ByteBuffer" />.
        /// </summary>
        /// <returns> The <seealso cref="ByteBuffer" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public java.nio.ByteBuffer encode() throws java.io.IOException
        public virtual ByteBuffer Encode()
        {
            /* 
             * we can't (easily) predict the size ahead of time, so we write to a
             * stream and then to the buffer
             */
            var bout = new ByteArrayOutputStream();
            var os = new DataOutputStream(bout);
            try
            {
                /* write the header */
                os.write(format);
                if (format >= 6)
                {
                    os.writeInt(version);
                }
                os.write(flags);

                /* calculate and write the number of non-null entries */
                os.writeShort(entries.Count);

                /* write the ids */
                var last = 0;
                for (var id = 0; id < Capacity(); id++)
                {
                    if (entries.ContainsKey(id))
                    {
                        var delta = id - last;
                        os.writeShort(delta);
                        last = id;
                    }
                }

                /* write the identifiers if required */
                if ((flags & FLAG_IDENTIFIERS) != 0)
                {
                    foreach (Entry entry in entries.Values)
                    {
                        os.writeInt(entry.identifier);
                    }
                }

                /* write the CRC checksums */
                foreach (Entry entry in entries.Values)
                {
                    os.writeInt(entry.crc);
                }

                /* write the whirlpool digests if required */
                if ((flags & FLAG_WHIRLPOOL) != 0)
                {
                    foreach (Entry entry in entries.Values)
                    {
                        os.write(entry.whirlpool);
                    }
                }

                /* write the versions */
                foreach (Entry entry in entries.Values)
                {
                    os.writeInt(entry.version);
                }

                /* calculate and write the number of non-null child entries */
                foreach (Entry entry in entries.Values)
                {
                    os.writeShort(entry.entries.Count);
                }

                /* write the child ids */
                foreach (Entry entry in entries.Values)
                {
                    last = 0;
                    for (var id = 0; id < entry.Capacity(); id++)
                    {
                        if (entry.entries.ContainsKey(id))
                        {
                            var delta = id - last;
                            os.writeShort(delta);
                            last = id;
                        }
                    }
                }

                /* write the child identifiers if required  */
                if ((flags & FLAG_IDENTIFIERS) != 0)
                {
                    foreach (Entry entry in entries.Values)
                    {
                        foreach (ChildEntry child in entry.entries.Values)
                        {
                            os.writeInt(child.identifier);
                        }
                    }
                }

                /* convert the stream to a byte array and then wrap a buffer */
                byte[] bytes = bout.toByteArray();
                return ByteBuffer.wrap(bytes);
            }
            finally
            {
                os.close();
            }
        }

        /// <summary>
        ///     Represents a child entry within an <seealso cref="Entry" /> in the
        ///     <seealso cref="ReferenceTable" />.
        ///     @author Graham Edgecombe
        /// </summary>
        public class ChildEntry
        {
            /// <summary>
            ///     This entry's identifier.
            /// </summary>
            internal int identifier = -1;

            /// <summary>
            ///     Gets the identifier of this entry.
            /// </summary>
            /// <returns> The identifier. </returns>
            public virtual int GetIdentifier()
            {
                return identifier;
            }

            /// <summary>
            ///     Sets the identifier of this entry.
            /// </summary>
            /// <param name="identifier"> The identifier. </param>
            public virtual void SetIdentifier(int identifier)
            {
                this.identifier = identifier;
            }
        }

        /// <summary>
        ///     Represents a single entry within a <seealso cref="ReferenceTable" />.
        ///     @author Graham Edgecombe
        /// </summary>
        public class Entry
        {
            /// <summary>
            ///     The CRC32 checksum of this entry.
            /// </summary>
            internal int crc;

            /// <summary>
            ///     The children in this entry.
            /// </summary>
            internal SortedDictionary<int?, ChildEntry> entries = new SortedDictionary<int?, ChildEntry>();

            /// <summary>
            ///     The identifier of this entry.
            /// </summary>
            internal int identifier = -1;

            /// <summary>
            ///     The version of this entry.
            /// </summary>
            internal int version;

            /// <summary>
            ///     The whirlpool digest of this entry.
            /// </summary>
            internal byte[] whirlpool = new byte[64];

            /// <summary>
            ///     Gets the identifier of this entry.
            /// </summary>
            /// <returns> The identifier. </returns>
            public virtual int GetIdentifier()
            {
                return identifier;
            }

            /// <summary>
            ///     Sets the identifier of this entry.
            /// </summary>
            /// <param name="identifier"> The identifier. </param>
            public virtual void SetIdentifier(int identifier)
            {
                this.identifier = identifier;
            }

            /// <summary>
            ///     Gets the CRC32 checksum of this entry.
            /// </summary>
            /// <returns> The CRC32 checksum. </returns>
            public virtual int GetCrc()
            {
                return crc;
            }

            /// <summary>
            ///     Sets the CRC32 checksum of this entry.
            /// </summary>
            /// <param name="crc"> The CRC32 checksum. </param>
            public virtual void SetCrc(int crc)
            {
                this.crc = crc;
            }

            /// <summary>
            ///     Gets the whirlpool digest of this entry.
            /// </summary>
            /// <returns> The whirlpool digest. </returns>
            public virtual byte[] GetWhirlpool()
            {
                return whirlpool;
            }

            /// <summary>
            ///     Sets the whirlpool digest of this entry.
            /// </summary>
            /// <param name="whirlpool"> The whirlpool digest. </param>
            /// <exception cref="IllegalArgumentException"> if the digest is not 64 bytes long. </exception>
            public virtual void SetWhirlpool(byte[] whirlpool)
            {
                if (whirlpool.Length != 64)
                {
                    throw new ArgumentException();
                }

                Array.Copy(whirlpool, 0, this.whirlpool, 0, whirlpool.Length);
            }

            /// <summary>
            ///     Gets the version of this entry.
            /// </summary>
            /// <returns> The version. </returns>
            public virtual int GetVersion()
            {
                return version;
            }

            /// <summary>
            ///     Sets the version of this entry.
            /// </summary>
            /// <param name="version"> The version. </param>
            public virtual void SetVersion(int version)
            {
                this.version = version;
            }

            /// <summary>
            ///     Gets the number of actual child entries.
            /// </summary>
            /// <returns> The number of actual child entries. </returns>
            public virtual int Size()
            {
                return entries.Count;
            }

            /// <summary>
            ///     Gets the maximum number of child entries.
            /// </summary>
            /// <returns> The maximum number of child entries. </returns>
            public virtual int Capacity()
            {
                if (entries.Count == 0)
                {
                    return 0;
                }

                return entries.Last().Key.Value + 1;
            }

            /// <summary>
            ///     Gets the child entry with the specified id.
            /// </summary>
            /// <param name="id"> The id. </param>
            /// <returns> The entry, or {@code null} if it does not exist. </returns>
            public virtual ChildEntry GetEntry(int id)
            {
                return entries[id];
            }

            /// <summary>
            ///     Replaces or inserts the child entry with the specified id.
            /// </summary>
            /// <param name="id"> The id. </param>
            /// <param name="entry"> The entry. </param>
            public virtual void PutEntry(int id, ChildEntry entry)
            {
                entries.Add(id, entry);
            }

            /// <summary>
            ///     Removes the entry with the specified id.
            /// </summary>
            /// <param name="id"> The id. </param>
            /// <param name="entry"> The entry. </param>
            public virtual void RemoveEntry(int id, ChildEntry entry)
            {
                entries.Remove(id);
            }
        }
    }
}
