using java.io;
using java.lang;
using java.net;
using java.nio;
using java.nio.channels;
using java.text;
using java.util;

namespace RSUtilities
{
    /// <summary>
    ///     Contains <seealso cref="FileChannel" />-related utility methods.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public static class FileChannelUtils
    {

        /// <summary>
        ///     Reads as much as possible from the channel into the buffer.
        /// </summary>
        /// <param name="channel"> The channel. </param>
        /// <param name="buffer"> The buffer. </param>
        /// <param name="ptr"> The initial position in the channel. </param>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        /// <exception cref="EOFException">
        ///     if the end of the file was reached and the buffer
        ///     could not be completely populated.
        /// </exception>
        public static void ReadFully(FileChannel channel, ByteBuffer buffer, long ptr)
        {
            while (buffer.remaining() > 0)
            {
                long read = channel.read(buffer, ptr);
                if (read == -1)
                {
                    throw new EOFException();
                }
                ptr += read;
            }
        }
    }
}
