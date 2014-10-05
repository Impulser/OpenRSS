using java.io;
using java.lang;
using java.math;
using java.net;
using java.nio;
using java.text;
using java.util;

namespace RSUtilities.Cryptography
{
    /// <summary>
    ///     An implementation of the RSA algorithm.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public static class Rsa
    {
        /// <summary>
        ///     Encrypts/decrypts the specified buffer with the key and modulus.
        /// </summary>
        /// <param name="buffer"> The input buffer. </param>
        /// <param name="modulus"> The modulus. </param>
        /// <param name="key"> The key. </param>
        /// <returns> The output buffer. </returns>
        public static ByteBuffer Crypt(ByteBuffer buffer, BigInteger modulus, BigInteger key)
        {
            var bytes = new byte[buffer.limit()];
            buffer.get(bytes);

            var @in = new BigInteger(bytes);
            var @out = @in.modPow(key, modulus);

            return ByteBuffer.wrap(@out.toByteArray());
        }
    }
}
