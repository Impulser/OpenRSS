using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using OpenRSS.Utility.Compression;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     A <seealso cref="Container" /> holds an optionally compressed file. This class can be
    ///     used to decompress and compress containers. A container can also have a two
    ///     byte trailer which specifies the version of the file within it.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class Container
    {
        /// <summary>
        ///     This type indicates that no compression is used.
        /// </summary>
        public const int COMPRESSION_NONE = 0;

        /// <summary>
        ///     This type indicates that BZIP2 compression is used.
        /// </summary>
        public const int COMPRESSION_BZIP2 = 1;

        /// <summary>
        ///     This type indicates that GZIP compression is used.
        /// </summary>
        public const int COMPRESSION_GZIP = 2;

        /// <summary>
        ///     The decompressed data.
        /// </summary>
        private ByteBuffer data;

        /// <summary>
        ///     The type of compression this container uses.
        /// </summary>
        private int type;

        /// <summary>
        ///     The version of the file within this container.
        /// </summary>
        private int version;

        /// <summary>
        ///     Creates a new unversioned container.
        /// </summary>
        /// <param name="type"> The type of compression. </param>
        /// <param name="data"> The decompressed data. </param>
        public Container(int type, ByteBuffer data)
            : this(type, data, -1)
        { }

        /// <summary>
        ///     Creates a new versioned container.
        /// </summary>
        /// <param name="type"> The type of compression. </param>
        /// <param name="data"> The decompressed data. </param>
        /// <param name="version"> The version of the file within this container. </param>
        public Container(int type, ByteBuffer data, int version)
        {
            this.type = type;
            this.data = data;
            this.version = version;
        }

        /// <summary>
        ///     Decodes and decompresses the container.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <returns> The decompressed container. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static Container Decode(ByteBuffer buffer)
        {
            /* decode the type and length */
            var type = buffer.get() & 0xFF;
            var length = buffer.getInt();

            /* check if we should decompress the data or not */
            if (type == COMPRESSION_NONE)
            {
                /* simply grab the data and wrap it in a buffer */
                var temp = new byte[length];
                buffer.get(temp);
                var data = ByteBuffer.wrap(temp);

                /* decode the version if present */
                var version = -1;
                if (buffer.remaining() >= 2)
                {
                    version = buffer.getShort();
                }

                /* and return the decoded container */
                return new Container(type, data, version);
            }
            else
            {
                /* grab the length of the uncompressed data */
                var uncompressedLength = buffer.getInt();

                /* grab the data */
                var compressed = new byte[length];
                buffer.get(compressed);

                /* uncompress it */
                byte[] uncompressed;
                if (type == COMPRESSION_BZIP2)
                {
                    uncompressed = CompressionUtils.Bunzip2(compressed);
                }
                else if (type == COMPRESSION_GZIP)
                {
                    uncompressed = CompressionUtils.Gunzip(compressed);
                }
                else
                {
                    throw new IOException("Invalid compression type");
                }

                /* check if the lengths are equal */
                if (uncompressed.Length != uncompressedLength)
                {
                    throw new IOException("Length mismatch");
                }

                /* decode the version if present */
                var version = -1;
                if (buffer.remaining() >= 2)
                {
                    version = buffer.getShort();
                }

                /* and return the decoded container */
                return new Container(type, ByteBuffer.wrap(uncompressed), version);
            }
        }

        /// <summary>
        ///     Checks if this container is versioned.
        /// </summary>
        /// <returns> {@code true} if so, {@code false} if not. </returns>
        public bool IsVersioned()
        {
            return version != -1;
        }

        /// <summary>
        ///     Gets the version of the file in this container.
        /// </summary>
        /// <returns> The version of the file. </returns>
        /// <exception cref="IllegalArgumentException"> if this container is not versioned. </exception>
        public int GetVersion()
        {
            if (!IsVersioned())
            {
                throw new IllegalStateException();
            }

            return version;
        }

        /// <summary>
        ///     Sets the version of this container.
        /// </summary>
        /// <param name="version"> The version. </param>
        public void SetVersion(int version)
        {
            this.version = version;
        }

        /// <summary>
        ///     Removes the version on this container so it becomes unversioned.
        /// </summary>
        public void RemoveVersion()
        {
            version = -1;
        }

        /// <summary>
        ///     Sets the type of this container.
        /// </summary>
        /// <param name="type"> The compression type. </param>
        public void SetType(int type)
        {
            this.type = type;
        }

        /// <summary>
        ///     Gets the type of this container.
        /// </summary>
        /// <returns> The compression type. </returns>
        public int GetType()
        {
            return type;
        }

        /// <summary>
        ///     Gets the decompressed data.
        /// </summary>
        /// <returns> The decompressed data. </returns>
        public ByteBuffer GetData()
        {
            return data.asReadOnlyBuffer();
        }

        /// <summary>
        ///     Encodes and compresses this container.
        /// </summary>
        /// <returns> The buffer. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public ByteBuffer Encode()
        {
            var data = GetData(); // so we have a read only view, making this method thread safe

            /* grab the data as a byte array for compression */
            var bytes = new byte[data.limit()];
            data.mark();
            data.get(bytes);
            data.reset();

            /* compress the data */
            byte[] compressed;
            if (type == COMPRESSION_NONE)
            {
                compressed = bytes;
            }
            else if (type == COMPRESSION_GZIP)
            {
                compressed = CompressionUtils.Gzip(bytes);
            }
            else if (type == COMPRESSION_BZIP2)
            {
                compressed = CompressionUtils.Bzip2(bytes);
            }
            else
            {
                throw new IOException("Invalid compression type");
            }

            /* calculate the size of the header and trailer and allocate a buffer */
            var header = 5 + (type == COMPRESSION_NONE ? 0 : 4) + (IsVersioned() ? 2 : 0);
            var buf = ByteBuffer.allocate(header + compressed.Length);

            /* write the header, with the optional uncompressed length */
            buf.put((byte) type);
            buf.putInt(compressed.Length);
            if (type != COMPRESSION_NONE)
            {
                buf.putInt(data.limit());
            }

            /* write the compressed length */
            buf.put(compressed);

            /* write the trailer with the optional version */
            if (IsVersioned())
            {
                buf.putShort((short) version);
            }

            /* flip the buffer and return it */
            return (ByteBuffer) buf.flip();
        }
    }
}
