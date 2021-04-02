using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Vectors
{
    //TODO Would it be possible to create a linked list style structure,
    //this would allow returning to the old VectorX<double> constructor setup and not need any array like type
    //Performance for later items would probably be bad however
    public readonly struct RegisterDouble
    {
        //TODO Figure out if double[] or ReadOnlySpan<double> is better,
        //ReadOnlySpan requires ref on VectorDouble which breaks interfaces.
        //I tried ReadOnlyMemory but it does not have a void* constructor
        //if this could be worked around and performance was good ReadOnlyMemory<double>.Span exists.
        internal readonly double[] Doubles;

        internal readonly bool MultiSize;

        internal unsafe RegisterDouble(Vector128<double> values)
        {
            Doubles = new Span<double>(&values, 2).ToArray();
            MultiSize = false;
        }

        internal unsafe RegisterDouble(Vector256<double> values, int count)
        {
            Doubles = new Span<double>(&values, count).ToArray();
            MultiSize = false;
        }

        internal RegisterDouble(double value, bool multiSize = false)
        {
            Doubles = new[] { value };
            MultiSize = multiSize;
        }

        internal RegisterDouble(double[] values)
        {
            Doubles = values;
            MultiSize = false;
        }

        //internal Vector64<double> ToVector64(int index) => Unsafe.As<double, Vector64<double>>(ref Doubles[index]);

        internal Vector128<double> ToVector128(int index) => Unsafe.As<double, Vector128<double>>(ref Doubles[index]);

        internal Vector256<double> ToVector256(int index) => Unsafe.As<double, Vector256<double>>(ref Doubles[index]);

        internal double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MultiSize ? Doubles[0] : Doubles[index];
        }
    }
}