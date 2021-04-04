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

        private Register(int count)
        {
            Values = new T[count];
            MultiSize = false;
        }

        //TODO Rewrite for whatever is needed now as 128 + Unsafe.SizeOf<T>() =/= 192 at all times, only for Double, Int64 and UInt64
        //Use Vector64<T>? If so we need another set of cases in Vector<T> to handle mmx
        //Used for Sse2 fallback of hardcoded 192 bit double vectors
        /*internal Register(Vector128<T> block128, T value)
        {
            //TODO Test
            Values = new T[Vector128<T>.Count + 1];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref Values[0]),
                ref Unsafe.As<Vector128<T>, byte>(ref block128), 16);
            Values[2] = value;

            MultiSize = false;
        }*/

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


        //Needed since x86 vector extension operations cannot handle VectorX<T> only VectorX<SomeTypeHere>, at least in c#
        //This function takes the result in terms of some hardcoded type U and casts it back to the general T to get around this
        internal static Register<T> Create<U>(Vector128<U> values, int count) where U : struct
        {
            Register<T> register = new(count);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref register.Values[0]),
                ref Unsafe.As<Vector128<U>, byte>(ref values), 16);

            return register;
        }

        //See comment on Vector128<U> Create function
        internal static Register<T> Create<U>(Vector256<U> values, int count) where U : struct
        {
            Register<T> register = new(count);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref register.Values[0]),
                ref Unsafe.As<Vector256<U>, byte>(ref values), 32);

            return register;
        }

        //TODO remove BitShiftHelpers.AmountInXBit<T> and just ask caller for values?
        //Add(...) does have the relevant values and might soon have the values as variables
        //See comment on Vector128<U> Create function
        //Constructs a new vector with all values from blocks256(optional) and then all values from blocks128(optional) and then finally value(optional) goes on the end
        //Note that despite the fact that all parameters are optional, at least one must be given
        internal static Register<T> Create<U>(int count, U? value = null, Vector128<U>[] blocks128 = null,
            Vector256<U>[] blocks256 = null) where U : struct
        {
            if (blocks256 == null && blocks128 == null && value == null)
            {
                //TODO Revert, this is just temporary to prevent crashes
                //this can be removed once operations can handle all vector sizes
                //throw new ArgumentNullException();
                return new Register<T>(count);
            }

            int processed = 0;

            Register<T> register = new(count);

            if (blocks256 != null)
            {
                processed = blocks256.Length << BitShiftAmountIn256Bit();

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref register.Values[0]),
                    ref Unsafe.As<Vector256<U>, byte>(ref blocks256[0]), (uint) (processed << 3));
            }

            if (blocks128 != null)
            {
                int count128 = blocks128.Length << BitShiftAmountIn128Bit();

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref register.Values[processed]),
                    ref Unsafe.As<Vector128<U>, byte>(ref blocks128[0]), (uint)(count128 << 3));

                processed += count128;
            }

            if (value != null)
            {
                //TODO would just storing value.Value somewhere and then using
                //register.Values[processed] = Unsafe.As<U, V>(ref storedValue) be better?
                Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref register.Values[processed]), value!.Value);
            }

            return register;
        }


        //TODO Decide if this should work as it does now and return adjacent vectors or should they overlap such that
        //the index has to increase by 2/4 to get an adjacent vector
        //TODO Decide if these need bounds checking for non constants. Provided at least some values in the subvector
        //exists but no all, Unsafe.As will still will return a valid vector but it will contain mostly junk and may
        //lead to crashes
        //Needed since x86 vector extension operations cannot handle Vector128<T> only Vector128<SomeTypeHere>, at least in c#
        //This function creates Vector128's in terms of a given type U to get around this
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<U> ToVector128<U>(int index) where U : struct => MultiSize
            ? Unsafe.As<T, Vector128<U>>(ref Values[0])
            : Unsafe.As<T, Vector128<U>>(ref Values[index << 1]);

        //Needed since x86 vector extension operations cannot handle Vector256<T> only Vector256<SomeTypeHere>, at least in c#
        //This function creates Vector256's in terms of a given type U to get around this
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector256<U> ToVector256<U>(int index) where U : struct => MultiSize
            ? Unsafe.As<T, Vector256<U>>(ref Values[0])
            : Unsafe.As<T, Vector256<U>>(ref Values[index << 2]);

        internal T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MultiSize ? Values[0] : Values[index];
        }


        //Provided I understand how AggressiveInlining and typeof work, this bitshift functions
        //should be reduced to a single result and inlined at compile time for every call

        //This is the result of log_2(128/Unsafe.SizeOf<T> / 8) and is designed to be used
        //as "a << AmountIn128Bit<T>()" whenever "a * Vector128<T>.Count() might be used
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BitShiftAmountIn128Bit()
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

        //This is the result of log_2(256/Unsafe.SizeOf<T> / 8) and is designed to be used
        //as "a << AmountIn256Bit<T>()" whenever "a * Vector256<T>.Count() might be used
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BitShiftAmountIn256Bit()
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

        //TODO Use or Remove
        //This is the result of 128/(Unsafe.SizeOf<T>*8)
        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NumberIn128Bits()
        {
            if (typeof(T) == typeof(byte))
            {
                return 16;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return 16;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return 8;
            }
            else if (typeof(T) == typeof(short))
            {
                return 8;
            }
            else if (typeof(T) == typeof(uint))
            {
                return 4;
            }
            else if (typeof(T) == typeof(int))
            {
                return 4;
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
                return 4;
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

        //This is the result of 256/(Unsafe.SizeOf<T>*8)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        }*/
    }
}