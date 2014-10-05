using System;

namespace com.io
{

    public class InputStream : Stream
    {
        private static readonly int[] BIT_MASK = {
            0,
            1,
            3,
            7,
            15,
            31,
            63,
            127,
            255,
            511,
            1023,
            2047,
            4095,
            8191,
            16383,
            32767,
            65535,
            131071,
            262143,
            524287,
            1048575,
            2097151,
            4194303,
            8388607,
            16777215,
            33554431,
            67108863,
            134217727,
            268435455,
            536870911,
            1073741823,
            2147483647,
            -1
        };

        public InputStream(int capacity)
        {
            buffer = new byte[capacity];
        }

        public InputStream(byte[] buffer)
        {
            this.buffer = buffer;
            length = buffer.Length;
        }

        public void InitBitAccess()
        {
            bitPosition = offset * 8;
        }

        public void FinishBitAccess()
        {
            offset = (7 + bitPosition) / 8;
        }

        public int ReadBits(int bitOffset)
        {
            var bytePos = bitPosition >> 1779819011;
            var i_8_ = -(0x7 & bitPosition) + 8;
            bitPosition += bitOffset;
            var value = 0;
            for (; (bitOffset ^ 0xffffffff) < (i_8_ ^ 0xffffffff); i_8_ = 8)
            {
                value += (BIT_MASK[i_8_] & buffer[bytePos++]) << -i_8_ + bitOffset;
                bitOffset -= i_8_;
            }
            if ((i_8_ ^ 0xffffffff) == (bitOffset ^ 0xffffffff))
            {
                value += buffer[bytePos] & BIT_MASK[i_8_];
            }
            else
            {
                value += (buffer[bytePos] >> -bitOffset + i_8_ & BIT_MASK[bitOffset]);
            }
            return value;
        }

        public void CheckCapacity(int length)
        {
            if (offset + length >= buffer.Length)
            {
                var newBuffer = new byte[(offset + length) * 2];
                Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
                buffer = newBuffer;
            }
        }

        public void Skip(int length)
        {
            offset += length;
        }

        public void SetLength(int length)
        {
            this.length = length;
        }

        public void SetOffset(int offset)
        {
            this.offset = offset;
        }

        public int GetRemaining()
        {
            return offset < length
                           ? length - offset
                           : 0;
        }

        public void AddBytes(byte[] b, int offset, int length)
        {
            CheckCapacity(length - offset);
            Array.Copy(b, offset, buffer, this.offset, length);
            this.length += length - offset;
        }

        public int ReadPacket()
        {
            return ReadUnsignedByte();
        }

        public int ReadByte()
        {
            return GetRemaining() > 0
                           ? buffer[offset++]
                           : 0;
        }

        public void ReadBytes(byte[] buffer, int off, int len)
        {
            for (var k = off; k < len + off; k++)
            {
                buffer[k] = (byte) ReadByte();
            }
        }

        public void ReadBytes(byte[] buffer)
        {
            ReadBytes(buffer, 0, buffer.Length);
        }

        public int ReadSmart2()
        {
            var i = 0;
            var i_33_ = ReadUnsignedSmart();
            while ((i_33_ ^ 0xffffffff) == -32768)
            {
                i_33_ = ReadUnsignedSmart();
                i += 32767;
            }
            i += i_33_;
            return i;
        }

        public int ReadUnsignedByte()
        {
            return ReadByte() & 0xff;
        }

        public int ReadByte128()
        {
            return unchecked((byte) (ReadByte() - 128));
        }

        public int ReadByteC()
        {
            return (byte) - ReadByte();
        }

        public int Read128Byte()
        {
            return unchecked((byte) (128 - ReadByte()));
        }

        public int ReadUnsignedByte128()
        {
            return ReadUnsignedByte() - 128 & 0xff;
        }

        public int ReadUnsignedByteC()
        {
            return -ReadUnsignedByte() & 0xff;
        }

        public int ReadUnsigned128Byte()
        {
            return 128 - ReadUnsignedByte() & 0xff;
        }

        public int ReadShortLE()
        {
            var i = ReadUnsignedByte() + (ReadUnsignedByte() << 8);
            if (i > 32767)
            {
                i -= 0x10000;
            }
            return i;
        }

        public int ReadShort128()
        {
            var i = (ReadUnsignedByte() << 8) + (ReadByte() - 128 & 0xff);
            if (i > 32767)
            {
                i -= 0x10000;
            }
            return i;
        }

        public int ReadShortLE128()
        {
            var i = (ReadByte() - 128 & 0xff) + (ReadUnsignedByte() << 8);
            if (i > 32767)
            {
                i -= 0x10000;
            }
            return i;
        }

        public int Read128ShortLE()
        {
            var i = (128 - ReadByte() & 0xff) + (ReadUnsignedByte() << 8);
            if (i > 32767)
            {
                i -= 0x10000;
            }
            return i;
        }

        public int ReadShort()
        {
            var i = (ReadUnsignedByte() << 8) + ReadUnsignedByte();
            if (i > 32767)
            {
                i -= 0x10000;
            }
            return i;
        }

        public int ReadUnsignedShortLE()
        {
            return ReadUnsignedByte() + (ReadUnsignedByte() << 8);
        }

        public int ReadUnsignedShort()
        {
            return (ReadUnsignedByte() << 8) + ReadUnsignedByte();
        }

        public int ReadUnsignedShort128()
        {
            return (ReadUnsignedByte() << 8) + (ReadByte() - 128 & 0xff);
        }

        public int ReadUnsignedShortLE128()
        {
            return (ReadByte() - 128 & 0xff) + (ReadUnsignedByte() << 8);
        }

        public int ReadInt()
        {
            return (ReadUnsignedByte() << 24) + (ReadUnsignedByte() << 16) + (ReadUnsignedByte() << 8) + ReadUnsignedByte();
        }

        public int Read24BitInt()
        {
            return (ReadUnsignedByte() << 16) + (ReadUnsignedByte() << 8) + (ReadUnsignedByte());
        }

        public int ReadIntV1()
        {
            return (ReadUnsignedByte() << 8) + ReadUnsignedByte() + (ReadUnsignedByte() << 24) + (ReadUnsignedByte() << 16);
        }

        public int ReadIntV2()
        {
            return (ReadUnsignedByte() << 16) + (ReadUnsignedByte() << 24) + ReadUnsignedByte() + (ReadUnsignedByte() << 8);
        }

        public int ReadIntLE()
        {
            return ReadUnsignedByte() + (ReadUnsignedByte() << 8) + (ReadUnsignedByte() << 16) + (ReadUnsignedByte() << 24);
        }

        public long ReadLong()
        {
            var l = ReadInt() & 0xffffffffL;
            var l1 = ReadInt() & 0xffffffffL;
            return (l << 32) + l1;
        }

        public string ReadString()
        {
            var s = "";
            int b;
            while ((b = ReadByte()) != 0)
            {
                s += (char) b;
            }
            return s;
        }

        public string ReadJagString()
        {
            ReadByte();
            var s = "";
            int b;
            while ((b = ReadByte()) != 0)
            {
                s += (char) b;
            }
            return s;
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @SuppressWarnings("unused") public int readBigSmart()
        public int ReadBigSmart()
        {
            if (Constants.CLIENT_BUILD < 670)
            {
                return ReadUnsignedShort();
            }
            if ((buffer[offset] ^ 0xffffffff) <= -1)
            {
                var value = ReadUnsignedShort();
                if (value == 32767)
                {
                    return -1;
                }
                return value;
            }
            return ReadInt() & 0x7fffffff;
        }

        public int ReadUnsignedSmart()
        {
            var i = 0xff & buffer[offset];
            if (i >= 128)
            {
                return -32768 + ReadUnsignedShort();
            }
            return ReadUnsignedByte();
        }
    }
}
