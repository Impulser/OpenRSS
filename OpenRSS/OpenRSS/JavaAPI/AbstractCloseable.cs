using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

namespace OpenRSS.JavaAPI
{
    public abstract class AbstractCloseable : Closeable
    {
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        public void close()
        {
            Close();
        }

        public abstract void Close();
    }
}
