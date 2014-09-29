using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;
using java.util.zip;

using OpenRSS.Utility.Compression.BZip2;

namespace OpenRSS.Utility.Compression
{
    /// <summary>
    ///     A class that contains methods to compress and uncompress BZIP2 and GZIP
    ///     byte arrays.
    ///     @author Graham
    ///     @author `Discardedx2
    /// </summary>
    public static class CompressionUtils
    {

        /// <summary>
        ///     Compresses a GZIP file.
        /// </summary>
        /// <param name="bytes"> The uncompressed bytes. </param>
        /// <returns> The compressed bytes. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static byte[] Gzip(byte[] bytes)
        {
            /* create the streams */
            var @is = new ByteArrayInputStream(bytes);
            try
            {
                var bout = new ByteArrayOutputStream();
                var os = new GZIPOutputStream(bout);
                try
                {
                    /* copy data between the streams */
                    var buf = new byte[4096];
                    var len = 0;
                    while ((len = @is.read(buf, 0, buf.Length)) != -1)
                    {
                        os.write(buf, 0, len);
                    }
                }
                finally
                {
                    os.close();
                }

                /* return the compressed bytes */
                return bout.toByteArray();
            }
            finally
            {
                @is.close();
            }
        }

        /// <summary>
        ///     Uncompresses a GZIP file.
        /// </summary>
        /// <param name="bytes"> The compressed bytes. </param>
        /// <returns> The uncompressed bytes. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static byte[] Gunzip(byte[] bytes)
        {
            /* create the streams */
            var @is = new GZIPInputStream(new ByteArrayInputStream(bytes));
            try
            {
                var os = new ByteArrayOutputStream();
                try
                {
                    /* copy data between the streams */
                    var buf = new byte[4096];
                    var len = 0;
                    while ((len = @is.read(buf, 0, buf.Length)) != -1)
                    {
                        os.write(buf, 0, len);
                    }
                }
                finally
                {
                    os.close();
                }

                /* return the uncompressed bytes */
                return os.toByteArray();
            }
            finally
            {
                @is.close();
            }
        }

        /// <summary>
        ///     Compresses a BZIP2 file.
        /// </summary>
        /// <param name="bytes"> The uncompressed bytes. </param>
        /// <returns> The compressed bytes without the header. </returns>
        /// <exception cref="IOException"> if an I/O erorr occurs. </exception>
        public static byte[] Bzip2(byte[] bytes)
        {
            var @is = new ByteArrayInputStream(bytes);
            try
            {
                var bout = new ByteArrayOutputStream();
                OutputStream os = new CBZip2OutputStream(bout, 1);
                try
                {
                    var buf = new byte[4096];
                    var len = 0;
                    while ((len = @is.read(buf, 0, buf.Length)) != -1)
                    {
                        os.write(buf, 0, len);
                    }
                }
                finally
                {
                    os.close();
                }

                /* strip the header from the byte array and return it */
                bytes = bout.toByteArray();
                var bzip2 = new byte[bytes.Length - 2];
                Array.Copy(bytes, 2, bzip2, 0, bzip2.Length);
                return bzip2;
            }
            finally
            {
                @is.close();
            }
        }

        /// <summary>
        ///     Uncompresses a BZIP2 file.
        /// </summary>
        /// <param name="bytes"> The compressed bytes without the header. </param>
        /// <returns> The uncompressed bytes. </returns>
        /// <exception cref="IOException"> if an I/O error occurs. </exception>
        public static byte[] Bunzip2(byte[] bytes)
        {
            /* prepare a new byte array with the bzip2 header at the start */
            var bzip2 = new byte[bytes.Length + 2];
            bzip2[0] = (byte) 'h';
            bzip2[1] = (byte) '1';
            Array.Copy(bytes, 0, bzip2, 2, bytes.Length);

            InputStream @is = new CBZip2InputStream(new ByteArrayInputStream(bzip2));
            try
            {
                var os = new ByteArrayOutputStream();
                try
                {
                    var buf = new byte[4096];
                    int len;
                    while ((len = @is.read(buf, 0, buf.Length)) != -1)
                    {
                        os.write(buf, 0, len);
                    }
                }
                finally
                {
                    os.close();
                }

                return os.toByteArray();
            }
            finally
            {
                @is.close();
            }
        }
    }
}
