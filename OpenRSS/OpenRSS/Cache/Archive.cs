using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using OpenRSS.Utility;

using VMUtilities.Collections;

namespace OpenRSS.Cache
{
    /// <summary>
    ///     An <seealso cref="Archive" /> is a file within the cache that can have multiple member
    ///     files inside it.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public class Archive
    {
        /// <summary>
        ///     The array of entries in this archive.
        /// </summary>
        private readonly ByteBuffer[] entries;

        /// <summary>
        ///     Creates a new archive.
        /// </summary>
        /// <param name="size"> The number of entries in the archive. </param>
        public Archive(int size)
        {
            entries = new ByteBuffer[size];
        }

        /// <summary>
        ///     Decodes the specified <seealso cref="ByteBuffer" /> into an <seealso cref="Archive" />.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <param name="size"> The size of the archive. </param>
        /// <returns> The decoded <seealso cref="Archive" />. </returns>
        public static Archive Decode(ByteBuffer buffer, int size)
        {
            /* allocate a new archive object */
            var archive = new Archive(size);

            /* read the number of chunks at the end of the archive */
            buffer.position(buffer.limit() - 1);
            var chunks = buffer.get() & 0xFF;

            /* read the sizes of the child entries and individual chunks */
            //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: int[][] chunkSizes = new int[chunks][size];
            var chunkSizes = ArrayUtil.ReturnRectangularArray<int>(chunks, size);
            var sizes = new int[size];
            buffer.position(buffer.limit() - 1 - chunks * size * 4);
            for (var chunk = 0; chunk < chunks; chunk++)
            {
                var chunkSize = 0;
                for (var id = 0; id < size; id++)
                {
                    /* read the delta-encoded chunk length */
                    var delta = buffer.getInt();
                    chunkSize += delta;

                    chunkSizes[chunk][id] = chunkSize; // store the size of this chunk
                    sizes[id] += chunkSize; // and add it to the size of the whole file
                }
            }

            /* allocate the buffers for the child entries */
            for (var id = 0; id < size; id++)
            {
                archive.entries[id] = ByteBuffer.allocate(sizes[id]);
            }

            /* read the data into the buffers */
            buffer.position(0);
            for (var chunk = 0; chunk < chunks; chunk++)
            {
                for (var id = 0; id < size; id++)
                {
                    /* get the length of this chunk */
                    var chunkSize = chunkSizes[chunk][id];

                    /* copy this chunk into a temporary buffer */
                    var temp = new byte[chunkSize];
                    buffer.get(temp);

                    /* copy the temporary buffer into the file buffer */
                    archive.entries[id].put(temp);
                }
            }

            /* flip all of the buffers */
            for (var id = 0; id < size; id++)
            {
                archive.entries[id].flip();
            }

            /* return the archive */
            return archive;
        }

        /// <summary>
        ///     Encodes this <seealso cref="Archive" /> into a <seealso cref="ByteBuffer" />.
        ///     <p />
        ///     Please note that this is a fairly simple implementation that does not
        ///     attempt to use more than one chunk.
        /// </summary>
        /// <returns> An encoded <seealso cref="ByteBuffer" />. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public virtual ByteBuffer Encode() // TODO: an implementation that can use more than one chunk
        {
            var bout = new ByteArrayOutputStream();
            var os = new DataOutputStream(bout);
            try
            {
                /* add the data for each entry */
                for (var id = 0; id < entries.Length; id++)
                {
                    /* copy to temp buffer */
                    var temp = new byte[entries[id].limit()];
                    entries[id].position(0);
                    try
                    {
                        entries[id].get(temp);
                    }
                    finally
                    {
                        entries[id].position(0);
                    }

                    /* copy to output stream */
                    os.write(temp);
                }

                /* write the chunk lengths */
                var prev = 0;
                for (var id = 0; id < entries.Length; id++)
                {
                    /* 
                     * since each file is stored in the only chunk, just write the
                     * delta-encoded file size
                     */
                    var chunkSize = entries[id].limit();
                    os.writeInt(chunkSize - prev);
                    prev = chunkSize;
                }

                /* we only used one chunk due to a limitation of the implementation */
                bout.write(1);

                /* wrap the bytes from the stream in a buffer */
                var bytes = bout.toByteArray();
                return ByteBuffer.wrap(bytes);
            }
            finally
            {
                os.close();
            }
        }

        /// <summary>
        ///     Gets the size of this archive.
        /// </summary>
        /// <returns> The size of this archive. </returns>
        public virtual int Size()
        {
            return entries.Length;
        }

        /// <summary>
        ///     Gets the entry with the specified id.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <returns> The entry. </returns>
        public virtual ByteBuffer GetEntry(int id)
        {
            return entries[id];
        }

        /// <summary>
        ///     Inserts/replaces the entry with the specified id.
        /// </summary>
        /// <param name="id"> The id. </param>
        /// <param name="buffer"> The entry. </param>
        public virtual void PutEntry(int id, ByteBuffer buffer)
        {
            entries[id] = buffer;
        }
    }
}
