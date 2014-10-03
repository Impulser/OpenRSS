using System;

namespace com.alex.io
{
    using Constants = com.alex.utils.Constants;

    public class OutputStream : Stream
    {
        private static readonly int[] BIT_MASK = new int[32];
        private int opcodeStart;

        static OutputStream()
        {
            for (var i = 0; i < 32; i++)
            {
                BIT_MASK[i] = (1 << i) - 1;
            }
        }

        public OutputStream(int capacity)
        {
            SetBuffer(new byte[capacity]);
        }

        public OutputStream()
        {
            SetBuffer(new byte[16]);
        }

        public OutputStream(byte[] buffer)
        {
            SetBuffer(buffer);
            offset = buffer.Length;
            length = buffer.Length;
        }

        public OutputStream(int[] buffer)
        {
            SetBuffer(new byte[buffer.Length]);
            foreach (var value in buffer)
            {
                WriteByte(value);
            }
        }

        public void CheckCapacityPosition(int position)
        {
            if (position >= GetBuffer().Length)
            {
                var newBuffer = new byte[position + 16];
                Array.Copy(GetBuffer(), 0, newBuffer, 0, GetBuffer().Length);
                SetBuffer(newBuffer);
            }
        }

        public void Skip(int length)
        {
            SetOffset(GetOffset() + length);
        }

        public void SetOffset(int offset)
        {
            this.offset = offset;
        }

        public void WriteBytes(byte[] b, int offset, int length)
        {
            CheckCapacityPosition(GetOffset() + length - offset);
            Array.Copy(b, offset, GetBuffer(), GetOffset(), length);
            SetOffset(GetOffset() + (length - offset));
        }

        public void WriteBytes(byte[] b)
        {
            var offset = 0;
            var length = b.Length;
            CheckCapacityPosition(GetOffset() + length - offset);
            Array.Copy(b, offset, GetBuffer(), GetOffset(), length);
            SetOffset(GetOffset() + (length - offset));
        }

        public void AddBytes128(byte[] data, int offset, int len)
        {
            for (var k = offset; k < len; k++)
            {
                WriteByte(unchecked((byte) (data[k] + 128)));
            }
        }

        public void AddBytesS(byte[] data, int offset, int len)
        {
            for (var k = offset; k < len; k++)
            {
                WriteByte(unchecked((byte) (-128 + data[k])));
            }
        }

        public void AddBytes_Reverse(byte[] data, int offset, int len)
        {
            for (var i = len - 1; i >= 0; i--)
            {
                WriteByte(data[i]);
            }
        }

        public void AddBytes_Reverse128(byte[] data, int offset, int len)
        {
            for (var i = len - 1; i >= 0; i--)
            {
                WriteByte(unchecked((byte) (data[i] + 128)));
            }
        }

        public void WriteByte(int i)
        {
            WriteByte(i, offset++);
        }

        public void WriteNegativeByte(int i)
        {
            WriteByte(-i, offset++);
        }

        public void WriteByte(int i, int position)
        {
            CheckCapacityPosition(position);
            GetBuffer()[position] = (byte) i;
        }

        public void WriteByte128(int i)
        {
            WriteByte(i + 128);
        }

        public void WriteByteC(int i)
        {
            WriteByte(-i);
        }

        public void Write3Byte(int i)
        {
            WriteByte(i >> 16);
            WriteByte(i >> 8);
            WriteByte(i);
        }

        public void Write128Byte(int i)
        {
            WriteByte(128 - i);
        }

        public void WriteShortLE128(int i)
        {
            WriteByte(i + 128);
            WriteByte(i >> 8);
        }

        public void WriteShort128(int i)
        {
            WriteByte(i >> 8);
            WriteByte(i + 128);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @SuppressWarnings("unused") public void writeBigSmart(int i)
        public void WriteBigSmart(int i)
        {
            if (Constants.CLIENT_BUILD < 670)
            {
                WriteShort(i);
                return;
            }
            if (i >= short.MaxValue && i >= 0)
            {
                WriteInt(i - int.MaxValue - 1);
            }
            else
            {
                WriteShort(i >= 0
                                   ? i
                                   : 32767);
            }
        }

        public void WriteSmart(int i)
        {
            if (i >= 128)
            {
                WriteShort(i + 32768);
            }
            else
            {
                WriteByte(i);
            }
        }

        public void WriteShort(int i)
        {
            WriteByte(i >> 8);
            WriteByte(i);
        }

        public void WriteShortLE(int i)
        {
            WriteByte(i);
            WriteByte(i >> 8);
        }

        public void Write24BitInt(int i)
        {
            WriteByte(i >> 16);
            WriteByte(i >> 8);
            WriteByte(i);
        }

        public override void WriteInt(int i)
        {
            WriteByte(i >> 24);
            WriteByte(i >> 16);
            WriteByte(i >> 8);
            WriteByte(i);
        }

        public void WriteIntV1(int i)
        {
            WriteByte(i >> 8);
            WriteByte(i);
            WriteByte(i >> 24);
            WriteByte(i >> 16);
        }

        public void WriteIntV2(int i)
        {
            WriteByte(i >> 16);
            WriteByte(i >> 24);
            WriteByte(i);
            WriteByte(i >> 8);
        }

        public void WriteIntLE(int i)
        {
            WriteByte(i);
            WriteByte(i >> 8);
            WriteByte(i >> 16);
            WriteByte(i >> 24);
        }

        public void WriteLong(long l)
        {
            WriteByte((int) (l >> 56));
            WriteByte((int) (l >> 48));
            WriteByte((int) (l >> 40));
            WriteByte((int) (l >> 32));
            WriteByte((int) (l >> 24));
            WriteByte((int) (l >> 16));
            WriteByte((int) (l >> 8));
            WriteByte((int) l);
        }

        public void WritePSmarts(int i)
        {
            if (i < 128)
            {
                WriteByte(i);
                return;
            }
            if (i < 32768)
            {
                WriteShort(32768 + i);
            }
            Console.WriteLine("Error psmarts out of range:");
        }

        public void WriteString(string s)
        {
            CheckCapacityPosition(GetOffset() + s.Length + 1);
            Array.Copy(s.getBytes(), 0, GetBuffer(), GetOffset(), s.Length);
            SetOffset(GetOffset() + s.Length);
            WriteByte(0);
        }

        public void WriteGJString(string s)
        {
            WriteByte(0);
            WriteString(s);
        }

        public void PutGJString3(string s)
        {
            WriteByte(0);
            WriteString(s);
            WriteByte(0);
        }

        public void WritePacket(int id)
        {
            WriteByte(id);
        }

        public void WritePacketVarByte(int id)
        {
            WritePacket(id);
            WriteByte(0);
            opcodeStart = GetOffset() - 1;
        }

        public void WritePacketVarShort(int id)
        {
            WritePacket(id);
            WriteShort(0);
            opcodeStart = GetOffset() - 2;
        }

        /*
         * public void writePacketShort(int id) { writeByte(id); writeShort(0);
         * opcodeStart = getOffset() - 2; }
         */

        public void EndPacketVarByte()
        {
            WriteByte(GetOffset() - (opcodeStart + 2) + 1, opcodeStart);
        }

        public void EndPacketVarShort()
        {
            var size = GetOffset() - (opcodeStart + 2);
            WriteByte(size >> 8, opcodeStart++);
            WriteByte(size, opcodeStart);
        }

        public void InitBitAccess()
        {
            bitPosition = GetOffset() * 8;
        }

        public void FinishBitAccess()
        {
            SetOffset((bitPosition + 7) / 8);
        }

        public int GetBitPos(int i)
        {
            return 8 * i - bitPosition;
        }

        public void WriteBits(int numBits, int value)
        {
            var bytePos = bitPosition >> 3;
            var bitOffset = 8 - (bitPosition & 7);
            bitPosition += numBits;
            for (; numBits > bitOffset; bitOffset = 8)
            {
                CheckCapacityPosition(bytePos);
                GetBuffer()[bytePos] &= (byte) (~BIT_MASK[bitOffset]);
                GetBuffer()[bytePos++] |= (byte) (value >> numBits - bitOffset & BIT_MASK[bitOffset]);
                numBits -= bitOffset;
            }
            CheckCapacityPosition(bytePos);
            if (numBits == bitOffset)
            {
                GetBuffer()[bytePos] &= (byte) (~BIT_MASK[bitOffset]);
                GetBuffer()[bytePos] |= (byte) (value & BIT_MASK[bitOffset]);
            }
            else
            {
                GetBuffer()[bytePos] &= (byte) (~(BIT_MASK[numBits] << bitOffset - numBits));
                GetBuffer()[bytePos] |= (byte) ((value & BIT_MASK[numBits]) << bitOffset - numBits);
            }
        }

        public void SetBuffer(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public void RsaEncode(System.Numerics.BigInteger key, System.Numerics.BigInteger modulus)
        {
            var length = offset;
            offset = 0;
            var data = new byte[length];
            GetBytes(data, 0, length);
            var biginteger2 = new System.Numerics.BigInteger(data);
            System.Numerics.BigInteger biginteger3 = biginteger2.modPow(key, modulus);
            byte[] @out = biginteger3.toByteArray();
            offset = 0;
            WriteBytes(@out, 0, @out.Length);
        }
    }
}
