using java.io;
using java.lang;
using java.math;
using java.nio;
using java.text;
using java.util;
using java.util.zip;

namespace VMUtilities.Collections
{
    public static class ArrayUtil
    {
        public static T[][] ReturnRectangularArray<T>(int Size1, int Size2)
            where T : struct
        {
            T[][] Array;
            if (Size1 > -1)
            {
                Array = new T[Size1][];
                if (Size2 > -1)
                {
                    for (var Array1 = 0; Array1 < Size1; Array1++)
                    {
                        Array[Array1] = new T[Size2];
                    }
                }
            }
            else
            {
                Array = null;
            }

            return Array;
        }
    }
}
