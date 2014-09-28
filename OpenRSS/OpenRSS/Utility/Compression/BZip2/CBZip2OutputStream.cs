using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

using Math = System.Math;

/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

/*
 * This package is based on the work done by Keiron Liddle, Aftex Software
 * <keiron@aftexsw.com> to whom the Ant project is very grateful for his
 * great code.
 */

namespace org.apache.tools.bzip2
{
    /// <summary>
    ///     An output stream that compresses into the BZip2 format (without the file
    ///     header chars) into another stream.
    ///     <p>
    ///         The compression requires large amounts of memory. Thus you should call the
    ///         <seealso cref="#close() close()" /> method as soon as possible, to force
    ///         <tt>CBZip2OutputStream</tt> to release the allocated memory.
    ///     </p>
    ///     <p>
    ///         You can shrink the amount of allocated memory and maybe raise
    ///         the compression speed by choosing a lower blocksize, which in turn
    ///         may cause a lower compression ratio. You can avoid unnecessary
    ///         memory allocation by avoiding using a blocksize which is bigger
    ///         than the size of the input.
    ///     </p>
    ///     <p>
    ///         You can compute the memory usage for compressing by the
    ///         following formula:
    ///     </p>
    ///     <pre>
    ///         &lt;code&gt;400k + (9 * blocksize)&lt;/code&gt;.
    ///     </pre>
    ///     <p>
    ///         To get the memory required for decompression by {@link
    ///         CBZip2InputStream CBZip2InputStream} use
    ///     </p>
    ///     <pre>
    ///         &lt;code&gt;65k + (5 * blocksize)&lt;/code&gt;.
    ///     </pre>
    ///     <table width="100%"
    ///            border="1">
    ///         <colgroup>
    ///             <col width="33%" /> <col width="33%" /> <col width="33%" />
    ///         </colgroup>
    ///         <tr>
    ///             <th colspan="3">Memory usage by blocksize</th>
    ///         </tr>
    ///         <tr>
    ///             <th align="right">Blocksize</th>
    ///             <th align="right">
    ///                 Compression
    ///                 <br>
    ///                     memory usage
    ///             </th>
    ///             <th align="right">
    ///                 Decompression
    ///                 <br>
    ///                     memory usage
    ///             </th>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">100k</td>
    ///             <td align="right">1300k</td>
    ///             <td align="right">565k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">200k</td>
    ///             <td align="right">2200k</td>
    ///             <td align="right">1065k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">300k</td>
    ///             <td align="right">3100k</td>
    ///             <td align="right">1565k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">400k</td>
    ///             <td align="right">4000k</td>
    ///             <td align="right">2065k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">500k</td>
    ///             <td align="right">4900k</td>
    ///             <td align="right">2565k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">600k</td>
    ///             <td align="right">5800k</td>
    ///             <td align="right">3065k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">700k</td>
    ///             <td align="right">6700k</td>
    ///             <td align="right">3565k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">800k</td>
    ///             <td align="right">7600k</td>
    ///             <td align="right">4065k</td>
    ///         </tr>
    ///         <tr>
    ///             <td align="right">900k</td>
    ///             <td align="right">8500k</td>
    ///             <td align="right">4565k</td>
    ///         </tr>
    ///     </table>
    ///     <p>
    ///         For decompression <tt>CBZip2InputStream</tt> allocates less memory if the
    ///         bzipped input is smaller than one block.
    ///     </p>
    ///     <p>
    ///         Instances of this class are not threadsafe.
    ///     </p>
    ///     <p>
    ///         TODO: Update to BZip2 1.0.1
    ///     </p>
    ///     <p>
    ///         <strong>
    ///             This class has been modified so it does not use randomized blocks as
    ///             these are not supported by the client's bzip2 implementation.
    ///         </strong>
    ///     </p>
    /// </summary>
    public class CBZip2OutputStream : OutputStream, BZip2Constants
    {
        /// <summary>
        ///     The minimum supported blocksize <tt> == 1</tt>.
        /// </summary>
        public const int MIN_BLOCKSIZE = 1;

        /// <summary>
        ///     The maximum supported blocksize <tt> == 9</tt>.
        /// </summary>
        public const int MAX_BLOCKSIZE = 9;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal const int GREATER_ICOST = 15;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal const int LESSER_ICOST = 0;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal const int SMALL_THRESH = 20;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal const int DEPTH_THRESH = 10;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal const int WORK_FACTOR = 30;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        ///     <p>
        ///         If you are ever unlucky/improbable enough to get a stack
        ///         overflow whilst sorting, increase the following constant and
        ///         try again. In practice I have never seen the stack go above 27
        ///         elems, so the following limit seems very generous.
        ///     </p>
        /// </summary>
        protected internal const int QSORT_STACK_SIZE = 1000;

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal static readonly int SETMASK = (1 << 21);

        /// <summary>
        ///     This constant is accessible by subclasses for historical
        ///     purposes. If you don't know what it means then you don't need
        ///     it.
        /// </summary>
        protected internal static readonly int CLEARMASK = (~SETMASK);

        /// <summary>
        ///     Knuth's increments seem to work better than Incerpi-Sedgewick here.
        ///     Possibly because the number of elems to sort is usually small, typically
        ///     &lt;= 20.
        /// </summary>
        private static readonly int[] INCS =
        {
            1,
            4,
            13,
            40,
            121,
            364,
            1093,
            3280,
            9841,
            29524,
            88573,
            265720,
            797161,
            2391484
        };

        /// <summary>
        ///     Always: in the range 0 .. 9. The current block size is 100000 * this
        ///     number.
        /// </summary>
        private readonly int blockSize100k;

        private readonly CRC crc = new CRC();

        private int allowableBlockSize;

        private int blockCRC;

        private bool blockRandomised;

        private int bsBuff;

        private int bsLive;

        private int combinedCRC;

        private int currentChar = -1;

        /// <summary>
        ///     All memory intensive stuff.
        /// </summary>
        private Data data;

        private bool firstAttempt;

        /// <summary>
        ///     Index of the last char in the block, so the block size == last + 1.
        /// </summary>
        private int last;

        private int nInUse;

        private int nMTF;

        /// <summary>
        ///     Index in fmap[] of original string after sorting.
        /// </summary>
        private int origPtr;

        private OutputStream @out;

        private int runLength;

        /*
         * Used when sorting. If too many long comparisons happen, we stop sorting,
         * randomise the block slightly, and try again.
         */

        private int workDone;

        private int workLimit;

        /// <summary>
        ///     Constructs a new <tt>CBZip2OutputStream</tt> with a blocksize of 900k.
        ///     <p>
        ///         <b>Attention: </b>The caller is resonsible to write the two BZip2 magic
        ///         bytes <tt>"BZ"</tt> to the specified stream prior to calling this
        ///         constructor.
        ///     </p>
        /// </summary>
        /// <param name="out">
        ///     *
        ///     the destination stream.
        /// </param>
        /// <exception cref="IOException">
        ///     if an I/O error occurs in the specified stream.
        /// </exception>
        /// <exception cref="NullPointerException">
        ///     if <code>out == null</code>.
        /// </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public CBZip2OutputStream(final java.io.OutputStream out) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        public CBZip2OutputStream(OutputStream @out)
            : this(@out, MAX_BLOCKSIZE)
        { }

        /// <summary>
        ///     Constructs a new <tt>CBZip2OutputStream</tt> with specified blocksize.
        ///     <p>
        ///         <b>Attention: </b>The caller is resonsible to write the two BZip2 magic
        ///         bytes <tt>"BZ"</tt> to the specified stream prior to calling this
        ///         constructor.
        ///     </p>
        /// </summary>
        /// <param name="out">
        ///     the destination stream.
        /// </param>
        /// <param name="blockSize">
        ///     the blockSize as 100k units.
        /// </param>
        /// <exception cref="IOException">
        ///     if an I/O error occurs in the specified stream.
        /// </exception>
        /// <exception cref="IllegalArgumentException">
        ///     if <code>(blockSize < 1) || (blockSize> 9)</code>.
        /// </exception>
        /// <exception cref="NullPointerException">
        ///     if <code>out == null</code>.
        /// </exception>
        /// <seealso cref= #MIN_BLOCKSIZE
        /// </seealso>
        /// <seealso cref= #MAX_BLOCKSIZE
        /// </seealso>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public CBZip2OutputStream(final java.io.OutputStream out, final int blockSize) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        public CBZip2OutputStream(OutputStream @out, int blockSize)
        {
            if (blockSize < 1)
            {
                throw new ArgumentException("blockSize(" + blockSize + ") < 1");
            }
            if (blockSize > 9)
            {
                throw new ArgumentException("blockSize(" + blockSize + ") > 9");
            }

            blockSize100k = blockSize;
            this.@out = @out;
            Init();
        }

        /// <summary>
        ///     This method is accessible by subclasses for historical
        ///     purposes. If you don't know what it does then you don't need
        ///     it.
        /// </summary>
        protected internal static void HbMakeCodeLengths(char[] len, int[] freq, int alphaSize, int maxLen)
        {
            /*
             * Nodes and heap entries run from 1. Entry 0 for both the heap and
             * nodes is a sentinel.
             */
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] heap = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2];
            var heap = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] weight = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2];
            var weight = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] parent = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2];
            var parent = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2];

            for (var i = alphaSize; --i >= 0;)
            {
                weight[i + 1] = (freq[i] == 0 ? 1 : freq[i]) << 8;
            }

            for (var tooLong = true; tooLong;)
            {
                tooLong = false;

                var nNodes = alphaSize;
                var nHeap = 0;
                heap[0] = 0;
                weight[0] = 0;
                parent[0] = -2;

                for (var i = 1; i <= alphaSize; i++)
                {
                    parent[i] = -1;
                    nHeap++;
                    heap[nHeap] = i;

                    var zz = nHeap;
                    var tmp = heap[zz];
                    while (weight[tmp] < weight[heap[zz >> 1]])
                    {
                        heap[zz] = heap[zz >> 1];
                        zz >>= 1;
                    }
                    heap[zz] = tmp;
                }

                // assert (nHeap < (MAX_ALPHA_SIZE + 2)) : nHeap;

                while (nHeap > 1)
                {
                    var n1 = heap[1];
                    heap[1] = heap[nHeap];
                    nHeap--;

                    var yy = 0;
                    var zz = 1;
                    var tmp = heap[1];

                    while (true)
                    {
                        yy = zz << 1;

                        if (yy > nHeap)
                        {
                            break;
                        }

                        if ((yy < nHeap) && (weight[heap[yy + 1]] < weight[heap[yy]]))
                        {
                            yy++;
                        }

                        if (weight[tmp] < weight[heap[yy]])
                        {
                            break;
                        }

                        heap[zz] = heap[yy];
                        zz = yy;
                    }

                    heap[zz] = tmp;

                    var n2 = heap[1];
                    heap[1] = heap[nHeap];
                    nHeap--;

                    yy = 0;
                    zz = 1;
                    tmp = heap[1];

                    while (true)
                    {
                        yy = zz << 1;

                        if (yy > nHeap)
                        {
                            break;
                        }

                        if ((yy < nHeap) && (weight[heap[yy + 1]] < weight[heap[yy]]))
                        {
                            yy++;
                        }

                        if (weight[tmp] < weight[heap[yy]])
                        {
                            break;
                        }

                        heap[zz] = heap[yy];
                        zz = yy;
                    }

                    heap[zz] = tmp;
                    nNodes++;
                    parent[n1] = parent[n2] = nNodes;

                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int weight_n1 = weight[n1];
                    var weight_n1 = weight[n1];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int weight_n2 = weight[n2];
                    var weight_n2 = weight[n2];
                    weight[nNodes] = (((weight_n1 & unchecked((int) 0xffffff00)) + (weight_n2 & unchecked((int) 0xffffff00))) |
                                      (1 + (((weight_n1 & 0x000000ff) > (weight_n2 & 0x000000ff)) ? (weight_n1 & 0x000000ff) : (weight_n2 & 0x000000ff))));

                    parent[nNodes] = -1;
                    nHeap++;
                    heap[nHeap] = nNodes;

                    tmp = 0;
                    zz = nHeap;
                    tmp = heap[zz];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int weight_tmp = weight[tmp];
                    var weight_tmp = weight[tmp];
                    while (weight_tmp < weight[heap[zz >> 1]])
                    {
                        heap[zz] = heap[zz >> 1];
                        zz >>= 1;
                    }
                    heap[zz] = tmp;
                }

                // assert (nNodes < (MAX_ALPHA_SIZE * 2)) : nNodes;

                for (var i = 1; i <= alphaSize; i++)
                {
                    var j = 0;
                    var k = i;

                    for (int parent_k; (parent_k = parent[k]) >= 0;)
                    {
                        k = parent_k;
                        j++;
                    }

                    len[i - 1] = (char) j;
                    if (j > maxLen)
                    {
                        tooLong = true;
                    }
                }

                if (tooLong)
                {
                    for (var i = 1; i < alphaSize; i++)
                    {
                        var j = weight[i] >> 8;
                        j = 1 + (j >> 1);
                        weight[i] = j << 8;
                    }
                }
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private static void hbMakeCodeLengths(final byte[] len, final int[] freq, final Data dat, final int alphaSize, final int maxLen)
        private static void HbMakeCodeLengths(byte[] len, int[] freq, Data dat, int alphaSize, int maxLen)
        {
            /*
             * Nodes and heap entries run from 1. Entry 0 for both the heap and
             * nodes is a sentinel.
             */
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] heap = dat.heap;
            var heap = dat.heap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] weight = dat.weight;
            var weight = dat.weight;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] parent = dat.parent;
            var parent = dat.parent;

            for (var i = alphaSize; --i >= 0;)
            {
                weight[i + 1] = (freq[i] == 0 ? 1 : freq[i]) << 8;
            }

            for (var tooLong = true; tooLong;)
            {
                tooLong = false;

                var nNodes = alphaSize;
                var nHeap = 0;
                heap[0] = 0;
                weight[0] = 0;
                parent[0] = -2;

                for (var i = 1; i <= alphaSize; i++)
                {
                    parent[i] = -1;
                    nHeap++;
                    heap[nHeap] = i;

                    var zz = nHeap;
                    var tmp = heap[zz];
                    while (weight[tmp] < weight[heap[zz >> 1]])
                    {
                        heap[zz] = heap[zz >> 1];
                        zz >>= 1;
                    }
                    heap[zz] = tmp;
                }

                while (nHeap > 1)
                {
                    var n1 = heap[1];
                    heap[1] = heap[nHeap];
                    nHeap--;

                    var yy = 0;
                    var zz = 1;
                    var tmp = heap[1];

                    while (true)
                    {
                        yy = zz << 1;

                        if (yy > nHeap)
                        {
                            break;
                        }

                        if ((yy < nHeap) && (weight[heap[yy + 1]] < weight[heap[yy]]))
                        {
                            yy++;
                        }

                        if (weight[tmp] < weight[heap[yy]])
                        {
                            break;
                        }

                        heap[zz] = heap[yy];
                        zz = yy;
                    }

                    heap[zz] = tmp;

                    var n2 = heap[1];
                    heap[1] = heap[nHeap];
                    nHeap--;

                    yy = 0;
                    zz = 1;
                    tmp = heap[1];

                    while (true)
                    {
                        yy = zz << 1;

                        if (yy > nHeap)
                        {
                            break;
                        }

                        if ((yy < nHeap) && (weight[heap[yy + 1]] < weight[heap[yy]]))
                        {
                            yy++;
                        }

                        if (weight[tmp] < weight[heap[yy]])
                        {
                            break;
                        }

                        heap[zz] = heap[yy];
                        zz = yy;
                    }

                    heap[zz] = tmp;
                    nNodes++;
                    parent[n1] = parent[n2] = nNodes;

                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int weight_n1 = weight[n1];
                    var weight_n1 = weight[n1];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int weight_n2 = weight[n2];
                    var weight_n2 = weight[n2];
                    weight[nNodes] = ((weight_n1 & unchecked((int) 0xffffff00)) + (weight_n2 & unchecked((int) 0xffffff00))) |
                                     (1 + (((weight_n1 & 0x000000ff) > (weight_n2 & 0x000000ff)) ? (weight_n1 & 0x000000ff) : (weight_n2 & 0x000000ff)));

                    parent[nNodes] = -1;
                    nHeap++;
                    heap[nHeap] = nNodes;

                    tmp = 0;
                    zz = nHeap;
                    tmp = heap[zz];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int weight_tmp = weight[tmp];
                    var weight_tmp = weight[tmp];
                    while (weight_tmp < weight[heap[zz >> 1]])
                    {
                        heap[zz] = heap[zz >> 1];
                        zz >>= 1;
                    }
                    heap[zz] = tmp;
                }

                for (var i = 1; i <= alphaSize; i++)
                {
                    var j = 0;
                    var k = i;

                    for (int parent_k; (parent_k = parent[k]) >= 0;)
                    {
                        k = parent_k;
                        j++;
                    }

                    len[i - 1] = (byte) j;
                    if (j > maxLen)
                    {
                        tooLong = true;
                    }
                }

                if (tooLong)
                {
                    for (var i = 1; i < alphaSize; i++)
                    {
                        var j = weight[i] >> 8;
                        j = 1 + (j >> 1);
                        weight[i] = j << 8;
                    }
                }
            }
        }

        /// <summary>
        ///     Chooses a blocksize based on the given length of the data to compress.
        /// </summary>
        /// <returns>
        ///     The blocksize, between <seealso cref="#MIN_BLOCKSIZE" /> and
        ///     <seealso cref="#MAX_BLOCKSIZE" /> both inclusive. For a negative
        ///     <tt>inputLength</tt> this method returns <tt>MAX_BLOCKSIZE</tt>
        ///     always.
        /// </returns>
        /// <param name="inputLength">
        ///     The length of the data which will be compressed by
        ///     <tt>CBZip2OutputStream</tt>.
        /// </param>
        public static int ChooseBlockSize(long inputLength)
        {
            return (inputLength > 0) ? (int) Math.Min((inputLength / 132000) + 1, 9) : MAX_BLOCKSIZE;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void write(final int b) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        public virtual void Write(int b)
        {
            if (@out != null)
            {
                Write0(b);
            }
            else
            {
                throw new IOException("closed");
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void writeRun() throws java.io.IOException
        private void WriteRun()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int lastShadow = this.last;
            var lastShadow = last;

            if (lastShadow < allowableBlockSize)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int currentCharShadow = this.currentChar;
                var currentCharShadow = currentChar;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final Data dataShadow = this.data;
                var dataShadow = data;
                dataShadow.inUse[currentCharShadow] = true;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte ch = (byte) currentCharShadow;
                var ch = (byte) currentCharShadow;

                var runLengthShadow = runLength;
                crc.UpdateCRC(currentCharShadow, runLengthShadow);

                switch (runLengthShadow)
                {
                    case 1:
                        dataShadow.block[lastShadow + 2] = ch;
                        last = lastShadow + 1;
                        break;

                    case 2:
                        dataShadow.block[lastShadow + 2] = ch;
                        dataShadow.block[lastShadow + 3] = ch;
                        last = lastShadow + 2;
                        break;

                    case 3:
                    {
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final byte[] block = dataShadow.block;
                        var block = dataShadow.block;
                        block[lastShadow + 2] = ch;
                        block[lastShadow + 3] = ch;
                        block[lastShadow + 4] = ch;
                        last = lastShadow + 3;
                    }
                        break;

                    default:
                    {
                        runLengthShadow -= 4;
                        dataShadow.inUse[runLengthShadow] = true;
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final byte[] block = dataShadow.block;
                        var block = dataShadow.block;
                        block[lastShadow + 2] = ch;
                        block[lastShadow + 3] = ch;
                        block[lastShadow + 4] = ch;
                        block[lastShadow + 5] = ch;
                        block[lastShadow + 6] = (byte) runLengthShadow;
                        last = lastShadow + 5;
                    }
                        break;
                }
            }
            else
            {
                EndBlock();
                InitBlock();
                WriteRun();
            }
        }

        /// <summary>
        ///     Overriden to close the stream.
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected void finalize() throws Throwable
        ~CBZip2OutputStream()
        {
            Finish();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void finish() throws java.io.IOException
        public virtual void Finish()
        {
            if (@out != null)
            {
                try
                {
                    if (runLength > 0)
                    {
                        WriteRun();
                    }
                    currentChar = -1;
                    EndBlock();
                    EndCompression();
                }
                finally
                {
                    @out = null;
                    data = null;
                }
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void close() throws java.io.IOException
        public virtual void Close()
        {
            if (@out != null)
            {
                var outShadow = @out;
                Finish();
                outShadow.close();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void flush() throws java.io.IOException
        public virtual void Flush()
        {
            var outShadow = @out;
            if (outShadow != null)
            {
                outShadow.flush();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void init() throws java.io.IOException
        private void Init()
        {
            // write magic: done by caller who created this stream
            // this.out.write('B');
            // this.out.write('Z');

            data = new Data(blockSize100k);

            /*
             * Write `magic' bytes h indicating file-format == huffmanised, followed
             * by a digit indicating blockSize100k.
             */
            BsPutUByte('h');
            BsPutUByte('0' + blockSize100k);

            combinedCRC = 0;
            InitBlock();
        }

        private void InitBlock()
        {
            // blockNo++;
            crc.InitialiseCRC();
            last = -1;
            // ch = 0;

            var inUse = data.inUse;
            for (var i = 256; --i >= 0;)
            {
                inUse[i] = false;
            }

            /* 20 is just a paranoia constant */
            allowableBlockSize = (blockSize100k * BZip2Constants_Fields.baseBlockSize) - 20;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void endBlock() throws java.io.IOException
        private void EndBlock()
        {
            blockCRC = crc.GetFinalCRC();
            combinedCRC = (combinedCRC << 1) | ((int) ((uint) combinedCRC >> 31));
            combinedCRC ^= blockCRC;

            // empty block at end of file
            if (last == -1)
            {
                return;
            }

            /* sort the block and establish posn of original string */
            BlockSort();

            /*
             * A 6-byte block header, the value chosen arbitrarily as 0x314159265359
             * :-). A 32 bit value does not really give a strong enough guarantee
             * that the value will not appear by chance in the compressed
             * datastream. Worst-case probability of this event, for a 900k block,
             * is about 2.0e-3 for 32 bits, 1.0e-5 for 40 bits and 4.0e-8 for 48
             * bits. For a compressed file of size 100Gb -- about 100000 blocks --
             * only a 48-bit marker will do. NB: normal compression/ decompression
             * donot rely on these statistical properties. They are only important
             * when trying to recover blocks from damaged files.
             */
            BsPutUByte(0x31);
            BsPutUByte(0x41);
            BsPutUByte(0x59);
            BsPutUByte(0x26);
            BsPutUByte(0x53);
            BsPutUByte(0x59);

            /* Now the block's CRC, so it is in a known place. */
            BsPutInt(blockCRC);

            /* Now a single bit indicating randomisation. */
            if (blockRandomised)
            {
                BsW(1, 1);
            }
            else
            {
                BsW(1, 0);
            }

            /* Finally, block's contents proper. */
            MoveToFrontCodeAndSend();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void endCompression() throws java.io.IOException
        private void EndCompression()
        {
            /*
             * Now another magic 48-bit number, 0x177245385090, to indicate the end
             * of the last block. (sqrt(pi), if you want to know. I did want to use
             * e, but it contains too much repetition -- 27 18 28 18 28 46 -- for me
             * to feel statistically comfortable. Call me paranoid.)
             */
            BsPutUByte(0x17);
            BsPutUByte(0x72);
            BsPutUByte(0x45);
            BsPutUByte(0x38);
            BsPutUByte(0x50);
            BsPutUByte(0x90);

            BsPutInt(combinedCRC);
            BsFinishedWithStream();
        }

        /// <summary>
        ///     Returns the blocksize parameter specified at construction time.
        /// </summary>
        public int GetBlockSize()
        {
            return blockSize100k;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void write(final byte[] buf, int offs, final int len) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        public virtual void Write(byte[] buf, int offs, int len)
        {
            if (offs < 0)
            {
                throw new IndexOutOfRangeException("offs(" + offs + ") < 0.");
            }
            if (len < 0)
            {
                throw new IndexOutOfRangeException("len(" + len + ") < 0.");
            }
            if (offs + len > buf.Length)
            {
                throw new IndexOutOfRangeException("offs(" + offs + ") + len(" + len + ") > buf.length(" + buf.Length + ").");
            }
            if (@out == null)
            {
                throw new IOException("stream closed");
            }

            for (var hi = offs + len; offs < hi;)
            {
                Write0(buf[offs++]);
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void write0(int b) throws java.io.IOException
        private void Write0(int b)
        {
            if (currentChar != -1)
            {
                b &= 0xff;
                if (currentChar == b)
                {
                    if (++runLength > 254)
                    {
                        WriteRun();
                        currentChar = -1;
                        runLength = 0;
                    }
                    // else nothing to do
                }
                else
                {
                    WriteRun();
                    runLength = 1;
                    currentChar = b;
                }
            }
            else
            {
                currentChar = b & 0xff;
                runLength++;
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private static void hbAssignCodes(final int[] code, final byte[] length, final int minLen, final int maxLen, final int alphaSize)
        private static void HbAssignCodes(int[] code, byte[] length, int minLen, int maxLen, int alphaSize)
        {
            var vec = 0;
            for (var n = minLen; n <= maxLen; n++)
            {
                for (var i = 0; i < alphaSize; i++)
                {
                    if ((length[i] & 0xff) == n)
                    {
                        code[i] = vec;
                        vec++;
                    }
                }
                vec <<= 1;
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void bsFinishedWithStream() throws java.io.IOException
        private void BsFinishedWithStream()
        {
            while (bsLive > 0)
            {
                var ch = bsBuff >> 24;
                @out.write(ch); // write 8-bit
                bsBuff <<= 8;
                bsLive -= 8;
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void bsW(final int n, final int v) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private void BsW(int n, int v)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.OutputStream outShadow = this.out;
            var outShadow = @out;
            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            while (bsLiveShadow >= 8)
            {
                outShadow.write(bsBuffShadow >> 24); // write 8-bit
                bsBuffShadow <<= 8;
                bsLiveShadow -= 8;
            }

            bsBuff = bsBuffShadow | (v << (32 - bsLiveShadow - n));
            bsLive = bsLiveShadow + n;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void bsPutUByte(final int c) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private void BsPutUByte(int c)
        {
            BsW(8, c);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void bsPutInt(final int u) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private void BsPutInt(int u)
        {
            BsW(8, (u >> 24) & 0xff);
            BsW(8, (u >> 16) & 0xff);
            BsW(8, (u >> 8) & 0xff);
            BsW(8, u & 0xff);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void sendMTFValues() throws java.io.IOException
        private void SendMTFValues()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[][] len = this.data.sendMTFValues_len;
            var len = data.sendMTFValues_len;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int alphaSize = this.nInUse + 2;
            var alphaSize = nInUse + 2;

            for (var t = BZip2Constants_Fields.N_GROUPS; --t >= 0;)
            {
                var len_t = len[t];
                for (var v = alphaSize; --v >= 0;)
                {
                    len_t[v] = GREATER_ICOST;
                }
            }

            /* Decide how many coding tables to use */
            // assert (this.nMTF > 0) : this.nMTF;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int nGroups = (this.nMTF < 200) ? 2 : (this.nMTF < 600) ? 3 : (this.nMTF < 1200) ? 4 : (this.nMTF < 2400) ? 5 : 6;
            var nGroups = (nMTF < 200) ? 2 : (nMTF < 600) ? 3 : (nMTF < 1200) ? 4 : (nMTF < 2400) ? 5 : 6;

            /* Generate an initial set of coding tables */
            SendMTFValues0(nGroups, alphaSize);

            /*
             * Iterate up to N_ITERS times to improve the tables.
             */
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int nSelectors = sendMTFValues1(nGroups, alphaSize);
            var nSelectors = SendMTFValues1(nGroups, alphaSize);

            /* Compute MTF values for the selectors. */
            SendMTFValues2(nGroups, nSelectors);

            /* Assign actual codes for the tables. */
            SendMTFValues3(nGroups, alphaSize);

            /* Transmit the mapping table. */
            SendMTFValues4();

            /* Now the selectors. */
            SendMTFValues5(nGroups, nSelectors);

            /* Now the coding tables. */
            SendMTFValues6(nGroups, alphaSize);

            /* And finally, the block data proper */
            SendMTFValues7(nSelectors);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private void sendMTFValues0(final int nGroups, final int alphaSize)
        private void SendMTFValues0(int nGroups, int alphaSize)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[][] len = this.data.sendMTFValues_len;
            var len = data.sendMTFValues_len;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] mtfFreq = this.data.mtfFreq;
            var mtfFreq = data.mtfFreq;

            var remF = nMTF;
            var gs = 0;

            for (var nPart = nGroups; nPart > 0; nPart--)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int tFreq = remF / nPart;
                var tFreq = remF / nPart;
                var ge = gs - 1;
                var aFreq = 0;

                for (var a = alphaSize - 1; (aFreq < tFreq) && (ge < a);)
                {
                    aFreq += mtfFreq[++ge];
                }

                if ((ge > gs) && (nPart != nGroups) && (nPart != 1) && (((nGroups - nPart) & 1) != 0))
                {
                    aFreq -= mtfFreq[ge--];
                }

                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte[] len_np = len[nPart - 1];
                var len_np = len[nPart - 1];
                for (var v = alphaSize; --v >= 0;)
                {
                    if ((v >= gs) && (v <= ge))
                    {
                        len_np[v] = LESSER_ICOST;
                    }
                    else
                    {
                        len_np[v] = GREATER_ICOST;
                    }
                }

                gs = ge + 1;
                remF -= aFreq;
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private int sendMTFValues1(final int nGroups, final int alphaSize)
        private int SendMTFValues1(int nGroups, int alphaSize)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] rfreq = dataShadow.sendMTFValues_rfreq;
            var rfreq = dataShadow.sendMTFValues_rfreq;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] fave = dataShadow.sendMTFValues_fave;
            var fave = dataShadow.sendMTFValues_fave;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final short[] cost = dataShadow.sendMTFValues_cost;
            var cost = dataShadow.sendMTFValues_cost;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[] sfmap = dataShadow.sfmap;
            var sfmap = dataShadow.sfmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] selector = dataShadow.selector;
            var selector = dataShadow.selector;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[][] len = dataShadow.sendMTFValues_len;
            var len = dataShadow.sendMTFValues_len;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] len_0 = len[0];
            var len_0 = len[0];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] len_1 = len[1];
            var len_1 = len[1];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] len_2 = len[2];
            var len_2 = len[2];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] len_3 = len[3];
            var len_3 = len[3];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] len_4 = len[4];
            var len_4 = len[4];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] len_5 = len[5];
            var len_5 = len[5];
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int nMTFShadow = this.nMTF;
            var nMTFShadow = nMTF;

            var nSelectors = 0;

            for (var iter = 0; iter < BZip2Constants_Fields.N_ITERS; iter++)
            {
                for (var t = nGroups; --t >= 0;)
                {
                    fave[t] = 0;
                    var rfreqt = rfreq[t];
                    for (var i = alphaSize; --i >= 0;)
                    {
                        rfreqt[i] = 0;
                    }
                }

                nSelectors = 0;

                for (var gs = 0; gs < nMTF;)
                {
                    /* Set group start & end marks. */

                    /*
                     * Calculate the cost of this group as coded by each of the
                     * coding tables.
                     */

                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int ge = Math.min(gs + BZip2Constants_Fields.G_SIZE - 1, nMTFShadow - 1);
                    var ge = Math.Min(gs + BZip2Constants_Fields.G_SIZE - 1, nMTFShadow - 1);

                    if (nGroups == BZip2Constants_Fields.N_GROUPS)
                    {
                        // unrolled version of the else-block

                        short cost0 = 0;
                        short cost1 = 0;
                        short cost2 = 0;
                        short cost3 = 0;
                        short cost4 = 0;
                        short cost5 = 0;

                        for (var i = gs; i <= ge; i++)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int icv = sfmap[i];
                            int icv = sfmap[i];
                            cost0 += (short) (len_0[icv] & 0xff);
                            cost1 += (short) (len_1[icv] & 0xff);
                            cost2 += (short) (len_2[icv] & 0xff);
                            cost3 += (short) (len_3[icv] & 0xff);
                            cost4 += (short) (len_4[icv] & 0xff);
                            cost5 += (short) (len_5[icv] & 0xff);
                        }

                        cost[0] = cost0;
                        cost[1] = cost1;
                        cost[2] = cost2;
                        cost[3] = cost3;
                        cost[4] = cost4;
                        cost[5] = cost5;
                    }
                    else
                    {
                        for (var t = nGroups; --t >= 0;)
                        {
                            cost[t] = 0;
                        }

                        for (var i = gs; i <= ge; i++)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int icv = sfmap[i];
                            int icv = sfmap[i];
                            for (var t = nGroups; --t >= 0;)
                            {
                                cost[t] += (short) (len[t][icv] & 0xff);
                            }
                        }
                    }

                    /*
                     * Find the coding table which is best for this group, and
                     * record its identity in the selector table.
                     */
                    var bt = -1;
                    for (int t = nGroups,
                             bc = 999999999;
                         --t >= 0;)
                    {
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int cost_t = cost[t];
                        int cost_t = cost[t];
                        if (cost_t < bc)
                        {
                            bc = cost_t;
                            bt = t;
                        }
                    }

                    fave[bt]++;
                    selector[nSelectors] = (byte) bt;
                    nSelectors++;

                    /*
                     * Increment the symbol frequencies for the selected table.
                     */
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int[] rfreq_bt = rfreq[bt];
                    var rfreq_bt = rfreq[bt];
                    for (var i = gs; i <= ge; i++)
                    {
                        rfreq_bt[sfmap[i]]++;
                    }

                    gs = ge + 1;
                }

                /*
                 * Recompute the tables based on the accumulated frequencies.
                 */
                for (var t = 0; t < nGroups; t++)
                {
                    HbMakeCodeLengths(len[t], rfreq[t], data, alphaSize, 20);
                }
            }

            return nSelectors;
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private void sendMTFValues2(final int nGroups, final int nSelectors)
        private void SendMTFValues2(int nGroups, int nSelectors)
        {
            // assert (nGroups < 8) : nGroups;

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            var pos = dataShadow.sendMTFValues2_pos;

            for (var i = nGroups; --i >= 0;)
            {
                pos[i] = (byte) i;
            }

            for (var i = 0; i < nSelectors; i++)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte ll_i = dataShadow.selector[i];
                var ll_i = dataShadow.selector[i];
                var tmp = pos[0];
                var j = 0;

                while (ll_i != tmp)
                {
                    j++;
                    var tmp2 = tmp;
                    tmp = pos[j];
                    pos[j] = tmp2;
                }

                pos[0] = tmp;
                dataShadow.selectorMtf[i] = (byte) j;
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private void sendMTFValues3(final int nGroups, final int alphaSize)
        private void SendMTFValues3(int nGroups, int alphaSize)
        {
            var code = data.sendMTFValues_code;
            var len = data.sendMTFValues_len;

            for (var t = 0; t < nGroups; t++)
            {
                var minLen = 32;
                var maxLen = 0;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte[] len_t = len[t];
                var len_t = len[t];
                for (var i = alphaSize; --i >= 0;)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int l = len_t[i] & 0xff;
                    var l = len_t[i] & 0xff;
                    if (l > maxLen)
                    {
                        maxLen = l;
                    }
                    if (l < minLen)
                    {
                        minLen = l;
                    }
                }

                // assert (maxLen <= 20) : maxLen;
                // assert (minLen >= 1) : minLen;

                HbAssignCodes(code[t], len[t], minLen, maxLen, alphaSize);
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void sendMTFValues4() throws java.io.IOException
        private void SendMTFValues4()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] inUse = this.data.inUse;
            var inUse = data.inUse;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] inUse16 = this.data.sentMTFValues4_inUse16;
            var inUse16 = data.sentMTFValues4_inUse16;

            for (var i = 16; --i >= 0;)
            {
                inUse16[i] = false;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int i16 = i * 16;
                var i16 = i * 16;
                for (var j = 16; --j >= 0;)
                {
                    if (inUse[i16 + j])
                    {
                        inUse16[i] = true;
                    }
                }
            }

            for (var i = 0; i < 16; i++)
            {
                BsW(1, inUse16[i] ? 1 : 0);
            }

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.OutputStream outShadow = this.out;
            var outShadow = @out;
            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            for (var i = 0; i < 16; i++)
            {
                if (inUse16[i])
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int i16 = i * 16;
                    var i16 = i * 16;
                    for (var j = 0; j < 16; j++)
                    {
                        // inlined: bsW(1, inUse[i16 + j] ? 1 : 0);
                        while (bsLiveShadow >= 8)
                        {
                            outShadow.write(bsBuffShadow >> 24); // write 8-bit
                            bsBuffShadow <<= 8;
                            bsLiveShadow -= 8;
                        }
                        if (inUse[i16 + j])
                        {
                            bsBuffShadow |= 1 << (32 - bsLiveShadow - 1);
                        }
                        bsLiveShadow++;
                    }
                }
            }

            bsBuff = bsBuffShadow;
            bsLive = bsLiveShadow;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void sendMTFValues5(final int nGroups, final int nSelectors) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private void SendMTFValues5(int nGroups, int nSelectors)
        {
            BsW(3, nGroups);
            BsW(15, nSelectors);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.OutputStream outShadow = this.out;
            var outShadow = @out;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] selectorMtf = this.data.selectorMtf;
            var selectorMtf = data.selectorMtf;

            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            for (var i = 0; i < nSelectors; i++)
            {
                for (int j = 0,
                         hj = selectorMtf[i] & 0xff;
                     j < hj;
                     j++)
                {
                    // inlined: bsW(1, 1);
                    while (bsLiveShadow >= 8)
                    {
                        outShadow.write(bsBuffShadow >> 24);
                        bsBuffShadow <<= 8;
                        bsLiveShadow -= 8;
                    }
                    bsBuffShadow |= 1 << (32 - bsLiveShadow - 1);
                    bsLiveShadow++;
                }

                // inlined: bsW(1, 0);
                while (bsLiveShadow >= 8)
                {
                    outShadow.write(bsBuffShadow >> 24);
                    bsBuffShadow <<= 8;
                    bsLiveShadow -= 8;
                }
                // bsBuffShadow |= 0 << (32 - bsLiveShadow - 1);
                bsLiveShadow++;
            }

            bsBuff = bsBuffShadow;
            bsLive = bsLiveShadow;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void sendMTFValues6(final int nGroups, final int alphaSize) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private void SendMTFValues6(int nGroups, int alphaSize)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[][] len = this.data.sendMTFValues_len;
            var len = data.sendMTFValues_len;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.OutputStream outShadow = this.out;
            var outShadow = @out;

            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            for (var t = 0; t < nGroups; t++)
            {
                var len_t = len[t];
                var curr = len_t[0] & 0xff;

                // inlined: bsW(5, curr);
                while (bsLiveShadow >= 8)
                {
                    outShadow.write(bsBuffShadow >> 24); // write 8-bit
                    bsBuffShadow <<= 8;
                    bsLiveShadow -= 8;
                }
                bsBuffShadow |= curr << (32 - bsLiveShadow - 5);
                bsLiveShadow += 5;

                for (var i = 0; i < alphaSize; i++)
                {
                    var lti = len_t[i] & 0xff;
                    while (curr < lti)
                    {
                        // inlined: bsW(2, 2);
                        while (bsLiveShadow >= 8)
                        {
                            outShadow.write(bsBuffShadow >> 24); // write 8-bit
                            bsBuffShadow <<= 8;
                            bsLiveShadow -= 8;
                        }
                        bsBuffShadow |= 2 << (32 - bsLiveShadow - 2);
                        bsLiveShadow += 2;

                        curr++; // 10
                    }

                    while (curr > lti)
                    {
                        // inlined: bsW(2, 3);
                        while (bsLiveShadow >= 8)
                        {
                            outShadow.write(bsBuffShadow >> 24); // write 8-bit
                            bsBuffShadow <<= 8;
                            bsLiveShadow -= 8;
                        }
                        bsBuffShadow |= 3 << (32 - bsLiveShadow - 2);
                        bsLiveShadow += 2;

                        curr--; // 11
                    }

                    // inlined: bsW(1, 0);
                    while (bsLiveShadow >= 8)
                    {
                        outShadow.write(bsBuffShadow >> 24); // write 8-bit
                        bsBuffShadow <<= 8;
                        bsLiveShadow -= 8;
                    }
                    // bsBuffShadow |= 0 << (32 - bsLiveShadow - 1);
                    bsLiveShadow++;
                }
            }

            bsBuff = bsBuffShadow;
            bsLive = bsLiveShadow;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void sendMTFValues7(final int nSelectors) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private void SendMTFValues7(int nSelectors)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[][] len = dataShadow.sendMTFValues_len;
            var len = dataShadow.sendMTFValues_len;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] code = dataShadow.sendMTFValues_code;
            var code = dataShadow.sendMTFValues_code;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.OutputStream outShadow = this.out;
            var outShadow = @out;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] selector = dataShadow.selector;
            var selector = dataShadow.selector;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[] sfmap = dataShadow.sfmap;
            var sfmap = dataShadow.sfmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int nMTFShadow = this.nMTF;
            var nMTFShadow = nMTF;

            var selCtr = 0;

            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            for (var gs = 0; gs < nMTFShadow;)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int ge = Math.min(gs + BZip2Constants_Fields.G_SIZE - 1, nMTFShadow - 1);
                var ge = Math.Min(gs + BZip2Constants_Fields.G_SIZE - 1, nMTFShadow - 1);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int selector_selCtr = selector[selCtr] & 0xff;
                var selector_selCtr = selector[selCtr] & 0xff;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int[] code_selCtr = code[selector_selCtr];
                var code_selCtr = code[selector_selCtr];
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte[] len_selCtr = len[selector_selCtr];
                var len_selCtr = len[selector_selCtr];

                while (gs <= ge)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int sfmap_i = sfmap[gs];
                    int sfmap_i = sfmap[gs];

                    //
                    // inlined: bsW(len_selCtr[sfmap_i] & 0xff,
                    // code_selCtr[sfmap_i]);
                    //
                    while (bsLiveShadow >= 8)
                    {
                        outShadow.write(bsBuffShadow >> 24);
                        bsBuffShadow <<= 8;
                        bsLiveShadow -= 8;
                    }
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int n = len_selCtr[sfmap_i] & 0xFF;
                    var n = len_selCtr[sfmap_i] & 0xFF;
                    bsBuffShadow |= code_selCtr[sfmap_i] << (32 - bsLiveShadow - n);
                    bsLiveShadow += n;

                    gs++;
                }

                gs = ge + 1;
                selCtr++;
            }

            bsBuff = bsBuffShadow;
            bsLive = bsLiveShadow;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void moveToFrontCodeAndSend() throws java.io.IOException
        private void MoveToFrontCodeAndSend()
        {
            BsW(24, origPtr);
            GenerateMTFValues();
            SendMTFValues();
        }

        /// <summary>
        ///     This is the most hammered method of this class.
        ///     <p>
        ///         This is the version using unrolled loops. Normally I never use such ones
        ///         in Java code. The unrolling has shown a noticable performance improvement
        ///         on JRE 1.4.2 (Linux i586 / HotSpot Client). Of course it depends on the
        ///         JIT compiler of the vm.
        ///     </p>
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private boolean mainSimpleSort(final Data dataShadow, final int lo, final int hi, final int d)
        private bool MainSimpleSort(Data dataShadow, int lo, int hi, int d)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int bigN = hi - lo + 1;
            var bigN = hi - lo + 1;
            if (bigN < 2)
            {
                return firstAttempt && (workDone > workLimit);
            }

            var hp = 0;
            while (INCS[hp] < bigN)
            {
                hp++;
            }

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] fmap = dataShadow.fmap;
            var fmap = dataShadow.fmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[] quadrant = dataShadow.quadrant;
            var quadrant = dataShadow.quadrant;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] block = dataShadow.block;
            var block = dataShadow.block;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int lastShadow = this.last;
            var lastShadow = last;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int lastPlus1 = lastShadow + 1;
            var lastPlus1 = lastShadow + 1;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean firstAttemptShadow = this.firstAttempt;
            var firstAttemptShadow = firstAttempt;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int workLimitShadow = this.workLimit;
            var workLimitShadow = workLimit;
            var workDoneShadow = workDone;

            // Following block contains unrolled code which could be shortened by
            // coding it in additional loops.

            while (--hp >= 0)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int h = INCS[hp];
                var h = INCS[hp];
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int mj = lo + h - 1;
                var mj = lo + h - 1;

                for (var i = lo + h; i <= hi;)
                {
                    // copy
                    for (var k = 3; (i <= hi) && (--k >= 0); i++)
                    {
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int v = fmap[i];
                        var v = fmap[i];
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int vd = v + d;
                        var vd = v + d;
                        var j = i;

                        // for (int a;
                        // (j > mj) && mainGtU((a = fmap[j - h]) + d, vd,
                        // block, quadrant, lastShadow);
                        // j -= h) {
                        // fmap[j] = a;
                        // }
                        //
                        // unrolled version:

                        // start inline mainGTU
                        var onceRunned = false;
                        var a = 0;

                        while (true)
                        {
                            if (onceRunned)
                            {
                                fmap[j] = a;
                                if ((j -= h) <= mj)
                                {
                                    goto HAMMERBreak;
                                }
                            }
                            else
                            {
                                onceRunned = true;
                            }

                            a = fmap[j - h];
                            var i1 = a + d;
                            var i2 = vd;

                            // following could be done in a loop, but
                            // unrolled it for performance:
                            if (block[i1 + 1] == block[i2 + 1])
                            {
                                if (block[i1 + 2] == block[i2 + 2])
                                {
                                    if (block[i1 + 3] == block[i2 + 3])
                                    {
                                        if (block[i1 + 4] == block[i2 + 4])
                                        {
                                            if (block[i1 + 5] == block[i2 + 5])
                                            {
                                                if (block[(i1 += 6)] == block[(i2 += 6)])
                                                {
                                                    var x = lastShadow;
                                                    while (x > 0)
                                                    {
                                                        x -= 4;

                                                        if (block[i1 + 1] == block[i2 + 1])
                                                        {
                                                            if (quadrant[i1] == quadrant[i2])
                                                            {
                                                                if (block[i1 + 2] == block[i2 + 2])
                                                                {
                                                                    if (quadrant[i1 + 1] == quadrant[i2 + 1])
                                                                    {
                                                                        if (block[i1 + 3] == block[i2 + 3])
                                                                        {
                                                                            if (quadrant[i1 + 2] == quadrant[i2 + 2])
                                                                            {
                                                                                if (block[i1 + 4] == block[i2 + 4])
                                                                                {
                                                                                    if (quadrant[i1 + 3] == quadrant[i2 + 3])
                                                                                    {
                                                                                        if ((i1 += 4) >= lastPlus1)
                                                                                        {
                                                                                            i1 -= lastPlus1;
                                                                                        }
                                                                                        if ((i2 += 4) >= lastPlus1)
                                                                                        {
                                                                                            i2 -= lastPlus1;
                                                                                        }
                                                                                        workDoneShadow++;
                                                                                    }
                                                                                    if ((quadrant[i1 + 3] > quadrant[i2 + 3]))
                                                                                    {
                                                                                        goto HAMMERContinue;
                                                                                    }
                                                                                    goto HAMMERBreak;
                                                                                }
                                                                                if ((block[i1 + 4] & 0xff) > (block[i2 + 4] & 0xff))
                                                                                {
                                                                                    goto HAMMERContinue;
                                                                                }
                                                                                goto HAMMERBreak;
                                                                            }
                                                                            if ((quadrant[i1 + 2] > quadrant[i2 + 2]))
                                                                            {
                                                                                goto HAMMERContinue;
                                                                            }
                                                                            goto HAMMERBreak;
                                                                        }
                                                                        if ((block[i1 + 3] & 0xff) > (block[i2 + 3] & 0xff))
                                                                        {
                                                                            goto HAMMERContinue;
                                                                        }
                                                                        goto HAMMERBreak;
                                                                    }
                                                                    if ((quadrant[i1 + 1] > quadrant[i2 + 1]))
                                                                    {
                                                                        goto HAMMERContinue;
                                                                    }
                                                                    goto HAMMERBreak;
                                                                }
                                                                if ((block[i1 + 2] & 0xff) > (block[i2 + 2] & 0xff))
                                                                {
                                                                    goto HAMMERContinue;
                                                                }
                                                                goto HAMMERBreak;
                                                            }
                                                            if ((quadrant[i1] > quadrant[i2]))
                                                            {
                                                                goto HAMMERContinue;
                                                            }
                                                            goto HAMMERBreak;
                                                        }
                                                        if ((block[i1 + 1] & 0xff) > (block[i2 + 1] & 0xff))
                                                        {
                                                            goto HAMMERContinue;
                                                        }
                                                        goto HAMMERBreak;

                                                        XContinue:
                                                        ;
                                                    }
                                                    goto HAMMERBreak;
                                                } // while x > 0
                                                if ((block[i1] & 0xff) > (block[i2] & 0xff))
                                                {
                                                    goto HAMMERContinue;
                                                }
                                                goto HAMMERBreak;
                                            }
                                            if ((block[i1 + 5] & 0xff) > (block[i2 + 5] & 0xff))
                                            {
                                                goto HAMMERContinue;
                                            }
                                            goto HAMMERBreak;
                                        }
                                        if ((block[i1 + 4] & 0xff) > (block[i2 + 4] & 0xff))
                                        {
                                            goto HAMMERContinue;
                                        }
                                        goto HAMMERBreak;
                                    }
                                    if ((block[i1 + 3] & 0xff) > (block[i2 + 3] & 0xff))
                                    {
                                        goto HAMMERContinue;
                                    }
                                    goto HAMMERBreak;
                                }
                                if ((block[i1 + 2] & 0xff) > (block[i2 + 2] & 0xff))
                                {
                                    goto HAMMERContinue;
                                }
                                goto HAMMERBreak;
                            }
                            if ((block[i1 + 1] & 0xff) > (block[i2 + 1] & 0xff))
                            { }
                            goto HAMMERBreak;

                            HAMMERContinue:
                            ;
                        }
                        HAMMERBreak: // HAMMER
                        // end inline mainGTU

                        fmap[j] = v;
                    }

                    if (firstAttemptShadow && (i <= hi) && (workDoneShadow > workLimitShadow))
                    {
                        goto HPBreak;
                    }
                }
            }
            HPBreak:

            workDone = workDoneShadow;
            return firstAttemptShadow && (workDoneShadow > workLimitShadow);
        }

        private static void Vswap(int[] fmap, int p1, int p2, int n)
        {
            n += p1;
            while (p1 < n)
            {
                var t = fmap[p1];
                fmap[p1++] = fmap[p2];
                fmap[p2++] = t;
            }
        }

        private static byte Med3(byte a, byte b, byte c)
        {
            return (a < b) ? (b < c ? b : a < c ? c : a) : (b > c ? b : a > c ? c : a);
        }

        private void BlockSort()
        {
            workLimit = WORK_FACTOR * last;
            workDone = 0;
            blockRandomised = false;
            firstAttempt = true;
            MainSort();

            if (firstAttempt && (workDone > workLimit))
            {
                //randomiseBlock();
                workLimit = workDone = 0;
                firstAttempt = false;
                MainSort();
            }

            var fmap = data.fmap;
            origPtr = -1;
            for (int i = 0,
                     lastShadow = last;
                 i <= lastShadow;
                 i++)
            {
                if (fmap[i] == 0)
                {
                    origPtr = i;
                    break;
                }
            }

            // assert (this.origPtr != -1) : this.origPtr;
        }

        /// <summary>
        ///     Method "mainQSort3", file "blocksort.c", BZip2 1.0.2
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private void mainQSort3(final Data dataShadow, final int loSt, final int hiSt, final int dSt)
        private void MainQSort3(Data dataShadow, int loSt, int hiSt, int dSt)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] stack_ll = dataShadow.stack_ll;
            var stack_ll = dataShadow.stack_ll;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] stack_hh = dataShadow.stack_hh;
            var stack_hh = dataShadow.stack_hh;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] stack_dd = dataShadow.stack_dd;
            var stack_dd = dataShadow.stack_dd;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] fmap = dataShadow.fmap;
            var fmap = dataShadow.fmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] block = dataShadow.block;
            var block = dataShadow.block;

            stack_ll[0] = loSt;
            stack_hh[0] = hiSt;
            stack_dd[0] = dSt;

            for (var sp = 1; --sp >= 0;)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int lo = stack_ll[sp];
                var lo = stack_ll[sp];
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int hi = stack_hh[sp];
                var hi = stack_hh[sp];
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int d = stack_dd[sp];
                var d = stack_dd[sp];

                if ((hi - lo < SMALL_THRESH) || (d > DEPTH_THRESH))
                {
                    if (MainSimpleSort(dataShadow, lo, hi, d))
                    {
                        return;
                    }
                }
                else
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int d1 = d + 1;
                    var d1 = d + 1;
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int med = med3(block[fmap[lo] + d1], block[fmap[hi] + d1], block[fmap[(lo + hi) >>> 1] + d1]) & 0xff;
                    var med = Med3(block[fmap[lo] + d1], block[fmap[hi] + d1], block[fmap[(int) ((uint) (lo + hi) >> 1)] + d1]) & 0xff;

                    var unLo = lo;
                    var unHi = hi;
                    var ltLo = lo;
                    var gtHi = hi;

                    while (true)
                    {
                        while (unLo <= unHi)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int n = ((int) block[fmap[unLo] + d1] & 0xff) - med;
                            var n = (block[fmap[unLo] + d1] & 0xff) - med;
                            if (n == 0)
                            {
                                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                                //ORIGINAL LINE: final int temp = fmap[unLo];
                                var temp = fmap[unLo];
                                fmap[unLo++] = fmap[ltLo];
                                fmap[ltLo++] = temp;
                            }
                            else if (n < 0)
                            {
                                unLo++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        while (unLo <= unHi)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int n = ((int) block[fmap[unHi] + d1] & 0xff) - med;
                            var n = (block[fmap[unHi] + d1] & 0xff) - med;
                            if (n == 0)
                            {
                                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                                //ORIGINAL LINE: final int temp = fmap[unHi];
                                var temp = fmap[unHi];
                                fmap[unHi--] = fmap[gtHi];
                                fmap[gtHi--] = temp;
                            }
                            else if (n > 0)
                            {
                                unHi--;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (unLo <= unHi)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int temp = fmap[unLo];
                            var temp = fmap[unLo];
                            fmap[unLo++] = fmap[unHi];
                            fmap[unHi--] = temp;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (gtHi < ltLo)
                    {
                        stack_ll[sp] = lo;
                        stack_hh[sp] = hi;
                        stack_dd[sp] = d1;
                        sp++;
                    }
                    else
                    {
                        var n = ((ltLo - lo) < (unLo - ltLo)) ? (ltLo - lo) : (unLo - ltLo);
                        Vswap(fmap, lo, unLo - n, n);
                        var m = ((hi - gtHi) < (gtHi - unHi)) ? (hi - gtHi) : (gtHi - unHi);
                        Vswap(fmap, unLo, hi - m + 1, m);

                        n = lo + unLo - ltLo - 1;
                        m = hi - (gtHi - unHi) + 1;

                        stack_ll[sp] = lo;
                        stack_hh[sp] = n;
                        stack_dd[sp] = d;
                        sp++;

                        stack_ll[sp] = n + 1;
                        stack_hh[sp] = m - 1;
                        stack_dd[sp] = d1;
                        sp++;

                        stack_ll[sp] = m;
                        stack_hh[sp] = hi;
                        stack_dd[sp] = d;
                        sp++;
                    }
                }
            }
        }

        private void MainSort()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] runningOrder = dataShadow.mainSort_runningOrder;
            var runningOrder = dataShadow.mainSort_runningOrder;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] copy = dataShadow.mainSort_copy;
            var copy = dataShadow.mainSort_copy;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] bigDone = dataShadow.mainSort_bigDone;
            var bigDone = dataShadow.mainSort_bigDone;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] ftab = dataShadow.ftab;
            var ftab = dataShadow.ftab;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] block = dataShadow.block;
            var block = dataShadow.block;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] fmap = dataShadow.fmap;
            var fmap = dataShadow.fmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[] quadrant = dataShadow.quadrant;
            var quadrant = dataShadow.quadrant;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int lastShadow = this.last;
            var lastShadow = last;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int workLimitShadow = this.workLimit;
            var workLimitShadow = workLimit;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean firstAttemptShadow = this.firstAttempt;
            var firstAttemptShadow = firstAttempt;

            // Set up the 2-byte frequency table
            for (var i = 65537; --i >= 0;)
            {
                ftab[i] = 0;
            }

            /*
             * In the various block-sized structures, live data runs from 0 to
             * last+NUM_OVERSHOOT_BYTES inclusive. First, set up the overshoot area
             * for block.
             */
            for (var i = 0; i < BZip2Constants_Fields.NUM_OVERSHOOT_BYTES; i++)
            {
                block[lastShadow + i + 2] = block[(i % (lastShadow + 1)) + 1];
            }
            for (var i = lastShadow + BZip2Constants_Fields.NUM_OVERSHOOT_BYTES + 1; --i >= 0;)
            {
                quadrant[i] = (char) 0;
            }
            block[0] = block[lastShadow + 1];

            // Complete the initial radix sort:

            var c1 = block[0] & 0xff;
            for (var i = 0; i <= lastShadow; i++)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int c2 = block[i + 1] & 0xff;
                var c2 = block[i + 1] & 0xff;
                ftab[(c1 << 8) + c2]++;
                c1 = c2;
            }

            for (var i = 1; i <= 65536; i++)
            {
                ftab[i] += ftab[i - 1];
            }

            c1 = block[1] & 0xff;
            for (var i = 0; i < lastShadow; i++)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int c2 = block[i + 2] & 0xff;
                var c2 = block[i + 2] & 0xff;
                fmap[--ftab[(c1 << 8) + c2]] = i;
                c1 = c2;
            }

            fmap[--ftab[((block[lastShadow + 1] & 0xff) << 8) + (block[1] & 0xff)]] = lastShadow;

            /*
             * Now ftab contains the first loc of every small bucket. Calculate the
             * running order, from smallest to largest big bucket.
             */
            for (var i = 256; --i >= 0;)
            {
                bigDone[i] = false;
                runningOrder[i] = i;
            }

            for (var h = 364; h != 1;)
            {
                h /= 3;
                for (var i = h; i <= 255; i++)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int vv = runningOrder[i];
                    var vv = runningOrder[i];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int a = ftab[(vv + 1) << 8] - ftab[vv << 8];
                    var a = ftab[(vv + 1) << 8] - ftab[vv << 8];
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int b = h - 1;
                    var b = h - 1;
                    var j = i;
                    for (var ro = runningOrder[j - h]; (ftab[(ro + 1) << 8] - ftab[ro << 8]) > a; ro = runningOrder[j - h])
                    {
                        runningOrder[j] = ro;
                        j -= h;
                        if (j <= b)
                        {
                            break;
                        }
                    }
                    runningOrder[j] = vv;
                }
            }

            /*
             * The main sorting loop.
             */
            for (var i = 0; i <= 255; i++)
            {
                /*
                 * Process big buckets, starting with the least full.
                 */
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int ss = runningOrder[i];
                var ss = runningOrder[i];

                // Step 1:
                /*
                 * Complete the big bucket [ss] by quicksorting any unsorted small
                 * buckets [ss, j]. Hopefully previous pointer-scanning phases have
                 * already completed many of the small buckets [ss, j], so we don't
                 * have to sort them at all.
                 */
                for (var j = 0; j <= 255; j++)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int sb = (ss << 8) + j;
                    var sb = (ss << 8) + j;
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int ftab_sb = ftab[sb];
                    var ftab_sb = ftab[sb];
                    if ((ftab_sb & SETMASK) != SETMASK)
                    {
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int lo = ftab_sb & CLEARMASK;
                        var lo = ftab_sb & CLEARMASK;
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int hi = (ftab[sb + 1] & CLEARMASK) - 1;
                        var hi = (ftab[sb + 1] & CLEARMASK) - 1;
                        if (hi > lo)
                        {
                            MainQSort3(dataShadow, lo, hi, 2);
                            if (firstAttemptShadow && (workDone > workLimitShadow))
                            {
                                return;
                            }
                        }
                        ftab[sb] = ftab_sb | SETMASK;
                    }
                }

                // Step 2:
                // Now scan this big bucket so as to synthesise the
                // sorted order for small buckets [t, ss] for all t != ss.

                for (var j = 0; j <= 255; j++)
                {
                    copy[j] = ftab[(j << 8) + ss] & CLEARMASK;
                }

                for (int j = ftab[ss << 8] & CLEARMASK,
                         hj = (ftab[(ss + 1) << 8] & CLEARMASK);
                     j < hj;
                     j++)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int fmap_j = fmap[j];
                    var fmap_j = fmap[j];
                    c1 = block[fmap_j] & 0xff;
                    if (!bigDone[c1])
                    {
                        fmap[copy[c1]] = (fmap_j == 0) ? lastShadow : (fmap_j - 1);
                        copy[c1]++;
                    }
                }

                for (var j = 256; --j >= 0;)
                {
                    ftab[(j << 8) + ss] |= SETMASK;
                }

                // Step 3:
                /*
                 * The ss big bucket is now done. Record this fact, and update the
                 * quadrant descriptors. Remember to update quadrants in the
                 * overshoot area too, if necessary. The "if (i < 255)" test merely
                 * skips this updating for the last bucket processed, since updating
                 * for the last bucket is pointless.
                 */
                bigDone[ss] = true;

                if (i < 255)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int bbStart = ftab[ss << 8] & CLEARMASK;
                    var bbStart = ftab[ss << 8] & CLEARMASK;
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int bbSize = (ftab[(ss + 1) << 8] & CLEARMASK) - bbStart;
                    var bbSize = (ftab[(ss + 1) << 8] & CLEARMASK) - bbStart;
                    var shifts = 0;

                    while ((bbSize >> shifts) > 65534)
                    {
                        shifts++;
                    }

                    for (var j = 0; j < bbSize; j++)
                    {
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int a2update = fmap[bbStart + j];
                        var a2update = fmap[bbStart + j];
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final char qVal = (char)(j >> shifts);
                        var qVal = (char) (j >> shifts);
                        quadrant[a2update] = qVal;
                        if (a2update < BZip2Constants_Fields.NUM_OVERSHOOT_BYTES)
                        {
                            quadrant[a2update + lastShadow + 1] = qVal;
                        }
                    }
                }
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @SuppressWarnings("unused") private void randomiseBlock()
        private void RandomiseBlock()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] inUse = this.data.inUse;
            var inUse = data.inUse;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] block = this.data.block;
            var block = data.block;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int lastShadow = this.last;
            var lastShadow = last;

            for (var i = 256; --i >= 0;)
            {
                inUse[i] = false;
            }

            var rNToGo = 0;
            var rTPos = 0;
            for (int i = 0,
                     j = 1;
                 i <= lastShadow;
                 i = j, j++)
            {
                if (rNToGo == 0)
                {
                    rNToGo = (char) BZip2Constants_Fields.rNums[rTPos];
                    if (++rTPos == 512)
                    {
                        rTPos = 0;
                    }
                }

                rNToGo--;
                block[j] ^= ((rNToGo == 1) ? 1 : 0);

                // handle 16 bit signed numbers
                inUse[block[j] & 0xff] = true;
            }

            blockRandomised = true;
        }

        private void GenerateMTFValues()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int lastShadow = this.last;
            var lastShadow = last;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] inUse = dataShadow.inUse;
            var inUse = dataShadow.inUse;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] block = dataShadow.block;
            var block = dataShadow.block;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] fmap = dataShadow.fmap;
            var fmap = dataShadow.fmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[] sfmap = dataShadow.sfmap;
            var sfmap = dataShadow.sfmap;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] mtfFreq = dataShadow.mtfFreq;
            var mtfFreq = dataShadow.mtfFreq;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] unseqToSeq = dataShadow.unseqToSeq;
            var unseqToSeq = dataShadow.unseqToSeq;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] yy = dataShadow.generateMTFValues_yy;
            var yy = dataShadow.generateMTFValues_yy;

            // make maps
            var nInUseShadow = 0;
            for (var i = 0; i < 256; i++)
            {
                if (inUse[i])
                {
                    unseqToSeq[i] = (byte) nInUseShadow;
                    nInUseShadow++;
                }
            }
            nInUse = nInUseShadow;

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int eob = nInUseShadow + 1;
            var eob = nInUseShadow + 1;

            for (var i = eob; i >= 0; i--)
            {
                mtfFreq[i] = 0;
            }

            for (var i = nInUseShadow; --i >= 0;)
            {
                yy[i] = (byte) i;
            }

            var wr = 0;
            var zPend = 0;

            for (var i = 0; i <= lastShadow; i++)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte ll_i = unseqToSeq[block[fmap[i]] & 0xff];
                var ll_i = unseqToSeq[block[fmap[i]] & 0xff];
                var tmp = yy[0];
                var j = 0;

                while (ll_i != tmp)
                {
                    j++;
                    var tmp2 = tmp;
                    tmp = yy[j];
                    yy[j] = tmp2;
                }
                yy[0] = tmp;

                if (j == 0)
                {
                    zPend++;
                }
                else
                {
                    if (zPend > 0)
                    {
                        zPend--;
                        while (true)
                        {
                            if ((zPend & 1) == 0)
                            {
                                sfmap[wr] = (char) BZip2Constants_Fields.RUNA;
                                wr++;
                                mtfFreq[BZip2Constants_Fields.RUNA]++;
                            }
                            else
                            {
                                sfmap[wr] = (char) BZip2Constants_Fields.RUNB;
                                wr++;
                                mtfFreq[BZip2Constants_Fields.RUNB]++;
                            }

                            if (zPend >= 2)
                            {
                                zPend = (zPend - 2) >> 1;
                            }
                            else
                            {
                                break;
                            }
                        }
                        zPend = 0;
                    }
                    sfmap[wr] = (char) (j + 1);
                    wr++;
                    mtfFreq[j + 1]++;
                }
            }

            if (zPend > 0)
            {
                zPend--;
                while (true)
                {
                    if ((zPend & 1) == 0)
                    {
                        sfmap[wr] = (char) BZip2Constants_Fields.RUNA;
                        wr++;
                        mtfFreq[BZip2Constants_Fields.RUNA]++;
                    }
                    else
                    {
                        sfmap[wr] = (char) BZip2Constants_Fields.RUNB;
                        wr++;
                        mtfFreq[BZip2Constants_Fields.RUNB]++;
                    }

                    if (zPend >= 2)
                    {
                        zPend = (zPend - 2) >> 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            sfmap[wr] = (char) eob;
            mtfFreq[eob]++;
            nMTF = wr + 1;
        }

        private sealed class Data : object
        {
            // with blockSize 900k
            internal readonly byte[] block; // 900021 byte

            internal readonly int[] fmap; // 3600000 byte

            internal readonly int[] ftab = new int[65537]; // 262148 byte

            internal readonly byte[] generateMTFValues_yy = new byte[256]; // 256 byte

            internal readonly int[] heap = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE + 2]; // 1040 byte

            internal readonly bool[] inUse = new bool[256]; // 256 byte

            internal readonly bool[] mainSort_bigDone = new bool[256]; // 256 byte

            internal readonly int[] mainSort_copy = new int[256]; // 1024 byte

            internal readonly int[] mainSort_runningOrder = new int[256]; // 1024 byte

            internal readonly int[] mtfFreq = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE]; // 1032 byte

            internal readonly int[] parent = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2]; // 2064 byte

            /// <summary>
            ///     Array instance identical to sfmap, both are used only
            ///     temporarily and indepently, so we do not need to allocate
            ///     additional memory.
            /// </summary>
            internal readonly char[] quadrant;

            internal readonly byte[] selector = new byte[BZip2Constants_Fields.MAX_SELECTORS]; // 18002 byte

            internal readonly byte[] selectorMtf = new byte[BZip2Constants_Fields.MAX_SELECTORS]; // 18002 byte

            internal readonly byte[] sendMTFValues2_pos = new byte[BZip2Constants_Fields.N_GROUPS]; // 6 byte

            internal readonly int[][] sendMTFValues_code = RectangularArrays.ReturnRectangularIntArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); // 6192

            internal readonly short[] sendMTFValues_cost = new short[BZip2Constants_Fields.N_GROUPS]; // 12 byte

            internal readonly int[] sendMTFValues_fave = new int[BZip2Constants_Fields.N_GROUPS]; // 24 byte

            //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: internal readonly byte[][] sendMTFValues_len = new byte[BZip2Constants_Fields.N_GROUPS][BZip2Constants_Fields.MAX_ALPHA_SIZE]; // 1548
            internal readonly byte[][] sendMTFValues_len = RectangularArrays.ReturnRectangularbyteArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); // 1548

            // byte
            //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: internal readonly int[][] sendMTFValues_rfreq = new int[BZip2Constants_Fields.N_GROUPS][BZip2Constants_Fields.MAX_ALPHA_SIZE]; // 6192
            internal readonly int[][] sendMTFValues_rfreq = RectangularArrays.ReturnRectangularIntArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); // 6192

            // byte

            internal readonly bool[] sentMTFValues4_inUse16 = new bool[16]; // 16 byte

            internal readonly char[] sfmap; // 3600000 byte

            internal readonly int[] stack_dd = new int[QSORT_STACK_SIZE]; // 4000 byte

            internal readonly int[] stack_hh = new int[QSORT_STACK_SIZE]; // 4000 byte

            internal readonly int[] stack_ll = new int[QSORT_STACK_SIZE]; // 4000 byte

            internal readonly byte[] unseqToSeq = new byte[256]; // 256 byte

            internal readonly int[] weight = new int[BZip2Constants_Fields.MAX_ALPHA_SIZE * 2]; // 2064 byte

            internal Data(int blockSize100k)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int n = blockSize100k * BZip2Constants_Fields.baseBlockSize;
                var n = blockSize100k * BZip2Constants_Fields.baseBlockSize;
                block = new byte[(n + 1 + BZip2Constants_Fields.NUM_OVERSHOOT_BYTES)];
                fmap = new int[n];
                sfmap = new char[2 * n];
                quadrant = sfmap;
            }
        }

        public override void write(int i)
        {
            @out.write(i);
        }
    }
}
