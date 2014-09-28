using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;
using java.util.zip;

using net.openrs.util.crypto;

using StringBuilder = System.Text.StringBuilder;

namespace net.openrs.util
{
    /// <summary>
    ///     Contains <seealso cref="ByteBuffer" />-related utility methods.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class ByteBufferUtils
    {
        /// <summary>
        ///     The modified set of 'extended ASCII' characters used by the client.
        /// </summary>
        private static char[] CHARACTERS = {
            '\u20AC',
            '\0',
            '\u201A',
            '\u0192',
            '\u201E',
            '\u2026',
            '\u2020',
            '\u2021',
            '\u02C6',
            '\u2030',
            '\u0160',
            '\u2039',
            '\u0152',
            '\0',
            '\u017D',
            '\0',
            '\0',
            '\u2018',
            '\u2019',
            '\u201C',
            '\u201D',
            '\u2022',
            '\u2013',
            '\u2014',
            '\u02DC',
            '\u2122',
            '\u0161',
            '\u203A',
            '\u0153',
            '\0',
            '\u017E',
            '\u0178'
        };

        /// <summary>
        ///     Default private constructor to prevent instantiation.
        /// </summary>
        private ByteBufferUtils()
        { }

        /// <summary>
        ///     Gets a null-terminated string from the specified buffer, using a
        ///     modified ISO-8859-1 character set.
        /// </summary>
        /// <param name="buf"> The buffer. </param>
        /// <returns> The decoded string. </returns>
        public static string GetJagexString(ByteBuffer buf)
        {
            var bldr = new StringBuilder();
            int b;
            while ((b = buf.get()) != 0)
            {
                if (b >= 127 && b < 160)
                {
                    var curChar = CHARACTERS[b - 128];
                    if (curChar != 0)
                    {
                        bldr.Append(curChar);
                    }
                }
                else
                {
                    bldr.Append((char) b);
                }
            }
            return bldr.ToString();
        }

        /// <summary>
        ///     Reads a 'tri-byte' from the specified buffer.
        /// </summary>
        /// <param name="buf"> The buffer. </param>
        /// <returns> The value. </returns>
        public static int GetTriByte(ByteBuffer buf)
        {
            return ((buf.get() & 0xFF) << 16) | ((buf.get() & 0xFF) << 8) | (buf.get() & 0xFF);
        }

        /// <summary>
        ///     Writes a 'tri-byte' to the specified buffer.
        /// </summary>
        /// <param name="buf"> The buffer. </param>
        /// <param name="value"> The value. </param>
        public static void PutTriByte(ByteBuffer buf, int value)
        {
            buf.put((byte) (value >> 16));
            buf.put((byte) (value >> 8));
            buf.put((byte) value);
        }

        /// <summary>
        ///     Calculates the CRC32 checksum of the specified buffer.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <returns> The CRC32 checksum. </returns>
        public static int GetCrcChecksum(ByteBuffer buffer)
        {
            var crc = new CRC32();
            for (var i = 0; i < buffer.limit(); i++)
            {
                crc.update(buffer.get(i));
            }
            return (int) crc.getValue();
        }

        /// <summary>
        ///     Calculates the whirlpool digest of the specified buffer.
        /// </summary>
        /// <param name="buf"> The buffer. </param>
        /// <returns> The 64-byte whirlpool digest. </returns>
        public static byte[] GetWhirlpoolDigest(ByteBuffer buf)
        {
            var bytes = new byte[buf.limit()];
            buf.get(bytes);
            return Whirlpool.Crypt(bytes, 0, bytes.Length);
        }

        /// <summary>
        ///     Converts the contents of the specified byte buffer to a string, which is
        ///     formatted similarly to the output of the <seealso cref="Arrays#toString()" />
        ///     method.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <returns> The string. </returns>
        public static string ToString(ByteBuffer buffer)
        {
            var builder = new StringBuilder("[");
            for (var i = 0; i < buffer.limit(); i++)
            {
                string hex = (buffer.get(i) & 0xFF).ToString("x")
                                                 .ToUpper();
                if (hex.Length == 1)
                {
                    hex = "0" + hex;
                }

                builder.Append("0x")
                       .Append(hex);
                if (i != buffer.limit() - 1)
                {
                    builder.Append(", ");
                }
            }
            builder.Append("]");
            return builder.ToString();
        }
    }
}
