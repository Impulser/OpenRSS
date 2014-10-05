// ***********************************************************************
// Assembly         : RSUtilities
// Author           : Alex Camilleri
//
// Last Modified By : Alex Camilleri
// Last Modified On : 10-03-2014
// ***********************************************************************
// <copyright file="AbstractCloseable.cs" company="Kaini Industries">
//     Copyright (c) Kaini Industries. All rights reserved.
// </copyright>
// ***********************************************************************

using java.io;
using java.lang;
using java.math;
using java.nio;
using java.text;
using java.util;
using java.util.zip;

namespace RSUtilities.Tools
{
    /// <summary>
    /// A {@code Closeable} is a source or destination of data that can be closed.
    /// The close method is invoked to release resources that the object is
    /// holding (such as open files).
    /// @since 1.5
    /// </summary>
    public abstract class AbstractCloseable : Closeable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Closes this stream and releases any system resources associated
        /// with it. If the stream is already closed then invoking this
        /// method has no effect.
        /// <p> As noted in <seealso cref="AutoCloseable#close()" />, cases where the
        /// close may fail require careful attention. It is strongly advised
        /// to relinquish the underlying resources and to internally
        /// <em>mark</em> the {@code Closeable} as closed, prior to throwing
        /// the <seealso cref="IOException" />.</p>
        /// </summary>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public void close()
        {
            Close();
        }

        /// <summary>
        /// Closes this stream and releases any system resources associated
        /// with it. If the stream is already closed then invoking this
        /// method has no effect.
        /// <p> As noted in <seealso cref="AutoCloseable#close()" />, cases where the
        /// close may fail require careful attention. It is strongly advised
        /// to relinquish the underlying resources and to internally
        /// <em>mark</em> the {@code Closeable} as closed, prior to throwing
        /// the <seealso cref="IOException" />.</p>
        /// </summary>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public abstract void Close();
    }
}
