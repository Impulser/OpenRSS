using System;

using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

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
    ///     An input stream that decompresses from the BZip2 format (without the file
    ///     header chars) to be read as any other stream.
    ///     <p>
    ///         The decompression requires large amounts of memory. Thus you
    ///         should call the <seealso cref="#close() close()" /> method as soon as
    ///         possible, to force <tt>CBZip2InputStream</tt> to release the
    ///         allocated memory.  See {@link CBZip2OutputStream
    ///         CBZip2OutputStream} for information about memory usage.
    ///     </p>
    ///     <p>
    ///         <tt>CBZip2InputStream</tt> reads bytes from the compressed
    ///         source stream via the single byte {@link java.io.InputStream#read()
    ///         read()} method exclusively. Thus you should consider to use a
    ///         buffered source stream.
    ///     </p>
    ///     <p>Instances of this class are not threadsafe.</p>
    /// </summary>
    public class CBZip2InputStream : InputStream, BZip2Constants
    {
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private static void reportCRCError() throws java.io.IOException

        private const int EOF = 0;

        private const int START_BLOCK_STATE = 1;

        private const int RAND_PART_A_STATE = 2;

        private const int RAND_PART_B_STATE = 3;

        private const int RAND_PART_C_STATE = 4;

        private const int NO_RAND_PART_A_STATE = 5;

        private const int NO_RAND_PART_B_STATE = 6;

        private const int NO_RAND_PART_C_STATE = 7;

        private readonly CRC crc = new CRC();

        private bool blockRandomised;

        /// <summary>
        ///     always: in the range 0 .. 9.
        ///     The current block size is 100000 * this number.
        /// </summary>
        private int blockSize100k;

        private int bsBuff;

        private int bsLive;

        private int computedBlockCRC,
                    computedCombinedCRC;

        private int currentChar = -1;

        private int currentState = START_BLOCK_STATE;

        /// <summary>
        ///     All memory intensive stuff.
        ///     This field is initialized by initBlock().
        /// </summary>
        private Data data;

        private InputStream @in;

        /// <summary>
        ///     Index of the last char in the block, so the block size == last + 1.
        /// </summary>
        private int last;

        private int nInUse;

        /// <summary>
        ///     Index in zptr[] of original string after sorting.
        /// </summary>
        private int origPtr;

        private int storedBlockCRC,
                    storedCombinedCRC;

        private int su_ch2;

        private int su_chPrev;

        private int su_count;

        private int su_i2;

        private int su_j2;

        private int su_rNToGo;

        private int su_rTPos;

        private int su_tPos;

        private char su_z;

        /// <summary>
        ///     Constructs a new CBZip2InputStream which decompresses bytes read from
        ///     the specified stream.
        ///     <p>
        ///         Although BZip2 headers are marked with the magic
        ///         <tt>"Bz"</tt> this constructor expects the next byte in the
        ///         stream to be the first one after the magic.  Thus callers have
        ///         to skip the first two bytes. Otherwise this constructor will
        ///         throw an exception.
        ///     </p>
        /// </summary>
        /// <exception cref="IOException">
        ///     if the stream content is malformed or an I/O error occurs.
        /// </exception>
        /// <exception cref="NullPointerException">
        ///     if <tt>in == null</tt>
        /// </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public CBZip2InputStream(final java.io.InputStream in) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        public CBZip2InputStream(InputStream @in)
        {
            this.@in = @in;
            Init();
        }

        private static void ReportCRCError()
        {
            // The clean way would be to throw an exception.
            throw new IOException("crc error");

            // Just print a message, like the previous versions of this class did
            //System.err.println("BZip2 CRC error");
        }

        private void MakeMaps()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] inUse = this.data.inUse;
            var inUse = data.inUse;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] seqToUnseq = this.data.seqToUnseq;
            var seqToUnseq = data.seqToUnseq;

            var nInUseShadow = 0;

            for (var i = 0; i < 256; i++)
            {
                if (inUse[i])
                {
                    seqToUnseq[nInUseShadow++] = (byte) i;
                }
            }

            nInUse = nInUseShadow;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int read() throws java.io.IOException
        public virtual int Read()
        {
            if (@in != null)
            {
                return Read0();
            }
            throw new IOException("stream closed");
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int read(final byte[] dest, final int offs, final int len) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        public virtual int Read(byte[] dest, int offs, int len)
        {
            if (offs < 0)
            {
                throw new IndexOutOfRangeException("offs(" + offs + ") < 0.");
            }
            if (len < 0)
            {
                throw new IndexOutOfRangeException("len(" + len + ") < 0.");
            }
            if (offs + len > dest.Length)
            {
                throw new IndexOutOfRangeException("offs(" + offs + ") + len(" + len + ") > dest.length(" + dest.Length + ").");
            }
            if (@in == null)
            {
                throw new IOException("stream closed");
            }

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int hi = offs + len;
            var hi = offs + len;
            var destOffs = offs;
            for (int b; (destOffs < hi) && ((b = Read0()) >= 0);)
            {
                dest[destOffs++] = (byte) b;
            }

            return (destOffs == offs) ? - 1 : (destOffs - offs);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private int read0() throws java.io.IOException
        private int Read0()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int retChar = this.currentChar;
            var retChar = currentChar;

            switch (currentState)
            {
                case EOF:
                    return -1;

                case START_BLOCK_STATE:
                    throw new IllegalStateException();

                case RAND_PART_A_STATE:
                    throw new IllegalStateException();

                case RAND_PART_B_STATE:
                    SetupRandPartB();
                    break;

                case RAND_PART_C_STATE:
                    SetupRandPartC();
                    break;

                case NO_RAND_PART_A_STATE:
                    throw new IllegalStateException();

                case NO_RAND_PART_B_STATE:
                    SetupNoRandPartB();
                    break;

                case NO_RAND_PART_C_STATE:
                    SetupNoRandPartC();
                    break;

                default:
                    throw new IllegalStateException();
            }

            return retChar;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void init() throws java.io.IOException
        private void Init()
        {
            if (null == @in)
            {
                throw new IOException("No InputStream");
            }
            if (@in.available() == 0)
            {
                throw new IOException("Empty InputStream");
            }
            int magic2 = @in.read();
            if (magic2 != 'h')
            {
                throw new IOException("Stream is not BZip2 formatted: expected 'h'" + " as first byte but got '" + (char) magic2 + "'");
            }

            int blockSize = @in.read();
            if ((blockSize < '1') || (blockSize > '9'))
            {
                throw new IOException("Stream is not BZip2 formatted: illegal " + "blocksize " + (char) blockSize);
            }

            blockSize100k = blockSize - '0';

            InitBlock();
            SetupBlock();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void initBlock() throws java.io.IOException
        private void InitBlock()
        {
            var magic0 = BsGetUByte();
            var magic1 = BsGetUByte();
            var magic2 = BsGetUByte();
            var magic3 = BsGetUByte();
            var magic4 = BsGetUByte();
            var magic5 = BsGetUByte();

            if (magic0 == 0x17 && magic1 == 0x72 && magic2 == 0x45 && magic3 == 0x38 && magic4 == 0x50 && magic5 == 0x90)
            {
                Complete(); // end of file
            } // '1'
            else if (magic0 != 0x31 || magic1 != 0x41 || magic2 != 0x59 || magic3 != 0x26 || magic4 != 0x53 || magic5 != 0x59) // 'Y' -  'S' -  '&' -  'Y' -  ')'
            {
                currentState = EOF;
                throw new IOException("bad block header");
            }
            else
            {
                storedBlockCRC = BsGetInt();
                blockRandomised = BsR(1) == 1;

                /// <summary>
                /// Allocate data here instead in constructor, so we do not
                /// allocate it if the input file is empty.
                /// </summary>
                if (data == null)
                {
                    data = new Data(blockSize100k);
                }

                // currBlockNo++;
                GetAndMoveToFrontDecode();

                crc.InitialiseCRC();
                currentState = START_BLOCK_STATE;
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void endBlock() throws java.io.IOException
        private void EndBlock()
        {
            computedBlockCRC = crc.GetFinalCRC();

            // A bad CRC is considered a fatal error.
            if (storedBlockCRC != computedBlockCRC)
            {
                // make next blocks readable without error
                // (repair feature, not yet documented, not tested)
                computedCombinedCRC = (storedCombinedCRC << 1) | ((int) ((uint) storedCombinedCRC >> 31));
                computedCombinedCRC ^= storedBlockCRC;

                ReportCRCError();
            }

            computedCombinedCRC = (computedCombinedCRC << 1) | ((int) ((uint) computedCombinedCRC >> 31));
            computedCombinedCRC ^= computedBlockCRC;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void complete() throws java.io.IOException
        private void Complete()
        {
            storedCombinedCRC = BsGetInt();
            currentState = EOF;
            data = null;

            if (storedCombinedCRC != computedCombinedCRC)
            {
                ReportCRCError();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void close() throws java.io.IOException
        public virtual void Close()
        {
            var inShadow = @in;
            if (inShadow != null)
            {
                try
                {
                    if (inShadow != java.lang.System.@in)
                    {
                        inShadow.close();
                    }
                }
                finally
                {
                    data = null;
                    @in = null;
                }
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private int bsR(final int n) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private int BsR(int n)
        {
            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            if (bsLiveShadow < n)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final java.io.InputStream inShadow = this.in;
                var inShadow = @in;
                do
                {
                    int thech = inShadow.read();

                    if (thech < 0)
                    {
                        throw new IOException("unexpected end of stream");
                    }

                    bsBuffShadow = (bsBuffShadow << 8) | thech;
                    bsLiveShadow += 8;
                }
                while (bsLiveShadow < n);

                bsBuff = bsBuffShadow;
            }

            bsLive = bsLiveShadow - n;
            return (bsBuffShadow >> (bsLiveShadow - n)) & ((1 << n) - 1);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private boolean bsGetBit() throws java.io.IOException
        private bool BsGetBit()
        {
            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            if (bsLiveShadow < 1)
            {
                int thech = @in.read();

                if (thech < 0)
                {
                    throw new IOException("unexpected end of stream");
                }

                bsBuffShadow = (bsBuffShadow << 8) | thech;
                bsLiveShadow += 8;
                bsBuff = bsBuffShadow;
            }

            bsLive = bsLiveShadow - 1;
            return ((bsBuffShadow >> (bsLiveShadow - 1)) & 1) != 0;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private char bsGetUByte() throws java.io.IOException
        private char BsGetUByte()
        {
            return (char) BsR(8);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private int bsGetInt() throws java.io.IOException
        private int BsGetInt()
        {
            return (((((BsR(8) << 8) | BsR(8)) << 8) | BsR(8)) << 8) | BsR(8);
        }

        /// <summary>
        ///     Called by createHuffmanDecodingTables() exclusively.
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private static void hbCreateDecodeTables(final int[] limit, final int[] base, final int[] perm, final char[] length, final int minLen, final int maxLen, final int alphaSize)
        private static void HbCreateDecodeTables(int[] limit, int[] @base, int[] perm, char[] length, int minLen, int maxLen, int alphaSize)
        {
            for (int i = minLen,
                     pp = 0;
                 i <= maxLen;
                 i++)
            {
                for (var j = 0; j < alphaSize; j++)
                {
                    if (length[j] == i)
                    {
                        perm[pp++] = j;
                    }
                }
            }

            for (var i = BZip2Constants_Fields.MAX_CODE_LEN; --i > 0;)
            {
                @base[i] = 0;
                limit[i] = 0;
            }

            for (var i = 0; i < alphaSize; i++)
            {
                @base[length[i] + 1]++;
            }

            for (int i = 1,
                     b = @base[0];
                 i < BZip2Constants_Fields.MAX_CODE_LEN;
                 i++)
            {
                b += @base[i];
                @base[i] = b;
            }

            for (int i = minLen,
                     vec = 0,
                     b = @base[i];
                 i <= maxLen;
                 i++)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int nb = base[i + 1];
                var nb = @base[i + 1];
                vec += nb - b;
                b = nb;
                limit[i] = vec - 1;
                vec <<= 1;
            }

            for (var i = minLen + 1; i <= maxLen; i++)
            {
                @base[i] = ((limit[i - 1] + 1) << 1) - @base[i];
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void recvDecodingTables() throws java.io.IOException
        private void RecvDecodingTables()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final boolean[] inUse = dataShadow.inUse;
            var inUse = dataShadow.inUse;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] pos = dataShadow.recvDecodingTables_pos;
            var pos = dataShadow.recvDecodingTables_pos;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] selector = dataShadow.selector;
            var selector = dataShadow.selector;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] selectorMtf = dataShadow.selectorMtf;
            var selectorMtf = dataShadow.selectorMtf;

            var inUse16 = 0;

            /* Receive the mapping table */
            for (var i = 0; i < 16; i++)
            {
                if (BsGetBit())
                {
                    inUse16 |= 1 << i;
                }
            }

            for (var i = 256; --i >= 0;)
            {
                inUse[i] = false;
            }

            for (var i = 0; i < 16; i++)
            {
                if ((inUse16 & (1 << i)) != 0)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int i16 = i << 4;
                    var i16 = i << 4;
                    for (var j = 0; j < 16; j++)
                    {
                        if (BsGetBit())
                        {
                            inUse[i16 + j] = true;
                        }
                    }
                }
            }

            MakeMaps();
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int alphaSize = this.nInUse + 2;
            var alphaSize = nInUse + 2;

            /* Now the selectors */
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int nGroups = bsR(3);
            var nGroups = BsR(3);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int nSelectors = bsR(15);
            var nSelectors = BsR(15);

            for (var i = 0; i < nSelectors; i++)
            {
                var j = 0;
                while (BsGetBit())
                {
                    j++;
                }
                selectorMtf[i] = (byte) j;
            }

            /* Undo the MTF values for the selectors. */
            for (var v = nGroups; --v >= 0;)
            {
                pos[v] = (byte) v;
            }

            for (var i = 0; i < nSelectors; i++)
            {
                var v = selectorMtf[i] & 0xff;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final byte tmp = pos[v];
                var tmp = pos[v];
                while (v > 0)
                {
                    // nearly all times v is zero, 4 in most other cases
                    pos[v] = pos[v - 1];
                    v--;
                }
                pos[0] = tmp;
                selector[i] = tmp;
            }

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[][] len = dataShadow.temp_charArray2d;
            var len = dataShadow.temp_charArray2d;

            /* Now the coding tables */
            for (var t = 0; t < nGroups; t++)
            {
                var curr = BsR(5);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final char[] len_t = len[t];
                var len_t = len[t];
                for (var i = 0; i < alphaSize; i++)
                {
                    while (BsGetBit())
                    {
                        curr += BsGetBit() ? - 1 : 1;
                    }
                    len_t[i] = (char) curr;
                }
            }

            // finally create the Huffman tables
            CreateHuffmanDecodingTables(alphaSize, nGroups);
        }

        /// <summary>
        ///     Called by recvDecodingTables() exclusively.
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: private void createHuffmanDecodingTables(final int alphaSize, final int nGroups)
        private void CreateHuffmanDecodingTables(int alphaSize, int nGroups)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[][] len = dataShadow.temp_charArray2d;
            var len = dataShadow.temp_charArray2d;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] minLens = dataShadow.minLens;
            var minLens = dataShadow.minLens;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] limit = dataShadow.limit;
            var limit = dataShadow.limit;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] base = dataShadow.base;
            var @base = dataShadow.@base;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] perm = dataShadow.perm;
            var perm = dataShadow.perm;

            for (var t = 0; t < nGroups; t++)
            {
                var minLen = 32;
                var maxLen = 0;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final char[] len_t = len[t];
                var len_t = len[t];
                for (var i = alphaSize; --i >= 0;)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final char lent = len_t[i];
                    var lent = len_t[i];
                    if (lent > maxLen)
                    {
                        maxLen = lent;
                    }
                    if (lent < minLen)
                    {
                        minLen = lent;
                    }
                }
                HbCreateDecodeTables(limit[t], @base[t], perm[t], len[t], minLen, maxLen, alphaSize);
                minLens[t] = minLen;
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void getAndMoveToFrontDecode() throws java.io.IOException
        private void GetAndMoveToFrontDecode()
        {
            origPtr = BsR(24);
            RecvDecodingTables();

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.InputStream inShadow = this.in;
            var inShadow = @in;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] ll8 = dataShadow.ll8;
            var ll8 = dataShadow.ll8;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] unzftab = dataShadow.unzftab;
            var unzftab = dataShadow.unzftab;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] selector = dataShadow.selector;
            var selector = dataShadow.selector;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] seqToUnseq = dataShadow.seqToUnseq;
            var seqToUnseq = dataShadow.seqToUnseq;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final char[] yy = dataShadow.getAndMoveToFrontDecode_yy;
            var yy = dataShadow.getAndMoveToFrontDecode_yy;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] minLens = dataShadow.minLens;
            var minLens = dataShadow.minLens;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] limit = dataShadow.limit;
            var limit = dataShadow.limit;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] base = dataShadow.base;
            var @base = dataShadow.@base;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[][] perm = dataShadow.perm;
            var perm = dataShadow.perm;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int limitLast = this.blockSize100k * 100000;
            var limitLast = blockSize100k * 100000;

            /*
              Setting up the unzftab entries here is not strictly
              necessary, but it does save having to do it later
              in a separate pass, and so saves a block's worth of
              cache misses.
            */
            for (var i = 256; --i >= 0;)
            {
                yy[i] = (char) i;
                unzftab[i] = 0;
            }

            var groupNo = 0;
            var groupPos = BZip2Constants_Fields.G_SIZE - 1;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int eob = this.nInUse + 1;
            var eob = nInUse + 1;
            var nextSym = GetAndMoveToFrontDecode0(0);
            var bsBuffShadow = bsBuff;
            var bsLiveShadow = bsLive;
            var lastShadow = -1;
            var zt = selector[groupNo] & 0xff;
            var base_zt = @base[zt];
            var limit_zt = limit[zt];
            var perm_zt = perm[zt];
            var minLens_zt = minLens[zt];

            while (nextSym != eob)
            {
                if ((nextSym == BZip2Constants_Fields.RUNA) || (nextSym == BZip2Constants_Fields.RUNB))
                {
                    var s = -1;

                    for (var n = 1;; n <<= 1)
                    {
                        if (nextSym == BZip2Constants_Fields.RUNA)
                        {
                            s += n;
                        }
                        else if (nextSym == BZip2Constants_Fields.RUNB)
                        {
                            s += n << 1;
                        }
                        else
                        {
                            break;
                        }

                        if (groupPos == 0)
                        {
                            groupPos = BZip2Constants_Fields.G_SIZE - 1;
                            zt = selector[++groupNo] & 0xff;
                            base_zt = @base[zt];
                            limit_zt = limit[zt];
                            perm_zt = perm[zt];
                            minLens_zt = minLens[zt];
                        }
                        else
                        {
                            groupPos--;
                        }

                        var zn = minLens_zt;

                        // Inlined:
                        // int zvec = bsR(zn);
                        while (bsLiveShadow < zn)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int thech = inShadow.read();
                            int thech = inShadow.read();
                            if (thech >= 0)
                            {
                                bsBuffShadow = (bsBuffShadow << 8) | thech;
                                bsLiveShadow += 8;
                            }
                            throw new IOException("unexpected end of stream");
                        }
                        var zvec = (bsBuffShadow >> (bsLiveShadow - zn)) & ((1 << zn) - 1);
                        bsLiveShadow -= zn;

                        while (zvec > limit_zt[zn])
                        {
                            zn++;
                            while (bsLiveShadow < 1)
                            {
                                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                                //ORIGINAL LINE: final int thech = inShadow.read();
                                int thech = inShadow.read();
                                if (thech >= 0)
                                {
                                    bsBuffShadow = (bsBuffShadow << 8) | thech;
                                    bsLiveShadow += 8;
                                }
                                throw new IOException("unexpected end of stream");
                            }
                            bsLiveShadow--;
                            zvec = (zvec << 1) | ((bsBuffShadow >> bsLiveShadow) & 1);
                        }
                        nextSym = perm_zt[zvec - base_zt[zn]];
                    }

                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final byte ch = seqToUnseq[yy[0]];
                    var ch = seqToUnseq[yy[0]];
                    unzftab[ch & 0xff] += s + 1;

                    while (s-- >= 0)
                    {
                        ll8[++lastShadow] = ch;
                    }

                    if (lastShadow >= limitLast)
                    {
                        throw new IOException("block overrun");
                    }
                }
                else
                {
                    if (++lastShadow >= limitLast)
                    {
                        throw new IOException("block overrun");
                    }

                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final char tmp = yy[nextSym - 1];
                    var tmp = yy[nextSym - 1];
                    unzftab[seqToUnseq[tmp] & 0xff]++;
                    ll8[lastShadow] = seqToUnseq[tmp];

                    /*
                      This loop is hammered during decompression,
                      hence avoid native method call overhead of
                      System.arraycopy for very small ranges to copy.
                    */
                    if (nextSym <= 16)
                    {
                        for (var j = nextSym - 1; j > 0;)
                        {
                            yy[j] = yy[--j];
                        }
                    }
                    else
                    {
                        Array.Copy(yy, 0, yy, 1, nextSym - 1);
                    }

                    yy[0] = tmp;

                    if (groupPos == 0)
                    {
                        groupPos = BZip2Constants_Fields.G_SIZE - 1;
                        zt = selector[++groupNo] & 0xff;
                        base_zt = @base[zt];
                        limit_zt = limit[zt];
                        perm_zt = perm[zt];
                        minLens_zt = minLens[zt];
                    }
                    else
                    {
                        groupPos--;
                    }

                    var zn = minLens_zt;

                    // Inlined:
                    // int zvec = bsR(zn);
                    while (bsLiveShadow < zn)
                    {
                        //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                        //ORIGINAL LINE: final int thech = inShadow.read();
                        int thech = inShadow.read();
                        if (thech >= 0)
                        {
                            bsBuffShadow = (bsBuffShadow << 8) | thech;
                            bsLiveShadow += 8;
                        }
                        throw new IOException("unexpected end of stream");
                    }
                    var zvec = (bsBuffShadow >> (bsLiveShadow - zn)) & ((1 << zn) - 1);
                    bsLiveShadow -= zn;

                    while (zvec > limit_zt[zn])
                    {
                        zn++;
                        while (bsLiveShadow < 1)
                        {
                            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                            //ORIGINAL LINE: final int thech = inShadow.read();
                            int thech = inShadow.read();
                            if (thech >= 0)
                            {
                                bsBuffShadow = (bsBuffShadow << 8) | thech;
                                bsLiveShadow += 8;
                            }
                            throw new IOException("unexpected end of stream");
                        }
                        bsLiveShadow--;
                        zvec = (zvec << 1) | ((bsBuffShadow >> bsLiveShadow) & 1);
                    }
                    nextSym = perm_zt[zvec - base_zt[zn]];
                }
            }

            last = lastShadow;
            bsLive = bsLiveShadow;
            bsBuff = bsBuffShadow;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private int getAndMoveToFrontDecode0(final int groupNo) throws java.io.IOException
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        private int GetAndMoveToFrontDecode0(int groupNo)
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.io.InputStream inShadow = this.in;
            var inShadow = @in;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Data dataShadow = this.data;
            var dataShadow = data;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int zt = dataShadow.selector[groupNo] & 0xff;
            var zt = dataShadow.selector[groupNo] & 0xff;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] limit_zt = dataShadow.limit[zt];
            var limit_zt = dataShadow.limit[zt];
            var zn = dataShadow.minLens[zt];
            var zvec = BsR(zn);
            var bsLiveShadow = bsLive;
            var bsBuffShadow = bsBuff;

            while (zvec > limit_zt[zn])
            {
                zn++;
                while (bsLiveShadow < 1)
                {
                    //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                    //ORIGINAL LINE: final int thech = inShadow.read();
                    int thech = inShadow.read();

                    if (thech >= 0)
                    {
                        bsBuffShadow = (bsBuffShadow << 8) | thech;
                        bsLiveShadow += 8;
                    }
                    throw new IOException("unexpected end of stream");
                }
                bsLiveShadow--;
                zvec = (zvec << 1) | ((bsBuffShadow >> bsLiveShadow) & 1);
            }

            bsLive = bsLiveShadow;
            bsBuff = bsBuffShadow;

            return dataShadow.perm[zt][zvec - dataShadow.@base[zt][zn]];
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupBlock() throws java.io.IOException
        private void SetupBlock()
        {
            if (data == null)
            {
                return;
            }

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] cftab = this.data.cftab;
            var cftab = data.cftab;
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final int[] tt = this.data.initTT(this.last + 1);
            var tt = data.InitTT(last + 1);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final byte[] ll8 = this.data.ll8;
            var ll8 = data.ll8;
            cftab[0] = 0;
            Array.Copy(data.unzftab, 0, cftab, 1, 256);

            for (int i = 1,
                     c = cftab[0];
                 i <= 256;
                 i++)
            {
                c += cftab[i];
                cftab[i] = c;
            }

            for (int i = 0,
                     lastShadow = last;
                 i <= lastShadow;
                 i++)
            {
                tt[cftab[ll8[i] & 0xff]++] = i;
            }

            if ((origPtr < 0) || (origPtr >= tt.Length))
            {
                throw new IOException("stream corrupted");
            }

            su_tPos = tt[origPtr];
            su_count = 0;
            su_i2 = 0;
            su_ch2 = 256; // not a char and not EOF

            if (blockRandomised)
            {
                su_rNToGo = 0;
                su_rTPos = 0;
                SetupRandPartA();
            }
            else
            {
                SetupNoRandPartA();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupRandPartA() throws java.io.IOException
        private void SetupRandPartA()
        {
            if (su_i2 <= last)
            {
                su_chPrev = su_ch2;
                var su_ch2Shadow = data.ll8[su_tPos] & 0xff;
                su_tPos = data.tt[su_tPos];
                if (su_rNToGo == 0)
                {
                    su_rNToGo = BZip2Constants_Fields.rNums[su_rTPos] - 1;
                    if (++su_rTPos == 512)
                    {
                        su_rTPos = 0;
                    }
                }
                else
                {
                    su_rNToGo--;
                }
                su_ch2 = su_ch2Shadow ^= (su_rNToGo == 1) ? 1 : 0;
                su_i2++;
                currentChar = su_ch2Shadow;
                currentState = RAND_PART_B_STATE;
                crc.UpdateCRC(su_ch2Shadow);
            }
            else
            {
                EndBlock();
                InitBlock();
                SetupBlock();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupNoRandPartA() throws java.io.IOException
        private void SetupNoRandPartA()
        {
            if (su_i2 <= last)
            {
                su_chPrev = su_ch2;
                var su_ch2Shadow = data.ll8[su_tPos] & 0xff;
                su_ch2 = su_ch2Shadow;
                su_tPos = data.tt[su_tPos];
                su_i2++;
                currentChar = su_ch2Shadow;
                currentState = NO_RAND_PART_B_STATE;
                crc.UpdateCRC(su_ch2Shadow);
            }
            else
            {
                currentState = NO_RAND_PART_A_STATE;
                EndBlock();
                InitBlock();
                SetupBlock();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupRandPartB() throws java.io.IOException
        private void SetupRandPartB()
        {
            if (su_ch2 != su_chPrev)
            {
                currentState = RAND_PART_A_STATE;
                su_count = 1;
                SetupRandPartA();
            }
            else if (++su_count >= 4)
            {
                su_z = (char) (data.ll8[su_tPos] & 0xff);
                su_tPos = data.tt[su_tPos];
                if (su_rNToGo == 0)
                {
                    su_rNToGo = BZip2Constants_Fields.rNums[su_rTPos] - 1;
                    if (++su_rTPos == 512)
                    {
                        su_rTPos = 0;
                    }
                }
                else
                {
                    su_rNToGo--;
                }
                su_j2 = 0;
                currentState = RAND_PART_C_STATE;
                if (su_rNToGo == 1)
                {
                    su_z ^= 1;
                }
                SetupRandPartC();
            }
            else
            {
                currentState = RAND_PART_A_STATE;
                SetupRandPartA();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupRandPartC() throws java.io.IOException
        private void SetupRandPartC()
        {
            if (su_j2 < su_z)
            {
                currentChar = su_ch2;
                crc.UpdateCRC(su_ch2);
                su_j2++;
            }
            else
            {
                currentState = RAND_PART_A_STATE;
                su_i2++;
                su_count = 0;
                SetupRandPartA();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupNoRandPartB() throws java.io.IOException
        private void SetupNoRandPartB()
        {
            if (su_ch2 != su_chPrev)
            {
                su_count = 1;
                SetupNoRandPartA();
            }
            else if (++su_count >= 4)
            {
                su_z = (char) (data.ll8[su_tPos] & 0xff);
                su_tPos = data.tt[su_tPos];
                su_j2 = 0;
                SetupNoRandPartC();
            }
            else
            {
                SetupNoRandPartA();
            }
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void setupNoRandPartC() throws java.io.IOException
        private void SetupNoRandPartC()
        {
            if (su_j2 < su_z)
            {
                var su_ch2Shadow = su_ch2;
                currentChar = su_ch2Shadow;
                crc.UpdateCRC(su_ch2Shadow);
                su_j2++;
                currentState = NO_RAND_PART_C_STATE;
            }
            else
            {
                su_i2++;
                su_count = 0;
                SetupNoRandPartA();
            }
        }

        private sealed class Data : object
        {
            // (with blockSize 900k)

            //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: internal readonly int[][] base = new int[BZip2Constants_Fields.N_GROUPS][BZip2Constants_Fields.MAX_ALPHA_SIZE]; //     6192 byte
            internal readonly int[][] @base = RectangularArrays.ReturnRectangularIntArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); //     6192 byte

            //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: internal readonly int[][] perm = new int[BZip2Constants_Fields.N_GROUPS][BZip2Constants_Fields.MAX_ALPHA_SIZE]; //     6192 byte

            internal readonly int[] cftab = new int[257]; //     1028 byte

            internal readonly char[] getAndMoveToFrontDecode_yy = new char[256]; //      512 byte

            internal readonly bool[] inUse = new bool[256]; //      256 byte

            internal readonly int[][] limit = RectangularArrays.ReturnRectangularIntArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); //     6192 byte

            internal readonly int[] minLens = new int[BZip2Constants_Fields.N_GROUPS]; //       24 byte

            internal readonly int[][] perm = RectangularArrays.ReturnRectangularIntArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); //     6192 byte

            //JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: internal readonly char[][] temp_charArray2d = new char[BZip2Constants_Fields.N_GROUPS][BZip2Constants_Fields.MAX_ALPHA_SIZE]; //     3096 byte

            internal readonly byte[] recvDecodingTables_pos = new byte[BZip2Constants_Fields.N_GROUPS]; //        6 byte

            internal readonly byte[] selector = new byte[BZip2Constants_Fields.MAX_SELECTORS]; //    18002 byte

            internal readonly byte[] selectorMtf = new byte[BZip2Constants_Fields.MAX_SELECTORS]; //    18002 byte

            internal readonly byte[] seqToUnseq = new byte[256]; //      256 byte

            internal readonly char[][] temp_charArray2d = RectangularArrays.ReturnRectangularCharArray(BZip2Constants_Fields.N_GROUPS, BZip2Constants_Fields.MAX_ALPHA_SIZE); //     3096 byte

            /// <summary>
            ///     Freq table collected to save a pass over the data during
            ///     decompression.
            /// </summary>
            internal readonly int[] unzftab = new int[256]; //     1024 byte

            //---------------
            //    60798 byte

            internal byte[] ll8; //   900000 byte

            internal int[] tt; //  3600000 byte

            //---------------
            //  4560782 byte
            //===============

            internal Data(int blockSize100k)
            {
                ll8 = new byte[blockSize100k * BZip2Constants_Fields.baseBlockSize];
            }

            /// <summary>
            ///     Initializes the <seealso cref="#tt" /> array.
            ///     This method is called when the required length of the array
            ///     is known.  I don't initialize it at construction time to
            ///     avoid unneccessary memory allocation when compressing small
            ///     files.
            /// </summary>
            internal int[] InitTT(int length)
            {
                var ttShadow = tt;

                // tt.length should always be >= length, but theoretically
                // it can happen, if the compressor mixed small and large
                // blocks.  Normally only the last block will be smaller
                // than others.
                if ((ttShadow == null) || (ttShadow.Length < length))
                {
                    tt = ttShadow = new int[length];
                }

                return ttShadow;
            }
        }

        public override int read()
        {
            return @in.read();
        }
    }
}
