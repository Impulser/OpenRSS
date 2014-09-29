using java.io;
using java.lang;
using java.net;
using java.nio;
using java.text;
using java.util;

namespace OpenRSS.Utility
{
    public static class ArrayUtilities
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
