﻿using System;
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
    internal readonly unsafe struct Register
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
        [FieldOffset(5)]
        internal readonly byte* pUInt8Values;

        [FieldOffset(5)]
        internal readonly sbyte* pInt8Values;

        [FieldOffset(5)]
        internal readonly ushort* pUInt16Values;

        [FieldOffset(5)]
        internal readonly short* pInt16Values;

        [FieldOffset(5)]
        internal readonly uint* pUInt32Values;

        [FieldOffset(5)]
        internal readonly int* pInt32Values;

        [FieldOffset(5)]
        internal readonly ulong* pUInt64Values;

        [FieldOffset(5)]
        internal readonly long* pInt64Values;

        [FieldOffset(5)]
        internal readonly float* pBinary32Values;

        [FieldOffset(5)]
        internal readonly double* pBinary64Values;


        [FieldOffset(5)]
        internal readonly Vector128<byte>* pVector128UInt8Values;

        [FieldOffset(5)]
        private readonly Vector128<sbyte>* pVector128Int8Values;

        [FieldOffset(5)]
        private readonly Vector128<ushort>* pVector128UInt16Values;

        [FieldOffset(5)]
        private readonly Vector128<short>* pVector128Int16Values;

        [FieldOffset(5)]
        private readonly Vector128<uint>* pVector128UInt32Values;

        [FieldOffset(5)]
        private readonly Vector128<int>* pVector128Int32Values;

        [FieldOffset(5)]
        private readonly Vector128<ulong>* pVector128UInt64Values;

        [FieldOffset(5)]
        private readonly Vector128<long>* pVector128Int64Values;

        [FieldOffset(5)]
        private readonly Vector128<float>* pVector128Binary32Values;

        [FieldOffset(5)]
        private readonly Vector128<double>* pVector128Binary64Values;


        [FieldOffset(5)]
        internal readonly Vector256<byte>* pVector256UInt8Values;

        [FieldOffset(5)]
        private readonly Vector256<sbyte>* pVector256Int8Values;

        [FieldOffset(5)]
        private readonly Vector256<ushort>* pVector256UInt16Values;

        [FieldOffset(5)]
        private readonly Vector256<short>* pVector256Int16Values;

        [FieldOffset(5)]
        private readonly Vector256<uint>* pVector256UInt32Values;

        [FieldOffset(5)]
        private readonly Vector256<int>* pVector256Int32Values;

        [FieldOffset(5)]
        private readonly Vector256<ulong>* pVector256UInt64Values;

        [FieldOffset(5)]
        private readonly Vector256<long>* pVector256Int64Values;

        [FieldOffset(5)]
        private readonly Vector256<float>* pVector256Binary32Values;

        [FieldOffset(5)]
        private readonly Vector256<double>* pVector256Binary64Values;

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

            pVector128UInt8Values = default;
            pVector128Int8Values = default;
            pVector128UInt16Values = default;
            pVector128Int16Values = default;
            pVector128UInt32Values = default;
            pVector128Int32Values = default;
            pVector128UInt64Values = default;
            pVector128Int64Values = default;
            pVector128Binary32Values = default;
            pVector128Binary64Values = default;

            pVector256UInt8Values = default;
            pVector256Int8Values = default;
            pVector256UInt16Values = default;
            pVector256Int16Values = default;
            pVector256UInt32Values = default;
            pVector256Int32Values = default;
            pVector256UInt64Values = default;
            pVector256Int64Values = default;
            pVector256Binary32Values = default;
            pVector256Binary64Values = default;

            byte[] bytes = new byte[count << bitShiftTypeSize];
            pUInt8Values = (byte*)Unsafe.As<byte[], IntPtr>(ref bytes).ToPointer();

            Constant = constant;
            Length = count;
#if DEBUG
            //Used in RegisterDebugView to make appropriately sized arrays for each type 
            ElementSize = typeSize / 8;

            //ElementType and ElementTypeLength are used in DebugToString which needs to know the correct type
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


        //TODO Decide if these need bounds checking for non constants. Provided at least some values in the subvector
        //exists but no all, Unsafe.As will still will return a vector but it will contain mostly junk and may lead to crashes
        internal Vector128<byte> GetVector128Byte(int index) =>
            Constant ? pVector128UInt8Values[0] : pVector128UInt8Values[index];

        internal Vector128<sbyte> GetVector128SByte(int index) =>
            Constant ? pVector128Int8Values[0] : pVector128Int8Values[index];

        internal Vector128<ushort> GetVector128UShort(int index) =>
            Constant ? pVector128UInt16Values[0] : pVector128UInt16Values[index];

        internal Vector128<short> GetVector128Short(int index) =>
            Constant ? pVector128Int16Values[0] : pVector128Int16Values[index];

        internal Vector128<uint> GetVector128UInt(int index) =>
            Constant ? pVector128UInt32Values[0] : pVector128UInt32Values[index];

        internal Vector128<int> GetVector128Int(int index) =>
            Constant ? pVector128Int32Values[0] : pVector128Int32Values[index];

        internal Vector128<ulong> GetVector128ULong(int index) =>
            Constant ? pVector128UInt64Values[0] : pVector128UInt64Values[index];

        internal Vector128<long> GetVector128Long(int index) =>
            Constant ? pVector128Int64Values[0] : pVector128Int64Values[index];

        internal Vector128<float> GetVector128Float(int index) =>
            Constant ? pVector128Binary32Values[0] : pVector128Binary32Values[index];

        internal Vector128<double> GetVector128Double(int index) =>
            Constant ? pVector128Binary64Values[0] : pVector128Binary64Values[index];


        internal Vector256<byte> GetVector256Byte(int index) =>
            Constant ? pVector256UInt8Values[0] : pVector256UInt8Values[index];

        internal Vector256<sbyte> GetVector256SByte(int index) =>
            Constant ? pVector256Int8Values[0] : pVector256Int8Values[index];

        internal Vector256<ushort> GetVector256UShort(int index) =>
            Constant ? pVector256UInt16Values[0] : pVector256UInt16Values[index];

        internal Vector256<short> GetVector256Short(int index) =>
            Constant ? pVector256Int16Values[0] : pVector256Int16Values[index];

        internal Vector256<uint> GetVector256UInt(int index) =>
            Constant ? pVector256UInt32Values[0] : pVector256UInt32Values[index];

        internal Vector256<int> GetVector256Int(int index) =>
            Constant ? pVector256Int32Values[0] : pVector256Int32Values[index];

        internal Vector256<ulong> GetVector256ULong(int index) =>
            Constant ? pVector256UInt64Values[0] : pVector256UInt64Values[index];

        internal Vector256<long> GetVector256Long(int index) =>
            Constant ? pVector256Int64Values[0] : pVector256Int64Values[index];

        internal Vector256<float> GetVector256Float(int index) =>
            Constant ? pVector256Binary32Values[0] : pVector256Binary32Values[index];

        internal Vector256<double> GetVector256Double(int index) =>
            Constant ? pVector256Binary64Values[0] : pVector256Binary64Values[index];



        //Closest thing possible to a generic indexer when Register cannot be generic,
        //the requirement to use Unsafe is not ideal
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


        internal byte GetByte(int index) => Constant ? pUInt8Values[0] : pUInt8Values[index];

        internal sbyte GetSByte(int index) => Constant ? pInt8Values[0] : pInt8Values[index];

        internal ushort GetUShort(int index) => Constant ? pUInt16Values[0] : pUInt16Values[index];

        internal short GetShort(int index) => Constant ? pInt16Values[0] : pInt16Values[index];

        internal uint GetUint(int index) => Constant ? pUInt32Values[0] : pUInt32Values[index];

        internal int GetInt(int index) => Constant ? pInt32Values[0] : pInt32Values[index];

        internal ulong GetULong(int index) => Constant ? pUInt64Values[0] : pUInt64Values[index];

        internal long GetLong(int index) => Constant ? pInt64Values[0] : pInt64Values[index];

        internal float GetFloat(int index) => Constant ? pBinary32Values[0] : pBinary32Values[index];

        internal double GetDouble(int index) => Constant ? pBinary64Values[0] : pBinary64Values[index];


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