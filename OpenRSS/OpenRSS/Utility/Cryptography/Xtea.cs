using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

namespace net.openrs.util.crypto
{
    /// <summary>
    ///     An implementation of the XTEA block cipher.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public sealed class Xtea
    {
        /// <summary>
        ///     The golden ratio.
        /// </summary>
        public const int GOLDEN_RATIO = unchecked((int) 0x9E3779B9);

        /// <summary>
        ///     The number of rounds.
        /// </summary>
        public const int ROUNDS = 32;

        /// <summary>
        ///     Default private constructor to prevent instantiation.
        /// </summary>
        private Xtea()
        { }

        /// <summary>
        ///     Deciphers the specified <seealso cref="ByteBuffer" /> with the given key.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <param name="key"> The key. </param>
        /// <exception cref="IllegalArgumentException">
        ///     if the key is not exactly 4 elements
        ///     long.
        /// </exception>
        public static void Decipher(ByteBuffer buffer, int[] key)
        {
            if (key.Length != 4)
            {
                throw new ArgumentException();
            }

            for (var i = 0; i < buffer.limit(); i += 8)
            {
                var sum = GOLDEN_RATIO * ROUNDS;
                int v0 = buffer.getInt(i * 4);
                int v1 = buffer.getInt(i * 4 + 4);
                for (var j = 0; j < ROUNDS; j++)
                {
                    v1 = (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
                    sum -= GOLDEN_RATIO;
                    v0 = (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
                }
                buffer.putInt(i * 4, v0);
                buffer.putInt(i * 4 + 4, v1);
            }
        }

        /// <summary>
        ///     Enciphers the specified <seealso cref="ByteBuffer" /> with the given key.
        /// </summary>
        /// <param name="buffer"> The buffer. </param>
        /// <param name="key"> The key. </param>
        /// <exception cref="IllegalArgumentException">
        ///     if the key is not exactly 4 elements
        ///     long.
        /// </exception>
        public static void Encipher(ByteBuffer buffer, int[] key)
        {
            if (key.Length != 4)
            {
                throw new ArgumentException();
            }

            for (var i = 0; i < buffer.limit(); i += 8)
            {
                var sum = 0;
                int v0 = buffer.getInt(i * 4);
                int v1 = buffer.getInt(i * 4 + 4);
                for (var j = 0; j < ROUNDS; j++)
                {
                    v0 = (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + key[sum & 3]);
                    sum += GOLDEN_RATIO;
                    v1 = (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + key[(sum >> 11) & 3]);
                }
                buffer.putInt(i * 4, v0);
                buffer.putInt(i * 4 + 4, v1);
            }
        }
    }
}
