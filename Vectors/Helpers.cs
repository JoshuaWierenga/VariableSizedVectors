using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Vectors
{
    //Provided I understand how AggressiveInlining and typeof work, these bitshift functions should
    //be reduced to a single result and inlined at compile time for every call for a given type
    internal static class BitShiftHelpers
    {
        //This is the result of log_2(Unsafe.SizeOf<T>) and is designed to be used
        //as "a << SizeOf<T>()" wherever "a * Unsafe.SizeOf<T>()" might be used
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

    internal static class SizeHelpers
    {
        //This is the result of 256/(Unsafe.SizeOf<T>*8)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int NumberIn256Bits<T>()
        {
            if (typeof(T) == typeof(byte))
            {
                return 32;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return 32;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return 16;
            }
            else if (typeof(T) == typeof(short))
            {
                return 16;
            }
            else if (typeof(T) == typeof(uint))
            {
                return 8;
            }
            else if (typeof(T) == typeof(int))
            {
                return 8;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return 4;
            }
            else if (typeof(T) == typeof(long))
            {
                return 4;
            }
            else if (typeof(T) == typeof(float))
            {
                return 8;
            }
            else if (typeof(T) == typeof(double))
            {
                return 4;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    //X.IsSupported is used all over Vector<T>, this should sightly improve performance by only doing each check once
    internal static class IntrinsicSupport
    {
        internal static bool IsSseSupported { get; } = Sse.IsSupported;

        internal static bool IsSse2Supported { get; } = Sse2.IsSupported;

        internal static bool IsAvxSupported { get; } = Avx.IsSupported;

        internal static bool IsAvx2Supported { get; } = Avx2.IsSupported;
    }
}