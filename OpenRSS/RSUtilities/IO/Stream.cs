using System.IO;

namespace com.io
{
    public abstract class Stream
    {
        protected internal int bitPosition;
        protected internal byte[] buffer;
        protected internal int length;
        protected internal int offset;

        public virtual int GetLength()
        {
            return length;
        }

        public virtual byte[] GetBuffer()
        {
            return buffer;
        }

        public virtual int GetOffset()
        {
            return offset;
        }

        public virtual void DecodeXTEA(int[] keys)
        {
            DecodeXTEA(keys, 5, length);
        }

        public virtual void DecodeXTEA(int[] keys, int start, int end)
        {
            var l = offset;
            offset = start;
            var i1 = (end - start) / 8;
            for (var j1 = 0; j1 < i1; j1++)
            {
                var k1 = ReadInt();
                var l1 = ReadInt();
                var sum = unchecked((int) 0xc6ef3720);
                var delta = unchecked((int) 0x9e3779b9);
                for (var k2 = 32; k2-- > 0;)
                {
                    l1 -= keys[(int) ((uint) (sum & 0x1c84) >> 11)] + sum ^ ((int) ((uint) k1 >> 5) ^ k1 << 4) + k1;
                    sum -= delta;
                    k1 -= ((int) ((uint) l1 >> 5) ^ l1 << 4) + l1 ^ keys[sum & 3] + sum;
                }
                offset -= 8;
                WriteInt(k1);
                WriteInt(l1);
            }
            offset = l;
        }

        public void EncodeXTEA(int[] keys, int start, int end)
        {
            var o = offset;
            var j = (end - start) / 8;
            offset = start;
            for (var k = 0; k < j; k++)
            {
                var l = ReadInt();
                var i1 = ReadInt();
                var sum = 0;
                var delta = unchecked((int) 0x9e3779b9);
                for (var l1 = 32; l1-- > 0;)
                {
                    l += sum + keys[3 & sum] ^ i1 + ((int) ((uint) i1 >> 5) ^ i1 << 4);
                    sum += delta;
                    i1 += l + ((int) ((uint) l >> 5) ^ l << 4) ^ keys[(int) ((uint) (0x1eec & sum) >> 11)] + sum;
                }

                offset -= 8;
                WriteInt(l);
                WriteInt(i1);
            }
            offset = o;
        }

        private int ReadInt()
        {
            offset += 4;
            return ((0xff & buffer[-3 + offset]) << 16) + ((((0xff & buffer[-4 + offset]) << 24) + ((buffer[-2 + offset] & 0xff) << 8)) + (buffer[-1 + offset] & 0xff));
        }

        public virtual void WriteInt(int value)
        {
            buffer[offset++] = (byte) (value >> 24);
            buffer[offset++] = (byte) (value >> 16);
            buffer[offset++] = (byte) (value >> 8);
            buffer[offset++] = (byte) value;
        }

        public void GetBytes(byte[] data, int off, int len)
        {
            for (var k = off; k < len + off; k++)
            {
                data[k] = buffer[offset++];
            }
        }
    }
}
