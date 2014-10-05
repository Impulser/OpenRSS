using System;
using System.Linq;

using java.io;
using java.lang;
using java.math;
using java.nio;
using java.text;
using java.util;
using java.util.zip;

namespace RSUtilities.Collections
{
    public static class ArrayUtil
    {
        public static T[][] ReturnRectangularArray<T>(int firstDimension, int secondDimension)
            where T : struct
        {
            return new T[firstDimension].Select(obj => new T[secondDimension]).ToArray();
        }
    }
}
