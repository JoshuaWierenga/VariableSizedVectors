using System;
using System.Runtime.CompilerServices;

namespace Vectors
{
    internal static class BitShiftHelpers
    {
        //This is the result of log_2(Unsafe.SizeOf<T>) and is designed to be used
        //as "a << BitShiftAmountSizeOf()" wherever "a * Unsafe.SizeOf<T>()" might be used
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SizeOf<T>()
        {
            if (typeof(T) == typeof(byte))
            {
                return 0;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return 0;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return 1;
            }
            else if (typeof(T) == typeof(short))
            {
                return 1;
            }
            else if (typeof(T) == typeof(uint))
            {
                return 2;
            }
            else if (typeof(T) == typeof(int))
            {
                return 2;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return 3;
            }
            else if (typeof(T) == typeof(long))
            {
                return 3;
            }
            else if (typeof(T) == typeof(float))
            {
                return 2;
            }
            else if (typeof(T) == typeof(double))
            {
                return 3;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}