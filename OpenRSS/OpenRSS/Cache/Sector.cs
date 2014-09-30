using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using OpenRSS.Extensions;
using OpenRSS.Utility;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     A <seealso cref="Sector" /> contains a header and data. The header contains information
    ///     used to verify the integrity of the cache like the current file id, type and
    ///     chunk. It also contains a pointer to the next sector such that the sectors
    ///     form a singly-linked list. The data is simply up to 512 bytes of the file.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class Sector
    {
        /// <summary>
        ///     The size of the header within a sector in bytes.
        /// </summary>
        public const int HEADER_SIZE = 8;

        /// <summary>
        ///     The size of the data within a sector in bytes.
        /// </summary>
        public const int DATA_SIZE = 512;

        /// <summary>
        ///     The total size of a sector in bytes.
        /// </summary>
        public static readonly int SIZE = HEADER_SIZE + DATA_SIZE;

        /// <summary>
        ///     The chunk within the file that this sector contains.
        /// </summary>
        private readonly int chunk;

        /// <summary>
        ///     The data in this sector.
        /// </summary>
        private readonly byte[] data;

        /// <summary>
        ///     The id of the file this sector contains.
        /// </summary>
        private readonly int id;

        /// <summary>
        ///     The next sector.
        /// </summary>
        private readonly int nextSector;

        /// <summary>
        ///     The type of file this sector contains.
        /// </summary>
        private readonly int type;

        /// <summary>
        ///     Creates a new sector.
        /// </summary>
        /// <param name="type"> The type of the file. </param>
        /// <param name="id"> The file's id. </param>
        /// <param name="chunk"> The chunk of the file this sector contains. </param>
        /// <param name="nextSector"> The sector containing the next chunk. </param>
        /// <param name="data"> The data in this sector. </param>
        public Sector(int type, int id, int chunk, int nextSector, byte[] data)
        {
            this.type = type;
            this.id = id;
            this.chunk = chunk;
            this.nextSector = nextSector;
            this.data = data;
        }

        /// <summary>
        ///     Decodes the specified <seealso cref="ByteBuffer" /> into a <seealso cref="Sector" /> object.
        /// </summary>
        /// <param name="buf"> The buffer. </param>
        /// <returns> The sector. </returns>
        public static Sector Decode(ByteBuffer buf)
        {
            if (buf.remaining() != SIZE)
            {
                throw new ArgumentException();
            }

            var id = buf.getShort() & 0xFFFF;
            var chunk = buf.getShort() & 0xFFFF;
            var nextSector = ByteBufferExtensions.GetTriByte(buf);
            var type = buf.get() & 0xFF;
            var data = new byte[DATA_SIZE];
            buf.get(data);

            return new Sector(type, id, chunk, nextSector, data);
        }

        /// <summary>
        ///     Gets the type of file in this sector.
        /// </summary>
        /// <returns> The type of file in this sector. </returns>
        public int GetType()
        {
            return type;
        }

        /// <summary>
        ///     Gets the id of the file within this sector.
        /// </summary>
        /// <returns> The id of the file in this sector. </returns>
        public int GetId()
        {
            return id;
        }

        /// <summary>
        ///     Gets the chunk of the file this sector contains.
        /// </summary>
        /// <returns> The chunk of the file this sector contains. </returns>
        public int GetChunk()
        {
            return chunk;
        }

        /// <summary>
        ///     Gets the next sector.
        /// </summary>
        /// <returns> The next sector. </returns>
        public int GetNextSector()
        {
            return nextSector;
        }

        /// <summary>
        ///     Gets this sector's data.
        /// </summary>
        /// <returns> The data within this sector. </returns>
        public byte[] GetData()
        {
            return data;
        }

        /// <summary>
        ///     Encodes this sector into a <seealso cref="ByteBuffer" />.
        /// </summary>
        /// <returns> The encoded buffer. </returns>
        public ByteBuffer Encode()
        {
            var buf = ByteBuffer.allocate(SIZE);

            buf.putShort((short) id);
            buf.putShort((short) chunk);
            ByteBufferExtensions.PutTriByte(buf, nextSector);
            buf.put((byte) type);
            buf.put(data);

            return (ByteBuffer) buf.flip();
        }
    }
}
