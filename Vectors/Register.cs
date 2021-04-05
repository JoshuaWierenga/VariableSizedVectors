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
        //Creates a new Register that can hold count values of a specified type
#if DEBUG
        //TODO remove type and just use typeSize + signed bool or an enum?
        private Register(Type type, int typeSize, int count, int bitShiftTypeSize, bool constant = false)
#else
        private Register(int count, int bitShiftTypeSize, bool constant = false)
#endif
        {
            pInt8Values = default;
            pUInt16Values = default;
            pInt16Values = default;
            pUInt32Values = default;
            pInt32Values = default;
            pUInt64Values = default;
            pInt64Values = default;
            pBinary32Values = default;
            pBinary64Values = default;

            byte[] bytes = new byte[count << bitShiftTypeSize];
            pUInt8Values = (byte*)Unsafe.As<byte[], IntPtr>(ref bytes).ToPointer();

            Constant = constant;
            Length = count;
#if DEBUG
            //Used in RegisterDebugView to make appropriately sized arrays for each type 
            ElementSize = typeSize / 8;

            //ElementType and ElementTypeLength is used in DebugToString which needs to know the correct type
            //Can't store Type, String or byte[] with FieldOffset so using byte*
#nullable enable
            string? typeFullName = type.FullName;
#nullable disable
            if (typeFullName is not null)
            {
                byte[] typeName = Encoding.Default.GetBytes(typeFullName);
                fixed (byte* pTypeName = typeName)
                {
                    ElementType = pTypeName;
                }

                ElementTypeLength = typeName.Length;
            }
            else
            {
                ElementType = default;
                ElementTypeLength = 0;
            }
#endif
        }


        //Creates a new Register that can hold a constant. Constant here means that the 
        //constant will be accessible regardless of any indexing and will be compatible
        //with x86 vector extensions which at a maximum require a 256 bit vector.
        internal static Register CreateConstant<T>(T value) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                NumberIn256Bits<T>(), BitShiftHelpers.SizeOf<T>(), true);

            for (int i = 0; i < NumberIn256Bits<T>(); i++)
            {
                Unsafe.WriteUnaligned(ref register.pUInt8Values[i << BitShiftHelpers.SizeOf<T>()], value);
            }

            return register;
        }

        //Creates a new Register that holds the given value of type T
        internal static Register Create<T>(T value) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                1, BitShiftHelpers.SizeOf<T>());

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], value);

            return register;
        }

        //TODO remove SizeOf<T>() and BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
        //Creates a new Register that holds the given values of type T
        internal static Register Create<T>(T[] values) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                 values.Length, BitShiftHelpers.SizeOf<T>());

            Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[0], ref Unsafe.As<T, byte>(ref values[0]),
                (uint)(values.Length << BitShiftHelpers.SizeOf<T>()));

            return register;
        }

        //Creates a new Register that holds 128 bits worth of values of type T
        //Needed since x86 vector extension operations cannot handle VectorX<T> only VectorX<SomeTypeHere>, at least in c#
        internal static Register Create<T>(int count, Vector128<T> values) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], values);

            return register;
        }

        //Creates a new Register that holds all the values from block128 and then value at the end
        internal static Register Create<T>(int count, Vector128<T> block128, T value) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], block128);
            Unsafe.WriteUnaligned(ref register.pUInt8Values[16], value);

            return register;
        }

        //TODO remove SizeOf<T>() and BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
        //Creates a new Register that holds all the values from block128 and then the given values
        internal static Register Create<T>(int count, Vector128<T> block128, T[] values) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], block128);
            Unsafe.CopyBlockUnaligned(ref register.pUInt8Values[16], ref Unsafe.As<T, byte>(ref values[0]),
                (uint)(values.Length << BitShiftHelpers.SizeOf<T>()));

            return register;
        }

        //Creates a new Register that holds 256 bits worth of values of type T
        //Used for Sse2 fallback of 256 bit vectors
        internal static Register Create<T>(int count, Vector128<T> firstBlock128, Vector128<T> secondBlock128) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], firstBlock128);
            Unsafe.WriteUnaligned(ref register.pUInt8Values[16], secondBlock128);

            return register;
        }

        //Creates a new Register that holds 256 bits worth of values of type T
        //See comment on Vector128<T> Create function
        internal static Register Create<T>(int count, Vector256<T> values) where T : struct
        {
            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

            Unsafe.WriteUnaligned(ref register.pUInt8Values[0], values);

            return register;
        }

        //TODO remove SizeOf<T>() and BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
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
                return new Register(
#if DEBUG
                    typeof(T), SizeOf<T>(),
#endif
                    count, BitShiftHelpers.SizeOf<T>());
            }

            int processed = 0;

            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

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

            if (value != null)
            {
                Unsafe.WriteUnaligned(ref register.pUInt8Values[processed << 3], value.Value);
            }

            return register;
        }

        //TODO remove SizeOf<T>() and BitShiftHelpers.SizeOf<T>() and just ask caller for size of type?
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
                return new Register(
#if DEBUG
                    typeof(T), SizeOf<T>(),
#endif
                    count, BitShiftHelpers.SizeOf<T>());
            }

            int processed = 0;

            Register register = new(
#if DEBUG
                typeof(T), SizeOf<T>(),
#endif
                count, BitShiftHelpers.SizeOf<T>());

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

        internal byte this[int index, byte uint8] => Constant ? pUInt8Values[0] : pUInt8Values[index];

        internal sbyte this[int index, sbyte int8] => Constant ? pInt8Values[0] : pInt8Values[index];

        internal ushort this[int index, ushort uint16] => Constant ? pUInt16Values[0] : pUInt16Values[index];

        internal short this[int index, short int16] => Constant ? pInt16Values[0] : pInt16Values[index];

        internal uint this[int index, uint uint32] => Constant ? pUInt32Values[0] : pUInt32Values[index];

        internal int this[int index, int int32] => Constant ? pInt32Values[0] : pInt32Values[index];

        internal ulong this[int index, ulong uint64] => Constant ? pUInt64Values[0] : pUInt64Values[index];

        internal long this[int index, long int64] => Constant ? pInt64Values[0] : pInt64Values[index];

        internal float this[int index, float binary32] => Constant ? pBinary32Values[0] : pBinary32Values[index];

        internal double this[int index, double binary64] => Constant ? pBinary64Values[0] : pBinary64Values[index];


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

        //Compile time constants from Unsafe.SizeOf<T>()
        //gives the size of each supported type in bits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SizeOf<T>()
        {
            if (typeof(T) == typeof(byte))
            {
                return 8;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return 8;
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
                return 32;
            }
            else if (typeof(T) == typeof(int))
            {
                return 32;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return 64;
            }
            else if (typeof(T) == typeof(long))
            {
                return 64;
            }
            else if (typeof(T) == typeof(float))
            {
                return 32;
            }
            else if (typeof(T) == typeof(double))
            {
                return 64;
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


        internal string DebugToString
        {
            get
            {
#if DEBUG
                if (ElementType is null)
                {
                    return "";
                }

                //Get stored type from ElementType
                byte[] items = new byte[ElementTypeLength];
                Marshal.Copy((IntPtr)ElementType, items, 0, ElementTypeLength);
#nullable enable
                Type? storedType = Type.GetType(Encoding.Default.GetString(items));
#nullable disable
                if (storedType is null)
                {
                    return "";
                }

                MethodInfo methodInfo = typeof(Register)
                    .GetMethod("GetValue", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(storedType);

                StringBuilder sb = new();
                string separator = NumberFormatInfo.CurrentInfo.NumberGroupSeparator;

                sb.Append('<');
                object firstNum = methodInfo?.Invoke(this, new object[] { 0 });

                sb.Append(((IFormattable)firstNum)?.ToString("G",
                    CultureInfo.CurrentCulture));

                for (int i = 1; i < Length; i++)
                {
                    sb.Append(separator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)methodInfo?.Invoke(this, new object[] { i }))?.ToString("G",
                        CultureInfo.CurrentCulture));
                }

                sb.Append('>');
                return sb.ToString();
#else
                return "";
#endif
            }
        }

    }
}