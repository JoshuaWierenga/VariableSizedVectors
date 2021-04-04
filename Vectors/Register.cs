using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#if DEBUG
using System.Globalization;
using System.Reflection;
using System.Text;
#endif

namespace Vectors
{
    //TODO use void* overloads for Unsafe
    //TODO Would it be possible to create a linked list style structure
    //this would allow returning to the old VectorX<double> constructor setup and not need any array like type
    //Performance for later items would probably be bad however
    [DebuggerDisplay("{" + nameof(DebugToString) + "}")]
    [DebuggerTypeProxy(typeof(RegisterDebugView))]
    [StructLayout(LayoutKind.Explicit)]
    public readonly unsafe struct Register
    {
        //TODO Check if it is possible to make constant vectors different in a way that is detectable at compile time
        //Check if them being static helps, I tried to go for compile time constants but that is not possible with structs
        //Would it be possible to store them as constant pure data and then at runtime do a one time conversion to a Vector?
        [FieldOffset(0)]
        internal readonly bool Constant;

        [FieldOffset(1)]
        internal readonly int Length;

        //TODO Fix this, why can't an managed array be at a FieldOffset?
        //TODO Figure out if T[] or ReadOnlySpan<T> would be better,
        //ReadOnlySpan requires ref on Vector which breaks interfaces.
        //I tried ReadOnlyMemory but it does not have a void* constructor
        //if this could be worked around and performance was good ReadOnlyMemory<T>.Span exists.
        //TODO Determine if these unioned array fields are useful and if so make them internal
        [FieldOffset(5)]
        internal readonly byte* pUInt8Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly sbyte* pInt8Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly ushort* pUInt16Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly short* pInt16Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly uint* pUInt32Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly int* pInt32Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly ulong* pUInt64Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly long* pInt64Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly float* pBinary32Values;
        [FieldOffset(5)]
#if DEBUG
        internal
#endif
        readonly double* pBinary64Values;

#if DEBUG
        [FieldOffset(13)]
        internal readonly int ElementSize;
        [FieldOffset(17)]
        private readonly int ElementTypeLength;
        [FieldOffset(21)]
        private readonly byte* ElementType;
#endif

#if DEBUG
        private Register(Type type, int typeSize, int count, bool constant = false)
#else
        private Register(int typeSize, int count, bool constant = false)
#endif
        {
            pUInt8Values = default;
            pInt8Values = default;
            pUInt16Values = default;
            pInt16Values = default;
            pUInt32Values = default;
            pInt32Values = default;
            pUInt64Values = default;
            pInt64Values = default;
            pBinary32Values = default;
            pBinary64Values = default;

            switch (typeSize)
            {
                //TODO Determine if Unsafe.As or fixed is quicker
                case 8:
                    sbyte[] sbytes = new sbyte[count];
                    pInt8Values = (sbyte*)Unsafe.As<sbyte[], IntPtr>(ref sbytes).ToPointer();
                    break;
                case 16:
                    short[] int16s = new short[count];
                    pInt16Values = (short*)Unsafe.As<short[], IntPtr>(ref int16s).ToPointer();
                    break;
                case 32:
                    int[] int32s = new int[count];
                    pInt32Values = (int*)Unsafe.As<int[], IntPtr>(ref int32s).ToPointer();
                    break;
                case 64:
                    long[] int64s = new long[count];
                    pInt64Values = (long*)Unsafe.As<long[], IntPtr>(ref int64s).ToPointer();
                    break;
                default:
                    throw new NotSupportedException();
            }

            Constant = constant;
            Length = count;
#if DEBUG
            //Used in RegisterDebugView to make appropriately sized arrays for each type 
            ElementSize = typeSize / 8;

            //Used in DebugToString which needs to know the correct type
            //Can't store Type, String or byte[] with FieldOffset so using byte*
            byte[] typeName = Encoding.Default.GetBytes(type.FullName);
            fixed (byte* pTypeName = typeName)
            {
                ElementType = pTypeName;
            }

            ElementTypeLength = typeName.Length;
#endif
        }


        //TODO Remove or Use
        //Creates a new Register that can hold 256 bits of the specified type
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Register Create256<T>(bool constant = false) where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return new Register(
#if DEBUG
                typeof(T),
#endif
                8, NumberIn256Bits<T>(), constant);

            }
            else if (typeof(T) == typeof(sbyte))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(short))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(uint))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(int))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(long))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(float))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, NumberIn256Bits<T>(), constant);
            }
            else if (typeof(T) == typeof(double))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, NumberIn256Bits<T>(), constant);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        //Creates a new Register that can hold count values of a specified type
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Register Create<T>(int count) where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, count);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, count);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, count);
            }
            else if (typeof(T) == typeof(short))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, count);
            }
            else if (typeof(T) == typeof(uint))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, count);
            }
            else if (typeof(T) == typeof(int))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, count);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, count);
            }
            else if (typeof(T) == typeof(long))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, count);
            }
            else if (typeof(T) == typeof(float))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, count);
            }
            else if (typeof(T) == typeof(double))
            {
                return new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, count);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        //Creates a new Register that can hold a constant. Constant here means that the 
        //constant will be accessible regardless of any indexing and will be compatible
        //with x86 vector extensions which at a maximum require a 256 bit vector.
        internal static Register CreateConstant<T>(T value) where T : struct
        {
            Register register;

            //TODO Remove for loops from cases, loop counter maximum and left shift amount are
            //constants for a given type
            //TODO Determine if the typeof compile optimisation works if branches are merged
            if (typeof(T) == typeof(byte))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 32; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i], value);
                }
            }
            else if (typeof(T) == typeof(sbyte))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 32; i++)
                {
                    //TODO Test if this works
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i], value);
                }
            }
            else if (typeof(T) == typeof(ushort))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 16; i++)
                {
                    //TODO Ensure this works
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 1], value);
                }
            }
            else if (typeof(T) == typeof(short))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 16; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 1], value);
                }
            }
            else if (typeof(T) == typeof(uint))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 8; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 2], value);
                }
            }
            else if (typeof(T) == typeof(int))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 8; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 2], value);
                }
            }
            else if (typeof(T) == typeof(ulong))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 4; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 3], value);
                }
            }
            else if (typeof(T) == typeof(long))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 4; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 3], value);
                }
            }
            else if (typeof(T) == typeof(float))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 8; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 2], value);
                }
            }
            else if (typeof(T) == typeof(double))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, NumberIn256Bits<T>(), true);
                for (int i = 0; i < 4; i++)
                {
                    Unsafe.WriteUnaligned(ref register.pUInt8Values[i << 3], value);
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            return register;
        }

        //Creates a new Register that holds the given value of type T
        internal static Register Create<T>(T value) where T : struct
        {
            Register register;

            if (typeof(T) == typeof(byte))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, 1);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    8, 1);
            }
            else if (typeof(T) == typeof(ushort))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, 1);
            }
            else if (typeof(T) == typeof(short))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    16, 1);
            }
            else if (typeof(T) == typeof(uint))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, 1);
            }
            else if (typeof(T) == typeof(int))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, 1);
            }
            else if (typeof(T) == typeof(ulong))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, 1);
            }
            else if (typeof(T) == typeof(long))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, 1);
            }
            else if (typeof(T) == typeof(float))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    32, 1);
            }
            else if (typeof(T) == typeof(double))
            {
                register = new Register(
#if DEBUG
                    typeof(T),
#endif
                    64, 1);
            }
            else
            {
                throw new NotSupportedException();
            }

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], value);

            return register;
        }

        //TODO remove BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
        //Creates a new Register that holds the given values of type T
        internal static Register Create<T>(T[] values) where T : struct
        {
            Register register = Create<T>(values.Length);

            Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[0], ref Unsafe.As<T, byte>(ref values[0]),
                (uint)(values.Length << BitShiftHelpers.SizeOf<T>()));

            return register;
        }

        //Creates a new Register that holds 128 bits worth of values of type T
        //Needed since x86 vector extension operations cannot handle VectorX<T> only VectorX<SomeTypeHere>, at least in c#
        internal static Register Create<T>(int count, Vector128<T> values) where T : struct
        {
            Register register = Create<T>(count);

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], values);

            return register;
        }

        //Creates a new Register that holds all the values from block128 and then value at the end
        internal static Register Create<T>(int count, Vector128<T> block128, T value) where T : struct
        {
            Register register = Create<T>(count);

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], block128);
            Unsafe.WriteUnaligned(ref register.pUInt8Values[16], value);

            return register;
        }

        //TODO remove BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
        //Creates a new Register that holds all the values from block128 and then the given values
        internal static Register Create<T>(int count, Vector128<T> block128, T[] values) where T : struct
        {
            Register register = Create<T>(count);

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], block128);
            //TODO Figure out why the old version of this line used 16 bytes, was it important?
            Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[16], ref Unsafe.As<T, byte>(ref values[0]),
                (uint)(values.Length << BitShiftHelpers.SizeOf<T>()));

            return register;
        }

        //Creates a new Register that holds 256 bits worth of values of type T
        //Used for Sse2 fallback of 256 bit vectors
        internal static Register Create<T>(int count, Vector128<T> firstBlock128, Vector128<T> secondBlock128) where T : struct
        {
            Register register = Create<T>(count);

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], firstBlock128);
            Unsafe.WriteUnaligned(ref register.pUInt8Values[16], secondBlock128);

            return register;
        }

        //Creates a new Register that holds 256 bits worth of values of type T
        //See comment on Vector128<T> Create function
        internal static Register Create<T>(int count, Vector256<T> values) where T : struct
        {
            Register register = Create<T>(count);

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], values);

            return register;
        }

        //TODO Check overload hidden warning
        //TODO remove BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
        //Creates a new Register that holds all the values from blocks256(optional) and then all values from
        //blocks128(optional) and then finally value(optional) goes on the end
        //Note that despite the fact that all parameters are optional, at least one must be given
        internal static Register Create<T>(int count, Vector256<T>[] blocks256 = null, Vector128<T>[] blocks128 = null,
            T? value = null) where T : struct
        {
            if (blocks256 == null && blocks128 == null && value == null)
            {
                //TODO Revert, this is just temporary to prevent crashes
                //this can be removed once operations can handle all vector sizes
                //throw new ArgumentNullException();
                return Create<T>(count);
            }

            int processed = 0;

            Register register = Create<T>(count);

            if (blocks256 != null)
            {
                processed = blocks256.Length << BitShiftAmountIn256Bit<T>();

                Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[0],
                    ref Unsafe.As<Vector256<T>, byte>(ref blocks256[0]), (uint)(processed << 3));
            }

            if (blocks128 != null)
            {
                int count128 = blocks128.Length << BitShiftAmountIn128Bit<T>();

                //TODO Ensure the register offset works
                Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[processed << 3],
                    ref Unsafe.As<Vector128<T>, byte>(ref blocks128[0]), (uint)(count128 << 3));

                processed += count128;
            }

            if (value != null)
            {
                //TODO Ensure the register offset works
                Unsafe.WriteUnaligned(ref register.pUInt8Values[processed << 3], value.Value);
            }

            return register;
        }

        //TODO Check overload hidden warning
        //TODO remove BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
        //Add(...) does have the relevant values and might soon have the values as variables
        //See comment on Vector128<U> Create function
        //Constructs a new vector with all values from blocks256(optional) and then all values from blocks128(optional) and then finally value(optional) goes on the end
        //Note that despite the fact that all parameters are optional, at least one must be given
        internal static Register Create<T>(int count, Vector256<T>[] blocks256 = null, Vector128<T>[] blocks128 = null,
            T[] values = null) where T : struct
        {
            if (blocks256 == null && blocks128 == null && values == null)
            {
                //TODO Revert, this is just temporary to prevent crashes
                //this can be removed once operations can handle all vector sizes
                //throw new ArgumentNullException();
                return Create<T>(count);
            }

            int processed = 0;

            Register register = Create<T>(count);

            if (blocks256 != null)
            {
                processed = blocks256.Length << BitShiftAmountIn256Bit<T>();

                Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[0],
                    ref Unsafe.As<Vector256<T>, byte>(ref blocks256[0]), (uint)(processed << 3));
            }

            if (blocks128 != null)
            {
                int count128 = blocks128.Length << BitShiftAmountIn128Bit<T>();

                Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[processed << 3],
                    ref Unsafe.As<Vector128<T>, byte>(ref blocks128[0]), (uint)(count128 << 3));

                processed += count128;
            }

            if (values != null)
            {
                Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[processed << 3],
                    ref Unsafe.As<T, byte>(ref values[0]), (uint)(values.Length << BitShiftHelpers.SizeOf<T>()));
            }

            return register;
        }

        //TODO Add Vector128<alltypes> unioned fields
        //TODO Ensure these work with the new UInt64 index
        //TODO Decide if this should work as it does now and return adjacent vectors or should they overlap such that
        //the index has to increase by 2/4 to get an adjacent vector
        //TODO Decide if these need bounds checking for non constants. Provided at least some values in the subvector
        //exists but no all, Unsafe.As will still will return a valid vector but it will contain mostly junk and may
        //lead to crashes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector128<T> ToVector128<T>(int index) where T : struct => Constant
            ? Unsafe.As<ulong, Vector128<T>>(ref pUInt64Values[0])
            : Unsafe.As<ulong, Vector128<T>>(ref pUInt64Values[index << 1]);

        //TODO Add Vector256<alltypes> unioned fields
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector256<T> ToVector256<T>(int index) where T : struct => Constant
            ? Unsafe.As<ulong, Vector256<T>>(ref pUInt64Values[0])
            : Unsafe.As<ulong, Vector256<T>>(ref pUInt64Values[index << 2]);

        //Closest thing possible to a generic indexer when Register cannot be generic,
        //the requirement to use Unsafe, it not ideal
        //TODO Remove Unsafe.As casts and use correct arrays for each, this was the entire point of unioning them
        //Does this require making a different call for each type, most uses are in type specific code so it should
        //be fine if that is required
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetValue<T>(int index)
        {
            if (typeof(T) == typeof(byte))
            {
                return Constant ? Unsafe.As<byte, T>(ref pUInt8Values[0]) : Unsafe.As<byte, T>(ref pUInt8Values[index]);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return Constant ? Unsafe.As<sbyte, T>(ref pInt8Values[0]) : Unsafe.As<sbyte, T>(ref pInt8Values[index]);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return Constant ? Unsafe.As<ushort, T>(ref pUInt16Values[0]) : Unsafe.As<ushort, T>(ref pUInt16Values[index]);
            }
            else if (typeof(T) == typeof(short))
            {
                return Constant ? Unsafe.As<short, T>(ref pInt16Values[0]) : Unsafe.As<short, T>(ref pInt16Values[index]);
            }
            else if (typeof(T) == typeof(uint))
            {
                return Constant ? Unsafe.As<uint, T>(ref pUInt32Values[0]) : Unsafe.As<uint, T>(ref pUInt32Values[index]);
            }
            else if (typeof(T) == typeof(int))
            {
                return Constant ? Unsafe.As<int, T>(ref pInt32Values[0]) : Unsafe.As<int, T>(ref pInt32Values[index]);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return Constant ? Unsafe.As<ulong, T>(ref pUInt64Values[0]) : Unsafe.As<ulong, T>(ref pUInt64Values[index]);
            }
            else if (typeof(T) == typeof(long))
            {
                return Constant ? Unsafe.As<long, T>(ref pInt64Values[0]) : Unsafe.As<long, T>(ref pInt64Values[index]);
            }
            else if (typeof(T) == typeof(float))
            {
                return Constant ? Unsafe.As<float, T>(ref pBinary32Values[0]) : Unsafe.As<float, T>(ref pBinary32Values[index]);
            }
            else if (typeof(T) == typeof(double))
            {
                return Constant ? Unsafe.As<double, T>(ref pBinary64Values[0]) : Unsafe.As<double, T>(ref pBinary64Values[index]);
            }
            else
            {
                throw new NotSupportedException();
            }
        }


        //Provided I understand how AggressiveInlining and typeof work, these bitshift functions should
        //be reduced to a single result and inlined at compile time for every call for a given type

        //This is the result of log_2(128/Unsafe.SizeOf<T> / 8) and is designed to be used
        //as "a << AmountIn128Bit<T>()" whenever "a * Vector128<T>.Count() might be used
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BitShiftAmountIn128Bit<T>()
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
        private static int BitShiftAmountIn256Bit<T>()
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
        }*/

        //This is the result of 256/(Unsafe.SizeOf<T>*8)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NumberIn256Bits<T>()
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


        internal unsafe string DebugToString
        {
            get
            {
#if DEBUG
                StringBuilder sb = new();
                string separator = NumberFormatInfo.CurrentInfo.NumberGroupSeparator;

                byte[] items = new byte[ElementTypeLength];
                Marshal.Copy((IntPtr)ElementType, items, 0, ElementTypeLength);
                Type storedType = Type.GetType(Encoding.Default.GetString(items));

                MethodInfo methodInfo = typeof(Register).GetMethod("GetValue", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(storedType);

                sb.Append('<');
                object firstNum = methodInfo.Invoke(this, new object[] { 0 });
                sb.Append(((IFormattable)firstNum).ToString("G",
                    CultureInfo.CurrentCulture));

                for (int i = 1; i < Length; i++)
                {
                    sb.Append(separator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)methodInfo.Invoke(null, new object[] { i })).ToString("G", CultureInfo.CurrentCulture));
                }

                sb.Append('>');
                return sb.ToString();
#endif
                return "";
            }
        }

    }
}