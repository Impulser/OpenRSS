using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using java.io;

namespace OpenRSS.JavaAPI
{
    public abstract class AbstractCloseable : Closeable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
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
