using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Vectors
{
    //TODO Search for All VectorDouble and double references and update

    //TODO Would it be possible to create a linked list style structure
    //this would allow returning to the old VectorX<double> constructor setup and not need any array like type
    //Performance for later items would probably be bad however
    public readonly struct Register<T>
        where T : struct
    {
        //TODO Figure out if double[] or ReadOnlySpan<double> is better,
        //ReadOnlySpan requires ref on Vector which breaks interfaces.
        //I tried ReadOnlyMemory but it does not have a void* constructor
        //if this could be worked around and performance was good ReadOnlyMemory<T>.Span exists.
        internal readonly T[] Values;

        //TODO Check if it is possible to make constant vectors different in a way that is detectable at compile time
        //Check if them being static helps, I tried to go for compile time constants but that is not possible with structs
        //Would it be possible to store them as constant pure data and then at runtime do a one time conversion to a Vector?
        internal readonly bool MultiSize;

        internal unsafe Register(Vector128<T> values)
        {
            //TODO Check if Unsafe.WriteUnaligned would be faster
            Values = new Span<T>(&values, 2).ToArray();
            MultiSize = false;
        }

        internal unsafe Register(Vector256<T> values, int count)
        {
            //TODO Check if Unsafe.WriteUnaligned would be faster
            Values = new Span<T>(&values, count).ToArray();
            MultiSize = false;
        }

        //TODO Rewrite for whatever is needed now as 128 + Unsafe.SizeOf<T>() =/= 192 at all times, only for Double, Int64 and UInt64
        //Use Vector64<T>? If so we need another set of cases in Vector<T> to handle mmx
        //Used for Sse2 fallback of hardcoded 192 bit double vectors
        internal Register(Vector128<T> block128, T value)
        {
            //TODO Test
            Values = new T[Vector128<double>.Count + 1];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Values[0]),
                ref Unsafe.As<Vector128<T>, byte>(ref block128), 16);
            Values[2] = value;

            MultiSize = false;
        }

        //Used for Sse2 fallback of hardcoded 256 bit vectors
        internal Register(Vector128<T> firstBlock128, Vector128<T> secondBlock128)
        {
            //TODO Test
            Values = new T[Vector128<T>.Count << 1];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Values[0]),
                ref Unsafe.As<Vector128<T>, byte>(ref firstBlock128), 16);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Values[2]),
                ref Unsafe.As<Vector128<T>, byte>(ref secondBlock128), 16);

            MultiSize = false;
        }

        //Constructs a new vector with all values from blocks256(optional) and then all values from blocks128(optional) and then finally value(optional) goes on the end
        //Note that despite the fact that all parameters are optional, at least one must be given
        internal Register(int count, T? value = null, Vector128<T>[] blocks128 = null, Vector256<T>[] blocks256 = null)
        {
            if (blocks256 == null && blocks128 == null && value == null)
            {
                //TODO Revert, this is just temporary to prevent crashes
                //throw new ArgumentNullException();
                Values = new T[count];
                MultiSize = false;
                return;
            }

            int processed = 0;

            Values = new T[count];

            if (blocks256 != null)
            {
                processed = blocks256.Length << BitShiftAmount256Bit();

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Values[0]),
                    ref Unsafe.As<Vector256<T>, byte>(ref blocks256[0]), (uint)(processed << 3));
            }

            if (blocks128 != null)
            {
                int count128 = blocks128.Length << BitShiftAmount128Bit();

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Values[processed]),
                    ref Unsafe.As<Vector128<T>, byte>(ref blocks128[0]), (uint)(count128 << 3));

                processed += count128;
            }

            if (value != null)
            {
                Values[processed] = value.Value;
            }

            MultiSize = false;
        }

        internal Register(T value, bool multiSize = false)
        {
            if (multiSize)
            {
                //Constant vectors need to work with avx instructions and so need to be 256 bit long
                Values = new T[Vector256<T>.Count];
                for (int i = 0; i < Values.Length; i++)
                {
                    Values[i] = value;
                }
            }
            else
            {
                Values = new[] { value };
            }

            MultiSize = multiSize;
        }

        internal Register(T[] values)
        {
            Values = values;
            MultiSize = false;
        }

        //TODO Decide if this should work as it does now and return adjacent vectors or should they overlap such that
        //TODO Decide if these need bounds checking for non constants, provided at least one value in the subvector
        //exists, Unsafe.As will return a valid vector but it normally contains mostly junk and may lead to crashes
        //the index has to increase by 2/4 to get an adjacent vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<T> ToVector128(int index) => MultiSize
            ? Unsafe.As<T, Vector128<T>>(ref Values[0])
            : Unsafe.As<T, Vector128<T>>(ref Values[index << 1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector256<T> ToVector256(int index) => MultiSize
            ? Unsafe.As<T, Vector256<T>>(ref Values[0])
            : Unsafe.As<T, Vector256<T>>(ref Values[index << 2]);

        internal T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MultiSize ? Values[0] : Values[index];
        }

        //Find number required to use while bitshifting to mimic multiplication for a specific type
        //Used to compute the number of values in an array of Vector128<T>s
        //Provided I understand how AggressiveInlining and typeof work, this should be reduced to a single
        //result and inlined at compile time for every instance of this class
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BitShiftAmount128Bit()
        {
            if (typeof(T) == typeof(byte))
            {
                return 4;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return 4;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return 3;
            }
            else if (typeof(T) == typeof(short))
            {
                return 3;
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
                return 1;
            }
            else if (typeof(T) == typeof(long))
            {
                return 1;
            }
            else if (typeof(T) == typeof(float))
            {
                return 2;
            }
            else if (typeof(T) == typeof(double))
            {
                return 1;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        //See above comment. Used to compute the number of values in an array of Vector256<T>s
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BitShiftAmount256Bit()
        {
            if (typeof(T) == typeof(byte))
            {
                return 5;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return 5;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return 4;
            }
            else if (typeof(T) == typeof(short))
            {
                return 4;
            }
            else if (typeof(T) == typeof(uint))
            {
                return 3;
            }
            else if (typeof(T) == typeof(int))
            {
                return 3;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return 2;
            }
            else if (typeof(T) == typeof(long))
            {
                return 2;
            }
            else if (typeof(T) == typeof(float))
            {
                return 3;
            }
            else if (typeof(T) == typeof(double))
            {
                return 2;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        //See above comment. Used to compute the number of values in an array of Vector256<T>s
        private static int NumberIn256Bits()
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
}