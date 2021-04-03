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

        //TODO Split constants into derived struct to rely on typeof(instance) == typeof(DerivedStruct) instead which is a compile time constant 
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

        //Constructs a new vector with all values from blocks256(optional) and then all values from blocks128(optional) and then finally value(optional) goes on the end
        //Note that despite the fact that all parameters are optional, at least one must be given
        internal RegisterDouble(int count, double? value = null, Vector128<double>[] blocks128 = null, Vector256<double>[] blocks256 = null)
        {
            if (blocks256 == null && blocks128 == null && value == null)
            {
                throw new ArgumentNullException();
            }

            int processed = 0;

            Doubles = new double[count];

            if (blocks256 != null)
            {
                processed += blocks256.Length << 2;

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref Doubles[0]),
                    ref Unsafe.As<Vector256<double>, byte>(ref blocks256[0]), (uint)(processed * sizeof(double)));
            }

            if (blocks128 != null)
            {
                int count128 = blocks128.Length << 1;

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref Doubles[processed]),
                    ref Unsafe.As<Vector128<double>, byte>(ref blocks128[0]), (uint)(count128 * sizeof(double)));

                processed += count128;
            }

            if (value != null)
            {
                Doubles[processed] = value.Value;
            }

            MultiSize = false;
        }

        internal RegisterDouble(double value, bool multiSize = false)
        {
            //Constant vectors need to work with avx instructions and so need to be 256 bit at a minimum
            Doubles = multiSize ? new[] { value, value, value, value } : new[] { value };
            MultiSize = multiSize;
        }

        internal RegisterDouble(double[] values)
        {
            Doubles = values;
            MultiSize = false;
        }

        //internal Vector64<double> ToVector64(int index) => Unsafe.As<double, Vector64<double>>(ref Doubles[index]);

        //TODO Decide if this should work as it does now and return adjacent vectors or should they overlap such that
        //TODO Decide if these need bounds checking for non constants, provided at least one value in the subvector
        //exists, Unsafe.As will return a valid vector but it normally contains mostly junk and may lead to crashes
        //the index has to increase by 2/4 to get an adjacent vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<double> ToVector128(int index) => MultiSize
            ? Unsafe.As<double, Vector128<double>>(ref Doubles[0])
            : Unsafe.As<double, Vector128<double>>(ref Doubles[index << 1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector256<double> ToVector256(int index) => MultiSize
            ? Unsafe.As<double, Vector256<double>>(ref Doubles[0])
            : Unsafe.As<double, Vector256<double>>(ref Doubles[index << 2]);

        internal double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MultiSize ? Doubles[0] : Doubles[index];
        }
    }
}