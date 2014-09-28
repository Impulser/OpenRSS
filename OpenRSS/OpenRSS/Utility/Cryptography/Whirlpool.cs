using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using Exception = System.Exception;

namespace net.openrs.util.crypto
{
    /// <summary>
    ///     The Whirlpool hashing function.
    ///     <P>
    ///         <b>References</b>
    ///         <P>
    ///             The Whirlpool algorithm was developed by
    ///             <a href="mailto:pbarreto@scopus.com.br">Paulo S. L. M. Barreto</a> and
    ///             <a href="mailto:vincent.rijmen@cryptomathic.com">Vincent Rijmen</a>.
    ///             See
    ///             P.S.L.M. Barreto, V. Rijmen,
    ///             ``The Whirlpool hashing function,''
    ///             First NESSIE workshop, 2000 (tweaked version, 2003),
    ///             <https://www.cosic.esat.kuleuven.ac.be/nessie/workshop/submissions/whirlpool.zip>
    ///                 @author    Paulo S.L.M. Barreto
    ///                 @author    Vincent Rijmen.
    ///                 @version 3.0 (2003.03.12)
    ///                 =============================================================================
    ///                 Differences from version 2.1:
    ///                 - Suboptimal diffusion matrix replaced by cir(1, 1, 4, 1, 8, 5, 2, 9).
    ///                 =============================================================================
    ///                 Differences from version 2.0:
    ///                 - Generation of ISO/IEC 10118-3 test vectors.
    ///                 - Bug fix: nonzero carry was ignored when tallying the data length
    ///                 (this bug apparently only manifested itself when feeding data
    ///                 in pieces rather than in a single chunk at once).
    ///                 Differences from version 1.0:
    ///                 - Original S-box replaced by the tweaked, hardware-efficient version.
    ///                 =============================================================================
    ///                 THIS SOFTWARE IS PROVIDED BY THE AUTHORS ''AS IS'' AND ANY EXPRESS
    ///                 OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
    ///                 WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ///                 ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHORS OR CONTRIBUTORS BE
    ///                 LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    ///                 CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
    ///                 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
    ///                 BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
    ///                 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
    ///                 OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
    ///                 EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
    /// </summary>
    public class Whirlpool
    {
        /// <summary>
        ///     The message digest size (in bits)
        /// </summary>
        public const int DIGESTBITS = 512;

        /// <summary>
        ///     The number of rounds of the internal dedicated block cipher.
        /// </summary>
        protected internal const int R = 10;

        /// <summary>
        ///     The substitution box.
        /// </summary>
        private const string sbox =
            "\u1823\uc6E8\u87B8\u014F\u36A6\ud2F5\u796F\u9152" + "\u60Bc\u9B8E\uA30c\u7B35\u1dE0\ud7c2\u2E4B\uFE57" + "\u1577\u37E5\u9FF0\u4AdA\u58c9\u290A\uB1A0\u6B85" +
            "\uBd5d\u10F4\ucB3E\u0567\uE427\u418B\uA77d\u95d8" + "\uFBEE\u7c66\udd17\u479E\ucA2d\uBF07\uAd5A\u8333" + "\u6302\uAA71\uc819\u49d9\uF2E3\u5B88\u9A26\u32B0" +
            "\uE90F\ud580\uBEcd\u3448\uFF7A\u905F\u2068\u1AAE" + "\uB454\u9322\u64F1\u7312\u4008\uc3Ec\udBA1\u8d3d" + "\u9700\ucF2B\u7682\ud61B\uB5AF\u6A50\u45F3\u30EF" +
            "\u3F55\uA2EA\u65BA\u2Fc0\udE1c\uFd4d\u9275\u068A" + "\uB2E6\u0E1F\u62d4\uA896\uF9c5\u2559\u8472\u394c" + "\u5E78\u388c\ud1A5\uE261\uB321\u9c1E\u43c7\uFc04" +
            "\u5199\u6d0d\uFAdF\u7E24\u3BAB\ucE11\u8F4E\uB7EB" + "\u3c81\u94F7\uB913\u2cd3\uE76E\uc403\u5644\u7FA9" + "\u2ABB\uc153\udc0B\u9d6c\u3174\uF646\uAc89\u14E1" +
            "\u163A\u6909\u70B6\ud0Ed\ucc42\u98A4\u285c\uF886";

        /// <summary>
        ///     The message digest size (in bytes)
        /// </summary>
        public static readonly int DIGESTBYTES = DIGESTBITS >> 3;

        //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
        //ORIGINAL LINE: private static long[][] C = new long[8][256];
        private static long[][] C = RectangularArrays.ReturnRectangularLongArray(8, 256);

        private static long[] rc = new long[R + 1];

        protected internal long[] K = new long[8]; // the round key

        protected internal long[] L = new long[8];

        /// <summary>
        ///     Global number of hashed bits (256-bit counter).
        /// </summary>
        protected internal byte[] bitLength = new byte[32];

        protected internal long[] block = new long[8]; // mu(buffer)

        /// <summary>
        ///     Buffer of data to hash.
        /// </summary>
        protected internal byte[] buffer = new byte[64];

        /// <summary>
        ///     Current number of bits on the buffer.
        /// </summary>
        protected internal int bufferBits = 0;

        /// <summary>
        ///     Current (possibly incomplete) byte slot on the buffer.
        /// </summary>
        protected internal int bufferPos = 0;

        /// <summary>
        ///     The hashing state.
        /// </summary>
        protected internal long[] hash = new long[8];

        protected internal long[] state = new long[8]; // the cipher state

        static Whirlpool()
        {
            for (var x = 0; x < 256; x++)
            {
                var c = sbox[x / 2];
                long v1 = ((x & 1) == 0) ? (int) ((uint) c >> 8) : c & 0xff;
                var v2 = v1 << 1;
                if (v2 >= 0x100L)
                {
                    v2 ^= 0x11dL;
                }
                var v4 = v2 << 1;
                if (v4 >= 0x100L)
                {
                    v4 ^= 0x11dL;
                }
                var v5 = v4 ^ v1;
                var v8 = v4 << 1;
                if (v8 >= 0x100L)
                {
                    v8 ^= 0x11dL;
                }
                var v9 = v8 ^ v1;
                /*
                 * build the circulant table C[0][x] = S[x].[1, 1, 4, 1, 8, 5, 2, 9]:
                 */
                C[0][x] = (v1 << 56) | (v1 << 48) | (v4 << 40) | (v1 << 32) | (v8 << 24) | (v5 << 16) | (v2 << 8) | (v9);
                /*
                 * build the remaining circulant tables C[t][x] = C[0][x] rotr t
                 */
                for (var t = 1; t < 8; t++)
                {
                    C[t][x] = ((long) ((ulong) C[t - 1][x] >> 8)) | ((C[t - 1][x] << 56));
                }
            }

            /*
             * build the round constants:
             */
            rc[0] = 0L; // not used (assigment kept only to properly initialize all variables)
            for (var r = 1; r <= R; r++)
            {
                var i = 8 * (r - 1);
                rc[r] = (C[0][i] & unchecked((long)0xff00000000000000L)) ^ (C[1][i + 1] & 0x00ff000000000000L) ^ (C[2][i + 2] & 0x0000ff0000000000L) ^ (C[3][i + 3] & 0x000000ff00000000L) ^
                        (C[4][i + 4] & 0x00000000ff000000L) ^ (C[5][i + 5] & 0x0000000000ff0000L) ^ (C[6][i + 6] & 0x000000000000ff00L) ^ (C[7][i + 7] & 0x00000000000000ffL);
            }
        }

        public static byte[] Crypt(byte[] data, int off, int len)
        {
            byte[] source;
            if (off <= 0)
            {
                source = data;
            }
            else
            {
                source = new byte[len];
                for (var i = 0; i < len; i++)
                {
                    source[i] = data[off + i];
                }
            }
            var whirlpool = new Whirlpool();
            whirlpool.NESSIEinit();
            whirlpool.NESSIEadd(source, len * 8);
            var digest = new byte[64];
            whirlpool.NESSIEfinalize(digest);
            return digest;
        }

        /// <summary>
        ///     The core Whirlpool transform.
        /// </summary>
        protected internal virtual void ProcessBuffer()
        {
            /*
             * map the buffer to a block:
             */
            for (int i = 0,
                     j = 0;
                 i < 8;
                 i++, j += 8)
            {
                block[i] = (((long) buffer[j]) << 56) ^ ((buffer[j + 1] & 0xffL) << 48) ^ ((buffer[j + 2] & 0xffL) << 40) ^ ((buffer[j + 3] & 0xffL) << 32) ^ ((buffer[j + 4] & 0xffL) << 24) ^
                           ((buffer[j + 5] & 0xffL) << 16) ^ ((buffer[j + 6] & 0xffL) << 8) ^ ((buffer[j + 7] & 0xffL));
            }
            /*
             * compute and apply K^0 to the cipher state:
             */
            for (var i = 0; i < 8; i++)
            {
                state[i] = block[i] ^ (K[i] = hash[i]);
            }
            /*
             * iterate over all rounds:
             */
            for (var r = 1; r <= R; r++)
            {
                /*
                 * compute K^r from K^{r-1}:
                 */
                for (var i = 0; i < 8; i++)
                {
                    L[i] = 0L;
                    for (int t = 0,
                             s = 56;
                         t < 8;
                         t++, s -= 8)
                    {
                        L[i] ^= C[t][(int) ((long) ((ulong) K[(i - t) & 7] >> s)) & 0xff];
                    }
                }
                for (var i = 0; i < 8; i++)
                {
                    K[i] = L[i];
                }
                K[0] ^= rc[r];
                /*
                 * apply the r-th round transformation:
                 */
                for (var i = 0; i < 8; i++)
                {
                    L[i] = K[i];
                    for (int t = 0,
                             s = 56;
                         t < 8;
                         t++, s -= 8)
                    {
                        L[i] ^= C[t][(int) ((long) ((ulong) state[(i - t) & 7] >> s)) & 0xff];
                    }
                }
                for (var i = 0; i < 8; i++)
                {
                    state[i] = L[i];
                }
            }
            /*
             * apply the Miyaguchi-Preneel compression function:
             */
            for (var i = 0; i < 8; i++)
            {
                hash[i] ^= state[i] ^ block[i];
            }
        }

        /// <summary>
        ///     Initialize the hashing state.
        /// </summary>
        public virtual void NESSIEinit()
        {
            Arrays.fill(bitLength, (byte) 0);
            bufferBits = bufferPos = 0;
            buffer[0] = 0; // it's only necessary to cleanup buffer[bufferPos].
            Arrays.fill(hash, 0L); // initial value
        }

        /// <summary>
        ///     Delivers input data to the hashing algorithm.
        /// </summary>
        /// <param name="source">        plaintext data to hash. </param>
        /// <param name="sourceBits">
        ///     how many bits of plaintext to process.
        ///     This method maintains the invariant: bufferBits < 512 </param>
        public virtual void NESSIEadd(byte[] source, long sourceBits)
        {
            /*
                               sourcePos
                               |
                               +-------+-------+-------
                                  ||||||||||||||||||||| source
                               +-------+-------+-------
            +-------+-------+-------+-------+-------+-------
            ||||||||||||||||||||||                           buffer
            +-------+-------+-------+-------+-------+-------
                            |
                            bufferPos
            */
            var sourcePos = 0; // index of leftmost source byte containing data (1 to 8 bits).
            var sourceGap = (8 - ((int) sourceBits & 7)) & 7; // space on source[sourcePos].
            var bufferRem = bufferBits & 7; // occupied bits on buffer[bufferPos].
            int b;
            // tally the length of the added data:
            var value = sourceBits;
            for (int i = 31,
                     carry = 0;
                 i >= 0;
                 i--)
            {
                carry += (bitLength[i] & 0xff) + ((int) value & 0xff);
                bitLength[i] = (byte) carry;
                carry = (int) ((uint) carry >> 8);
                value = (long) ((ulong) value >> 8);
            }
            // process data in chunks of 8 bits:
            while (sourceBits > 8) // at least source[sourcePos] and source[sourcePos+1] contain data.
            {
                // take a byte from the source:
                b = ((source[sourcePos] << sourceGap) & 0xff) | ((int) ((uint) (source[sourcePos + 1] & 0xff) >> (8 - sourceGap)));
                if (b < 0 || b >= 256)
                {
                    throw new Exception("LOGIC ERROR");
                }
                // process this byte:
                buffer[bufferPos++] |= (byte) ((int) ((uint) b >> bufferRem));
                bufferBits += 8 - bufferRem; // bufferBits = 8*bufferPos;
                if (bufferBits == 512)
                {
                    // process data block:
                    ProcessBuffer();
                    // reset buffer:
                    bufferBits = bufferPos = 0;
                }
                buffer[bufferPos] = unchecked((byte) ((b << (8 - bufferRem)) & 0xff));
                bufferBits += bufferRem;
                // proceed to remaining data:
                sourceBits -= 8;
                sourcePos++;
            }
            // now 0 <= sourceBits <= 8;
            // furthermore, all data (if any is left) is in source[sourcePos].
            if (sourceBits > 0)
            {
                b = (source[sourcePos] << sourceGap) & 0xff; // bits are left-justified on b.
                // process the remaining bits:
                buffer[bufferPos] |= (byte) ((int) ((uint) b >> bufferRem));
            }
            else
            {
                b = 0;
            }
            if (bufferRem + sourceBits < 8)
            {
                // all remaining data fits on buffer[bufferPos], and there still remains some space.
                bufferBits += (int) sourceBits;
            }
            else
            {
                // buffer[bufferPos] is full:
                bufferPos++;
                bufferBits += 8 - bufferRem; // bufferBits = 8*bufferPos;
                sourceBits -= 8 - bufferRem;
                // now 0 <= sourceBits < 8; furthermore, all data is in source[sourcePos].
                if (bufferBits == 512)
                {
                    // process data block:
                    ProcessBuffer();
                    // reset buffer:
                    bufferBits = bufferPos = 0;
                }
                buffer[bufferPos] = unchecked((byte) ((b << (8 - bufferRem)) & 0xff));
                bufferBits += (int) sourceBits;
            }
        }

        /// <summary>
        ///     Get the hash value from the hashing state.
        ///     This method uses the invariant: bufferBits < 512
        /// </summary>
        public virtual void NESSIEfinalize(byte[] digest)
        {
            // append a '1'-bit:
            buffer[bufferPos] |= (byte) ((int) ((uint) 0x80 >> (bufferBits & 7)));
            bufferPos++; // all remaining bits on the current byte are set to zero.
            // pad with zero bits to complete 512N + 256 bits:
            if (bufferPos > 32)
            {
                while (bufferPos < 64)
                {
                    buffer[bufferPos++] = 0;
                }
                // process data block:
                ProcessBuffer();
                // reset buffer:
                bufferPos = 0;
            }
            while (bufferPos < 32)
            {
                buffer[bufferPos++] = 0;
            }
            // append bit length of hashed data:
            Array.Copy(bitLength, 0, buffer, 32, 32);
            // process data block:
            ProcessBuffer();
            // return the completed message digest:
            for (int i = 0,
                     j = 0;
                 i < 8;
                 i++, j += 8)
            {
                var h = hash[i];
                digest[j] = (byte) ((long) ((ulong) h >> 56));
                digest[j + 1] = (byte) ((long) ((ulong) h >> 48));
                digest[j + 2] = (byte) ((long) ((ulong) h >> 40));
                digest[j + 3] = (byte) ((long) ((ulong) h >> 32));
                digest[j + 4] = (byte) ((long) ((ulong) h >> 24));
                digest[j + 5] = (byte) ((long) ((ulong) h >> 16));
                digest[j + 6] = (byte) ((long) ((ulong) h >> 8));
                digest[j + 7] = (byte) (h);
            }
        }

        /// <summary>
        ///     Delivers string input data to the hashing algorithm.
        /// </summary>
        /// <param name="source">
        ///     plaintext data to hash (ASCII text string).
        ///     This method maintains the invariant: bufferBits < 512 </param>
        public virtual void NESSIEadd(string source)
        {
            if (source.Length > 0)
            {
                var data = new byte[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    data[i] = (byte) source[i];
                }
                NESSIEadd(data, 8 * data.Length);
            }
        }
    }
}
