using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using OpenRSS.Utility;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     An <seealso cref="Index" /> points to a file inside a <seealso cref="FileStore" />.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class Index
    {
        /// <summary>
        ///     The size of an index, in bytes.
        /// </summary>
        public const int SIZE = 6;

        /// <summary>
        ///     The number of the first sector that contains the file.
        /// </summary>
        private int sector;

        /// <summary>
        ///     The size of the file in bytes.
        /// </summary>
        private int size;

        /// <summary>
        ///     Creates a new index.
        /// </summary>
        /// <param name="size"> The size of the file in bytes. </param>
        /// <param name="sector"> The number of the first sector that contains the file. </param>
        public Index(int size, int sector)
        {
            this.size = size;
            this.sector = sector;
        }

        /// <summary>
        ///     Decodes the specified <seealso cref="ByteBuffer" /> into an <seealso cref="Index" /> object.
        /// </summary>
        /// <param name="buf"> The buffer. </param>
        /// <returns> The index. </returns>
        public static Index Decode(ByteBuffer buf)
        {
            if (buf.remaining() != SIZE)
            {
                throw new ArgumentException();
            }

            var size = ByteBufferUtils.GetTriByte(buf);
            var sector = ByteBufferUtils.GetTriByte(buf);
            return new Index(size, sector);
        }

        /// <summary>
        ///     Gets the size of the file.
        /// </summary>
        /// <returns> The size of the file in bytes. </returns>
        public int GetSize()
        {
            return size;
        }

        /// <summary>
        ///     Gets the number of the first sector that contains the file.
        /// </summary>
        /// <returns> The number of the first sector that contains the file. </returns>
        public int GetSector()
        {
            return sector;
        }

        /// <summary>
        ///     Encodes this index into a byte buffer.
        /// </summary>
        /// <returns> The buffer. </returns>
        public ByteBuffer Encode()
        {
            var buf = ByteBuffer.allocate(SIZE);
            ByteBufferUtils.PutTriByte(buf, size);
            ByteBufferUtils.PutTriByte(buf, sector);
            return (ByteBuffer) buf.flip();
        }
    }
}
