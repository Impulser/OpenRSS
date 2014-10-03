using System;
using Console = System.Console;

using java.io;
using java.nio;
using java.util;
using java.math;
using java.text;
using java.util.zip;



namespace VMUtilities.IO
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
