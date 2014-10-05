using System.Linq;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

namespace RSUtilities.Cryptography
{
    /// <summary>
    ///     An implementation of the {@code djb2} hash function.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public static class Djb2
    {
        /// <summary>
        ///     An implementation of Dan Bernstein's {@code djb2} hash function
        ///     which is slightly modified. Instead of the initial hash being 5381, it
        ///     is zero.
        /// </summary>
        /// <param name="str"> The string to hash. </param>
        /// <returns> The hash code. </returns>
        public static int Crypt(string str)
        {
            return str.Aggregate(0, (current, t) => t + ((current << 5) - current));
        }
    }
}
