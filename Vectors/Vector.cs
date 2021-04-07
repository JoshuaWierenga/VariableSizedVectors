//Contains most of Vector<T> from https://github.com/dotnet/runtime/blob/76a50c6/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector_1.cs which is under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Vectors
{
    //TODO Ensure only valid numeric value types can be used
    //TODO Readd support for operations and equality
    //TODO Support operations between vectors of different types, these may not be as fast however
    //since the smaller type has to be extended to handle the bigger type, small+big=big is a given though to avoid unneeded overflow
    //TODO Add Vector.Create shorthand that gives specific vector types automatically for general type inputs
    public readonly struct Vector<T> : IEquatable<Vector<T>>, IFormattable where T : struct
    {
        private readonly Register _vector;

        public readonly int Length => _vector.Length;


        public static Vector<T> Zero { get; } = new(Register.CreateConstant<T>(default));

        //TODO Fix, need different constant for each type
        //public static VectorDouble<T> One { get; } = new(1, true);

        //TODO This needs a new way to get all 1's
        //internal static VectorDouble<T> AllBitsSet { get; } = new(BitConverter.Int64BitsToDouble(-1), true);

        private Vector(Register register) => _vector = register;

        public Vector(T value) => _vector = Register.Create(value);

        public Vector(T[] values) : this(values, 0) { }

        public Vector(T[] values, int index)
        {
            if (values is null)
            {
                throw new NullReferenceException();
            }

            if (index < 0 || values.Length <= index)
            {
                throw new IndexOutOfRangeException();
            }

            //TODO Check performance of array[Range], is Span.Splice faster?
            _vector = Register.Create(values[index..]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<byte> values) => _vector = Register.Create<T>(values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<T> values) => _vector = Register.Create(values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(Span<T> values) : this((ReadOnlySpan<T>)values) { }


        //Used for operations on single value vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(U value) where U : struct => new(Register.Create(value));

        //Used on systems without x86 vector extensions for all operations and vector sizes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(U[] values) where U : struct => new(Register.Create(values));

        //Used for x86 Sse(2) optimised operations on 128 bit vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, Vector128<U> values) where U : struct =>
            new(Register.Create(count, values));

        //Used for x86 Sse(2) semi optimised operations on 192 bit vectors containing 64 bit types
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, Vector128<U> block128, U value) where U : struct =>
            new(Register.Create(count, block128, value));

        //Used for x86 Sse(2) semi optimised operations on 136 to 248 bit vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, Vector128<U> block128, U[] values) where U : struct =>
            new(Register.Create(count, block128, values));

        //Used for x86 Sse(2) semi optimised fallback operations on 256 bit vectors
        private Vector(int count, Vector128<T> firstBlock128, Vector128<T> secondBlock128) =>
            _vector = Register.Create(count, firstBlock128, secondBlock128);

        //Used for x86 Avx(2) optimised operations on 256 bit vectors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, Vector256<U> values) where U : struct =>
            new(Register.Create(count, values));

        //Used for x86 Sse(2) and Avx(2) optimised operations on vectors larger than 256 bits for all 64 bit types
        //as well as smaller types when the vector is composed of some number of 256 bit blocks, some number of 
        //128 bit blocks and then zero or one extra value
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, Vector256<U>[] blocks256 = null, Vector128<U>[] blocks128 = null,
            U? value = null) where U : struct => new(Register.Create(count, blocks256, blocks128, value));

        //Used for all x86 Sse(2) and Avx(2) optimised operations on vectors larger than 256 bits for all types
        //smaller than 64 bit where the vector is composed of some number of 256 bit blocks, some number of 128
        //bit blocks and then 2 or more extra values
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, Vector256<U>[] blocks256 = null, Vector128<U>[] blocks128 = null,
            U[] values = null) where U : struct => new(Register.Create(count, blocks256, blocks128, values));


        public readonly void CopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)(_vector.Length << BitShiftHelpers.SizeOf<T>()))
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            unsafe
            {
                Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination), ref _vector.pUInt8Values[0],
                    (uint)(_vector.Length << BitShiftHelpers.SizeOf<T>()));
            }
        }

        public readonly void CopyTo(Span<T> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(destination));
            }

            unsafe
            {

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)),
                    ref _vector.pUInt8Values[0], (uint)(_vector.Length << BitShiftHelpers.SizeOf<T>()));
            }
        }

        public readonly void CopyTo(T[] destination) => CopyTo(destination, 0);

        public readonly unsafe void CopyTo(T[] destination, int startIndex)
        {
            if (destination is null)
            {
                throw new NullReferenceException();
            }

            if ((uint)startIndex >= (uint)destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), ref _vector.pUInt8Values[0],
                (uint)(_vector.Length << BitShiftHelpers.SizeOf<T>()));
        }

        //TODO Fix, this will break for constant vectors when index >= the number of values of type T in 256 bits
        public readonly unsafe T this[int index] => _vector.GetValueUnsafe<T>(index);

        public readonly unsafe Vector<T> Slice(int start, int length) => new(
            new Span<T>(_vector.pUInt8Values, _vector.Length << BitShiftHelpers.SizeOf<T>()).Slice(start, length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
            (obj is Vector<T> other) && Equals(other);

        public readonly bool Equals(Vector<T> other) => this == other;

        public override readonly int GetHashCode()
        {
            if (_vector.Length == 1)
            {
                return _vector.GetValueUnsafe<T>(0).GetHashCode();
            }

            HashCode hashCode = default;
            ReadOnlySpan<T> values = _vector.GetValues<T>();

            for (int i = 0; i < _vector.Length; i++)
            {
                hashCode.Add(values[i]);
            }

            return hashCode.ToHashCode();
        }

        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

        public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            StringBuilder sb = new();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            ReadOnlySpan<T> values = _vector.GetValues<T>();

            sb.Append('<');
            sb.Append(((IFormattable)values[0]).ToString(format, formatProvider));

            //TODO Decide if the hardcoded cases should be inside each other with if statements like case 3-4 or kept separate as they are now
            switch (_vector.Length)
            {
                case 1:
                    break;
                case 2:
                    sb.Append(separator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)values[1]).ToString(format, formatProvider));
                    break;
                case 3:
                case 4:
                    sb.Append(separator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)values[1]).ToString(format, formatProvider));
                    sb.Append(separator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)values[2]).ToString(format, formatProvider));
                    if (_vector.Length == 4)
                    {
                        sb.Append(separator);
                        sb.Append(' ');
                        sb.Append(((IFormattable)values[3]).ToString(format, formatProvider));
                    }
                    break;

                default:
                    for (int i = 1; i < _vector.Length; i++)
                    {
                        sb.Append(separator);
                        sb.Append(' ');
                        sb.Append(((IFormattable)values[i]).ToString(format, formatProvider));
                    }
                    break;
            }

            sb.Append('>');
            return sb.ToString();
        }

        public readonly bool TryCopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Length << BitShiftHelpers.SizeOf<T>())
            {
                return false;
            }

            unsafe
            {
                Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination), ref _vector.pUInt8Values[0],
                    (uint)(_vector.Length << BitShiftHelpers.SizeOf<T>()));
                return true;
            }
        }

        public readonly bool TryCopyTo(Span<T> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Length)
            {
                return false;
            }

            unsafe
            {
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)),
                    ref _vector.pUInt8Values[0], (uint)(_vector.Length << BitShiftHelpers.SizeOf<T>()));
                return true;
            }
        }

        //Operators
        public static Vector<T> operator +(Vector<T> left, Vector<T> right)
        {
            int count;

            if (left._vector.Constant)
            {
                count = right._vector.Length;
            }
            else if (right._vector.Constant)
            {
                count = left._vector.Length;
            }
            else if (left._vector.Length != right._vector.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                count = left._vector.Length;
            }

            return Add(left, right, count);
        }

        public static Vector<T> operator -(Vector<T> left, Vector<T> right)
        {
            int size;

            if (left._vector.Constant)
            {
                size = right._vector.Length;
            }
            else if (right._vector.Constant)
            {
                size = left._vector.Length;
            }
            else if (left._vector.Length != right._vector.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                //return new Vector<T>(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                /*return new Vector<T>(Avx.Subtract(left._vector.ToVector256(0), right._vector.ToVector256(0)),
                    size);*/

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    left._vector.GetValue<T>(2) - right._vector.GetValue<T>(2));*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    Sse2.Subtract(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector.GetValue<T>(0) - right._vector.GetValue<T>(0));
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) - right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) - right._vector.GetValue<T>(1)
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) - right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) - right._vector.GetValue<T>(1),
                        left._vector.GetValue<T>(2) - right._vector.GetValue<T>(2)
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) - right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) - right._vector.GetValue<T>(1),
                        left._vector.GetValue<T>(2) - right._vector.GetValue<T>(2),
                        left._vector.GetValue<T>(3) - right._vector.GetValue<T>(3)
                    }, 0);*/
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector.GetValue<T>(i) - right._vector.GetValue<T>(i);
                        }

                        return new Vector<T>(values64);
                    }

                    Vector128<T>[] blocks128 = null;
                    Vector256<T>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<T>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            //blocks256[i] = Avx.Subtract(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<T>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            //blocks128[i] = Sse2.Subtract(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        /*return new Vector<T>(size,
                            left._vector[processedSubOperations] - right._vector[processedSubOperations], blocks128,
                            blocks256);*/
                    }

                    return new Vector<T>(new T[size]);
            }
        }

        //TODO Add Dot/Transposed Multiplication, Cross?
        //Element Wise Multiplication
        public static Vector<T> operator *(Vector<T> left, Vector<T> right)
        {
            int size;

            if (left._vector.Constant)
            {
                size = right._vector.Length;
            }
            else if (right._vector.Constant)
            {
                size = left._vector.Length;
            }
            else if (left._vector.Length != right._vector.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                //return new Vector<T>(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                /*return new Vector<T>(Avx.Multiply(left._vector.ToVector256(0), right._vector.ToVector256(0)),
                    size);*/

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    left._vector.GetValue<T>(2) * right._vector.GetValue<T>(2));*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    Sse2.Multiply(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector.GetValue<T>(0) * right._vector.GetValue<T>(0));
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) * right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) * right._vector.GetValue<T>(1)
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) * right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) * right._vector.GetValue<T>(1),
                        left._vector.GetValue<T>(2) * right._vector.GetValue<T>(2)
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) * right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) * right._vector.GetValue<T>(1),
                        left._vector.GetValue<T>(2) * right._vector.GetValue<T>(2),
                        left._vector.GetValue<T>(3) * right._vector.GetValue<T>(3)
                    }, 0);*/
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector.GetValue<T>(i) * right._vector.GetValue<T>(i);
                        }

                        return new Vector<T>(values64);
                    }

                    Vector128<T>[] blocks128 = null;
                    Vector256<T>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<T>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            //blocks256[i] = Avx.Multiply(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<T>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            //blocks128[i] = Sse2.Multiply(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        /*return new Vector<T>(size,
                            left._vector[processedSubOperations] * right._vector[processedSubOperations], blocks128,
                            blocks256);*/
                    }

                    return new Vector<T>(new T[size]);
            }
        }

        //Element wise multiplication
        public static Vector<T> operator *(Vector<T> value, double factor)
        {
            //TODO Add Sse2/Avx support? Requires broadcasting factor to a vector and then multiplying with full/partial size vector instruction support
            switch (value._vector.Length)
            {
                case 1:
                //return new Vector<T>(value._vector.GetValue<T>(0) * factor);
                case 2:
                /*return new Vector<T>(
                    new[] { value._vector.GetValue<T>(0) * factor, value._vector.GetValue<T>(1) * factor }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        value._vector.GetValue<T>(0) * factor, value._vector.GetValue<T>(1) * factor,
                        value._vector.GetValue<T>(2) * factor
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        value._vector.GetValue<T>(0) * factor, value._vector.GetValue<T>(1) * factor,
                        value._vector.GetValue<T>(2) * factor, value._vector.GetValue<T>(3) * factor
                    }, 0);*/
                default:
                    T[] newValues = new T[value._vector.Length];
                    for (int i = 0; i < value._vector.Length; i++)
                    {
                        //newValues[i] = value._vector.GetValue<T>(i) * factor;
                    }

                    return new Vector<T>(newValues);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(double factor, Vector<T> value) => value * factor;

        //Element wise division
        public static Vector<T> operator /(Vector<T> left, Vector<T> right)
        {
            int size;

            if (left._vector.Constant)
            {
                size = right._vector.Length;
            }
            else if (right._vector.Constant)
            {
                size = left._vector.Length;
            }
            else if (left._vector.Length != right._vector.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                //return new Vector<T>(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                /*return new Vector<T>(Avx.Divide(left._vector.ToVector256(0), right._vector.ToVector256(0)),
                    size);*/

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    left._vector.GetValue<T>(2) / right._vector.GetValue<T>(2));*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    Sse2.Divide(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector.GetValue<T>(0) / right._vector.GetValue<T>(0));
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) / right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) / right._vector.GetValue<T>(1)
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) / right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) / right._vector.GetValue<T>(1),
                        left._vector.GetValue<T>(2) / right._vector.GetValue<T>(2)
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector.GetValue<T>(0) / right._vector.GetValue<T>(0),
                        left._vector.GetValue<T>(1) / right._vector.GetValue<T>(1),
                        left._vector.GetValue<T>(2) / right._vector.GetValue<T>(2),
                        left._vector.GetValue<T>(3) / right._vector.GetValue<T>(3)
                    }, 0);*/
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector.GetValue<T>(i) / right._vector.GetValue<T>(i);
                        }

                        return new Vector<T>(values64);
                    }

                    Vector128<T>[] blocks128 = null;
                    Vector256<T>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<T>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            //blocks256[i] = Avx.Divide(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<T>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            //blocks128[i] = Sse2.Divide(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        /*return new Vector<T>(size,
                            left._vector[processedSubOperations] / right._vector[processedSubOperations], blocks128,
                            blocks256);*/
                    }

                    return new Vector<T>(new T[size]);
            }
        }

        //TODO Fix constants
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> value) => Zero - value;

        //TODO Add bitwise operations when integer types are supported

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector<T> left, Vector<T> right)
        {
            //Shortcut if both refer to the same memory location
            unsafe
            {
                if (left._vector.pUInt8Values == right._vector.pUInt8Values)
                {
                    return true;
                }
            }

            int size;

            if (left._vector.Constant)
            {
                size = right._vector.Length;
            }
            else if (right._vector.Constant)
            {
                size = left._vector.Length;
            }
            else if (left._vector.Length != right._vector.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Length;
            }

            if (typeof(T) == typeof(float))
            {
                switch (size)
                {
                    case 8 when IntrinsicSupport.IsAvxSupported:
                    {
                        Vector256<float> result = Avx.Compare(left._vector.GetVector256Float(0),
                            right._vector.GetVector256Float(0), FloatComparisonMode.OrderedEqualNonSignaling);
                        return Avx.MoveMask(result) == 0b11111111;
                    }
                    case 4 when IntrinsicSupport.IsSseSupported:
                    {
                        Vector128<float> result = Sse.CompareEqual(left._vector.GetVector128Float(0),
                            right._vector.GetVector128Float(0));
                        return Sse.MoveMask(result) == 0b1111;
                    }
                }
            }
            else if (typeof(T) == typeof(double))
            {
                switch (size)
                {
                    case 4 when IntrinsicSupport.IsAvxSupported:
                    {
                        Vector256<double> result = Avx.Compare(left._vector.GetVector256Double(0),
                            right._vector.GetVector256Double(0), FloatComparisonMode.OrderedEqualNonSignaling);
                        return Avx.MoveMask(result) == 0b1111;
                    }
                    case 2 when IntrinsicSupport.IsSse2Supported:
                    {
                        Vector128<double> result = Sse2.CompareEqual(left._vector.GetVector128Double(0),
                            right._vector.GetVector128Double(0));
                        return Sse2.MoveMask(result) == 0b11;
                    }
                }
            }
            //There are no MoveMask commands for types other than byte, float and double so just using byte
            //Comments on https://stackoverflow.com/a/47244428 suggest that this is fine and shouldn't affect performance
            else
            {
                if (size == SizeHelpers.NumberIn256Bits<T>() && IntrinsicSupport.IsAvx2Supported)
                {
                    Vector256<byte> result = Avx2.CompareEqual(left._vector.GetVector256Byte(0),
                        right._vector.GetVector256Byte(0));
                    return unchecked((uint)Avx2.MoveMask(result)) == 0xFFFFFFFF;
                }
                //TODO Add support for 136 though 248 bit vectors
                //TODO Add Sse2 fallback for 256 bit vectors
                if (size == NumberIn128Bits() && IntrinsicSupport.IsSse2Supported)
                {
                    Vector128<byte> result = Sse2.CompareEqual(left._vector.GetVector128Byte(0),
                        right._vector.GetVector128Byte(0));
                    //TODO Confirm this needs a different number for comparision of different T
                    return Sse2.MoveMask(result) == 0xFFFF;
                }
                //TODO Check if arm supports Vector64<T> equality checking and if so use it
            }

            ReadOnlySpan<T> leftValues = left._vector.GetValues<T>();
            ReadOnlySpan<T> rightValues = right._vector.GetValues<T>();

            //Assumption is made that no Sse support means no Avx support
            //TODO Remove true once single value and accelerated fallbacks are working, i.e. 136-248 and >256
            if (true || !IntrinsicSupport.IsSseSupported)
            {
                for (int i = 0; i < size; i++)
                {
                    if (!leftValues[i].Equals(rightValues[i]))
                    {
                        return false;
                    }
                }
            }

            switch (size)
            {
                //Partial size vector instructions
                //case 3 when Sse2.IsSupported:
                /*{
                    Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                    return Sse2.MoveMask(result) == 0b11 && left._vector.GetValue<T>(2).Equals(right._vector.GetValue<T>(2));
                }*/
                //case 4 when Sse2.IsSupported:
                /*{
                    Vector128<double> result1 = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                    Vector128<double> result2 = Sse2.CompareEqual(left._vector.ToVector128(1), right._vector.ToVector128(1));
                    return Sse2.MoveMask(result1) == 0b11 && Sse2.MoveMask(result2) == 0b11;
                }*/

                //Software fallback
                case 1 when leftValues[0].Equals(rightValues[0]):
                case 2 when leftValues[0].Equals(rightValues[0]) && leftValues[1].Equals(rightValues[1]):
                case 3 when leftValues[0].Equals(rightValues[0]) && leftValues[1].Equals(rightValues[1]) &&
                            leftValues[2].Equals(rightValues[2]):
                case 4 when leftValues[0].Equals(rightValues[0]) && leftValues[1].Equals(rightValues[1]) &&
                            leftValues[2].Equals(rightValues[2]) && leftValues[3].Equals(rightValues[3]):
                    return true;
                case > 4:
                    int remainingSubOperations = size;
                    //int processedSubOperations = 0;

                    if (Avx.IsSupported)
                    {
                        int count256 = remainingSubOperations >> 2;

                        for (int i = 0; i < count256; i++)
                        {
                            /*Vector256<double> result = Avx.Compare(left._vector.ToVector256(0), right._vector.ToVector256(0),
                                FloatComparisonMode.OrderedEqualNonSignaling);
                            if (Avx.MoveMask(result) != 0b1111)
                            {
                                return false;
                            }*/
                        }

                        remainingSubOperations -= count256 << 2;
                        //processedSubOperations += count256 << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        int count128 = remainingSubOperations >> 1;

                        for (int i = 0; i < count128; i++)
                        {
                            /*Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                            if (Sse2.MoveMask(result) != 0b11)
                            {
                                return false;
                            }*/
                        }

                        remainingSubOperations -= count128 << 1;
                        //processedSubOperations += count128 << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        //TODO Fix this and the rest of this function
                        return false;
                        //return left._vector.GetValue<T>(processedSubOperations).Equals(right._vector.GetValue<T>(processedSubOperations));
                    }

                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector<T> left, Vector<T> right) => !(left == right);


        //TODO Find way to merge branches since other than types, they all contain the same code
        //TODO Ensure constants are set correctly, is it worth making the constant for each type a variable in each if case?
        //There are functions in Register<T> to calculate them but that feels unnecessary since those are for generic cases
        //This method performs addition on the vectors left and right.
        //If x86 vector extensions are supported then this method first handles the conversion of Vector<T>s to some
        //intermediate Vector128<U>/Vector256<U> where U is the same as T but has to be named differently to get around
        //c#'s restriction of x86 vector extension operations to be in terms of concrete types.
        //It then performs addition using Sse(2) or Avx(2) and then casts the resulting VectorX<U> to a new Vector<T>
        //object that is then returned.
        //If the right size of vector extensions is not supported then the operations are completed one at a time using
        //scalar operations.
        //Additionally, in the case of vectors larger than 256 bits, the vector is broken up into 256 bit, 128 bit and
        //scalar components, operated on and then put back together.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Add(Vector<T> left, Vector<T> right, int count)
        {
            if (typeof(T) == typeof(byte))
            {
                switch (count)
                {
                    case 32 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256Byte(0), right._vector.GetVector256Byte(0)));
                    case < 32 and > 16 when IntrinsicSupport.IsSse2Supported:
                        Vector128<byte> lower128 = Sse2.Add(left._vector.GetVector128Byte(0),
                            right._vector.GetVector128Byte(0));

                        byte[] upperValues = new byte[count - 16];
                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 16;

                        //TODO Figure out if this is any faster than the for loop that was here
                        if (remaining >= 8)
                        {
                            upperValues[0] = (byte)(left._vector.GetByte(16) + right._vector.GetByte(16));
                            upperValues[1] = (byte)(left._vector.GetByte(17) + right._vector.GetByte(17));
                            upperValues[2] = (byte)(left._vector.GetByte(18) + right._vector.GetByte(18));
                            upperValues[3] = (byte)(left._vector.GetByte(19) + right._vector.GetByte(19));
                            upperValues[4] = (byte)(left._vector.GetByte(20) + right._vector.GetByte(20));
                            upperValues[5] = (byte)(left._vector.GetByte(21) + right._vector.GetByte(21));
                            upperValues[6] = (byte)(left._vector.GetByte(22) + right._vector.GetByte(22));
                            upperValues[8] = (byte)(left._vector.GetByte(23) + right._vector.GetByte(23));
                            position = 8;
                            remaining -= 8;
                            arrayPosition = 24;
                        }

                        if (remaining >= 4)
                        {
                            upperValues[position++] = (byte)(left._vector.GetByte(arrayPosition) +
                                                              right._vector.GetByte(arrayPosition++));
                            upperValues[position++] = (byte)(left._vector.GetByte(arrayPosition) +
                                                              right._vector.GetByte(arrayPosition++));
                            upperValues[position++] = (byte)(left._vector.GetByte(arrayPosition) +
                                                              right._vector.GetByte(arrayPosition++));
                            upperValues[position++] = (byte)(left._vector.GetByte(arrayPosition) +
                                                              right._vector.GetByte(arrayPosition++));
                            remaining -= 4;
                        }

                        if (remaining >= 2)
                        {
                            upperValues[position++] = (byte)(left._vector.GetByte(arrayPosition) +
                                                              right._vector.GetByte(arrayPosition++));
                            upperValues[position++] = (byte)(left._vector.GetByte(arrayPosition) +
                                                              right._vector.GetByte(arrayPosition++));
                            remaining -= 2;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] = (byte)(left._vector.GetByte(arrayPosition) +
                                                            right._vector.GetByte(arrayPosition));
                        }

                        return Create(count, lower128, upperValues);
                    case 16 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128Byte(0), right._vector.GetVector128Byte(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 8 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create((byte)(left._vector.GetByte(0) + right._vector.GetByte(0)));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            byte[] values = new byte[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = (byte)(left._vector.GetByte(i) + right._vector.GetByte(i));
                            }

                            return Create(values);
                        }

                        Vector128<byte>[] blocks128 = null;
                        Vector256<byte>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 32)
                        {
                            //remainingSubOperations >> 5 = remainingSubOperations / 32
                            blocks256 = new Vector256<byte>[remainingSubOperations >> 5];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256Byte(i),
                                    right._vector.GetVector256Byte(i));
                            }

                            processedSubOperations = blocks256.Length << 5;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 16)
                        {
                            //remainingSubOperations >> 4 = remainingSubOperations / 16
                            blocks128 = new Vector128<byte>[remainingSubOperations >> 4];

                            for (int i = 0, j = processedSubOperations >> 4; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128Byte(j),
                                    right._vector.GetVector128Byte(j));
                            }

                            processedSubOperations += blocks128.Length << 4;
                            remainingSubOperations -= blocks128.Length << 4;
                        }

                        if (remainingSubOperations > 1)
                        {
                            byte[] values = new byte[remainingSubOperations];
                            position = 0;

                            //TODO Figure out if this is any faster than the for loop that was here
                            if (remainingSubOperations >= 8)
                            {
                                values[0] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[1] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[2] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[3] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[4] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[5] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[6] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                values[7] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                    right._vector.GetByte(processedSubOperations++));
                                position = 8;
                                remainingSubOperations -= 8;
                            }

                            if (remainingSubOperations >= 4)
                            {
                                values[position++] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                  right._vector.GetByte(processedSubOperations++));
                                values[position++] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                  right._vector.GetByte(processedSubOperations++));
                                values[position++] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                  right._vector.GetByte(processedSubOperations++));
                                values[position++] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                  right._vector.GetByte(processedSubOperations++));
                                remainingSubOperations -= 4;
                            }

                            if (remainingSubOperations >= 2)
                            {
                                values[position++] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                  right._vector.GetByte(processedSubOperations++));
                                values[position++] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                  right._vector.GetByte(processedSubOperations++));
                                remainingSubOperations -= 2;
                            }

                            if (remainingSubOperations == 1)
                            {
                                values[position] = (byte)(left._vector.GetByte(processedSubOperations) +
                                                                right._vector.GetByte(processedSubOperations));
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                (byte)(left._vector.GetByte(processedSubOperations) +
                                        right._vector.GetByte(processedSubOperations)));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(sbyte))
            {
                switch (count)
                {
                    case 32 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256SByte(0), right._vector.GetVector256SByte(0)));
                    case < 32 and > 16 when IntrinsicSupport.IsSse2Supported:
                        Vector128<sbyte> lower128 = Sse2.Add(left._vector.GetVector128SByte(0),
                            right._vector.GetVector128SByte(0));

                        sbyte[] upperValues = new sbyte[count - 16];
                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 16;

                        //TODO Figure out if this is any faster than the for loop that was here
                        if (remaining >= 8)
                        {
                            upperValues[0] = (sbyte)(left._vector.GetSByte(16) + right._vector.GetSByte(16));
                            upperValues[1] = (sbyte)(left._vector.GetSByte(17) + right._vector.GetSByte(17));
                            upperValues[2] = (sbyte)(left._vector.GetSByte(18) + right._vector.GetSByte(18));
                            upperValues[3] = (sbyte)(left._vector.GetSByte(19) + right._vector.GetSByte(19));
                            upperValues[4] = (sbyte)(left._vector.GetSByte(20) + right._vector.GetSByte(20));
                            upperValues[5] = (sbyte)(left._vector.GetSByte(21) + right._vector.GetSByte(21));
                            upperValues[6] = (sbyte)(left._vector.GetSByte(22) + right._vector.GetSByte(22));
                            upperValues[8] = (sbyte)(left._vector.GetSByte(23) + right._vector.GetSByte(23));
                            position = 8;
                            remaining -= 8;
                            arrayPosition = 24;
                        }

                        if (remaining >= 4)
                        {
                            upperValues[position++] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                              right._vector.GetSByte(arrayPosition++));
                            upperValues[position++] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                              right._vector.GetSByte(arrayPosition++));
                            upperValues[position++] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                              right._vector.GetSByte(arrayPosition++));
                            upperValues[position++] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                              right._vector.GetSByte(arrayPosition++));
                            remaining -= 4;
                        }

                        if (remaining >= 2)
                        {
                            upperValues[position++] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                              right._vector.GetSByte(arrayPosition++));
                            upperValues[position++] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                              right._vector.GetSByte(arrayPosition++));
                            remaining -= 2;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] = (sbyte)(left._vector.GetSByte(arrayPosition) +
                                                            right._vector.GetSByte(arrayPosition));
                        }

                        return Create(count, lower128, upperValues);
                    case 16 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128Short(0), right._vector.GetVector128Short(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 8 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create((sbyte)(left._vector.GetSByte(0) + right._vector.GetSByte(0)));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            sbyte[] values = new sbyte[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = (sbyte)(left._vector.GetSByte(i) + right._vector.GetSByte(i));
                            }

                            return Create(values);
                        }

                        Vector128<sbyte>[] blocks128 = null;
                        Vector256<sbyte>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 32)
                        {
                            //remainingSubOperations >> 5 = remainingSubOperations / 32
                            blocks256 = new Vector256<sbyte>[remainingSubOperations >> 5];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256SByte(i),
                                    right._vector.GetVector256SByte(i));
                            }

                            processedSubOperations = blocks256.Length << 5;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 16)
                        {
                            //remainingSubOperations >> 4 = remainingSubOperations / 16
                            blocks128 = new Vector128<sbyte>[remainingSubOperations >> 4];

                            for (int i = 0, j = processedSubOperations >> 4; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128SByte(j),
                                    right._vector.GetVector128SByte(j));
                            }

                            processedSubOperations += blocks128.Length << 4;
                            remainingSubOperations -= blocks128.Length << 4;
                        }

                        if (remainingSubOperations > 1)
                        {
                            sbyte[] values = new sbyte[remainingSubOperations];
                            position = 0;

                            //TODO Figure out if this is any faster than the for loop that was here
                            if (remainingSubOperations >= 8)
                            {
                                values[0] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[1] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[2] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[3] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[4] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[5] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[6] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                values[7] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                    right._vector.GetSByte(processedSubOperations++));
                                position = 8;
                                remainingSubOperations -= 8;
                            }

                            if (remainingSubOperations >= 4)
                            {
                                values[position++] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                  right._vector.GetSByte(processedSubOperations++));
                                values[position++] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                  right._vector.GetSByte(processedSubOperations++));
                                values[position++] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                  right._vector.GetSByte(processedSubOperations++));
                                values[position++] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                  right._vector.GetSByte(processedSubOperations++));
                                remainingSubOperations -= 4;
                            }

                            if (remainingSubOperations >= 2)
                            {
                                values[position++] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                  right._vector.GetSByte(processedSubOperations++));
                                values[position++] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                  right._vector.GetSByte(processedSubOperations++));
                                remainingSubOperations -= 2;
                            }

                            if (remainingSubOperations == 1)
                            {
                                values[position] = (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                                                right._vector.GetSByte(processedSubOperations));
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                (sbyte)(left._vector.GetSByte(processedSubOperations) +
                                         right._vector.GetSByte(processedSubOperations)));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(ushort))
            {
                switch (count)
                {
                    case 16 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256UShort(0), right._vector.GetVector256UShort(0)));
                    case < 16 and > 8 when IntrinsicSupport.IsSse2Supported:
                        Vector128<ushort> lower128 = Sse2.Add(left._vector.GetVector128UShort(0),
                            right._vector.GetVector128UShort(0));

                        ushort[] upperValues = new ushort[count - 8];
                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 8;

                        //TODO Figure out if this is any faster than the for loop that was here
                        if (remaining >= 4)
                        {
                            upperValues[0] = (ushort)(left._vector.GetUShort(8) + right._vector.GetUShort(8));
                            upperValues[1] = (ushort)(left._vector.GetUShort(9) + right._vector.GetUShort(9));
                            upperValues[2] = (ushort)(left._vector.GetUShort(10) + right._vector.GetUShort(10));
                            upperValues[3] = (ushort)(left._vector.GetUShort(11) + right._vector.GetUShort(11));
                            position = 4;
                            remaining -= 4;
                            arrayPosition = 12;
                        }

                        if (remaining >= 2)
                        {
                            upperValues[position++] = (ushort)(left._vector.GetUShort(arrayPosition) +
                                                              right._vector.GetUShort(arrayPosition++));
                            upperValues[position++] = (ushort)(left._vector.GetUShort(arrayPosition) +
                                                              right._vector.GetUShort(arrayPosition++));
                            remaining -= 2;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] = (ushort)(left._vector.GetUShort(arrayPosition) +
                                                            right._vector.GetUShort(arrayPosition));
                        }

                        return Create(count, lower128, upperValues);
                    case 8 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128UShort(0), right._vector.GetVector128UShort(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 4 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create((ushort)(left._vector.GetUShort(0) + right._vector.GetUShort(0)));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            ushort[] values = new ushort[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = (ushort)(left._vector.GetUShort(i) + right._vector.GetUShort(i));
                            }

                            return Create(values);
                        }

                        Vector128<ushort>[] blocks128 = null;
                        Vector256<ushort>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 16)
                        {
                            //remainingSubOperations >> 4 = remainingSubOperations / 16
                            blocks256 = new Vector256<ushort>[remainingSubOperations >> 4];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256UShort(i),
                                    right._vector.GetVector256UShort(i));
                            }

                            processedSubOperations = blocks256.Length << 4;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 8)
                        {
                            //remainingSubOperations >> 3 = remainingSubOperations / 8
                            blocks128 = new Vector128<ushort>[remainingSubOperations >> 3];

                            for (int i = 0, j = processedSubOperations >> 3; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128UShort(j),
                                    right._vector.GetVector128UShort(j));
                            }

                            processedSubOperations += blocks128.Length << 3;
                            remainingSubOperations -= blocks128.Length << 3;
                        }

                        if (remainingSubOperations > 1)
                        {
                            ushort[] values = new ushort[remainingSubOperations];
                            position = 0;

                            //TODO Figure out if this is any faster than the for loop that was here
                            if (remainingSubOperations >= 4)
                            {
                                values[0] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                  right._vector.GetUShort(processedSubOperations++));
                                values[1] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                  right._vector.GetUShort(processedSubOperations++));
                                values[2] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                  right._vector.GetUShort(processedSubOperations++));
                                values[3] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                  right._vector.GetUShort(processedSubOperations++));
                                position = 4;
                                remainingSubOperations -= 4;
                            }

                            if (remainingSubOperations >= 2)
                            {
                                values[position++] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                  right._vector.GetUShort(processedSubOperations++));
                                values[position++] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                  right._vector.GetUShort(processedSubOperations++));
                                remainingSubOperations -= 2;
                            }

                            if (remainingSubOperations == 1)
                            {
                                values[position] = (ushort)(left._vector.GetUShort(processedSubOperations) +
                                                                right._vector.GetUShort(processedSubOperations));
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                (ushort)(left._vector.GetUShort(processedSubOperations) +
                                         right._vector.GetUShort(processedSubOperations)));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(short))
            {
                switch (count)
                {
                    case 16 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256Short(0), right._vector.GetVector256Short(0)));
                    case < 16 and > 8 when IntrinsicSupport.IsSse2Supported:
                        Vector128<short> lower128 = Sse2.Add(left._vector.GetVector128Short(0),
                            right._vector.GetVector128Short(0));

                        short[] upperValues = new short[count - 8];
                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 8;

                        //TODO Figure out if this is any faster than the for loop that was here
                        if (remaining >= 4)
                        {
                            upperValues[0] = (short)(left._vector.GetShort(8) + right._vector.GetShort(8));
                            upperValues[1] = (short)(left._vector.GetShort(9) + right._vector.GetShort(9));
                            upperValues[2] = (short)(left._vector.GetShort(10) + right._vector.GetShort(10));
                            upperValues[3] = (short)(left._vector.GetShort(11) + right._vector.GetShort(11));
                            position = 4;
                            remaining -= 4;
                            arrayPosition = 12;
                        }

                        if (remaining >= 2)
                        {
                            upperValues[position++] = (short)(left._vector.GetShort(arrayPosition) +
                                                               right._vector.GetShort(arrayPosition++));
                            upperValues[position++] = (short)(left._vector.GetShort(arrayPosition) +
                                                               right._vector.GetShort(arrayPosition++));
                            remaining -= 2;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] = (short)(left._vector.GetShort(arrayPosition) +
                                                             right._vector.GetShort(arrayPosition));
                        }

                        return Create(count, lower128, upperValues);
                    case 8 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128Short(0), right._vector.GetVector128Short(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 4 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create((short)(left._vector.GetShort(0) + right._vector.GetShort(0)));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            short[] values = new short[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = (short)(left._vector.GetShort(i) + right._vector.GetShort(i));
                            }

                            return Create(values);
                        }

                        Vector128<short>[] blocks128 = null;
                        Vector256<short>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 16)
                        {
                            //remainingSubOperations >> 4 = remainingSubOperations / 16
                            blocks256 = new Vector256<short>[remainingSubOperations >> 4];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256Short(i),
                                    right._vector.GetVector256Short(i));
                            }

                            processedSubOperations = blocks256.Length << 4;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 8)
                        {
                            //remainingSubOperations >> 3 = remainingSubOperations / 8
                            blocks128 = new Vector128<short>[remainingSubOperations >> 3];

                            for (int i = 0, j = processedSubOperations >> 3; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128Short(j),
                                    right._vector.GetVector128Short(j));
                            }

                            processedSubOperations += blocks128.Length << 3;
                            remainingSubOperations -= blocks128.Length << 3;
                        }

                        if (remainingSubOperations > 1)
                        {
                            short[] values = new short[remainingSubOperations];
                            position = 0;

                            //TODO Figure out if this is any faster than the for loop that was here
                            if (remainingSubOperations >= 4)
                            {
                                values[0] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                  right._vector.GetShort(processedSubOperations++));
                                values[1] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                  right._vector.GetShort(processedSubOperations++));
                                values[2] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                  right._vector.GetShort(processedSubOperations++));
                                values[3] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                  right._vector.GetShort(processedSubOperations++));
                                position = 4;
                                remainingSubOperations -= 4;
                            }

                            if (remainingSubOperations >= 2)
                            {
                                values[position++] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                  right._vector.GetShort(processedSubOperations++));
                                values[position++] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                  right._vector.GetShort(processedSubOperations++));
                                remainingSubOperations -= 2;
                            }

                            if (remainingSubOperations == 1)
                            {
                                values[position] = (short)(left._vector.GetShort(processedSubOperations) +
                                                                right._vector.GetShort(processedSubOperations));
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                (short)(left._vector.GetShort(processedSubOperations) +
                                         right._vector.GetShort(processedSubOperations)));
                        }
                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(uint))
            {
                switch (count)
                {
                    case 8 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256UInt(0), right._vector.GetVector256UInt(0)));
                    case < 8 and > 4 when IntrinsicSupport.IsSse2Supported:
                        Vector128<uint> lower128 = Sse2.Add(left._vector.GetVector128UInt(0),
                            right._vector.GetVector128UInt(0));

                        uint[] upperValues = new uint[count - 4];
                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 4;

                        if (remaining >= 2)
                        {
                            upperValues[0] = left._vector.GetUInt(4) + right._vector.GetUInt(4);
                            upperValues[1] = left._vector.GetUInt(5) + right._vector.GetUInt(5);
                            position = 2;
                            remaining -= 2;
                            arrayPosition = 6;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] = left._vector.GetUInt(arrayPosition) +
                                                    right._vector.GetUInt(arrayPosition);
                        }

                        return Create(count, lower128, upperValues);
                    case 4 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128UInt(0), right._vector.GetVector128UInt(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 2 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create(left._vector.GetUInt(0) + right._vector.GetUInt(0));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            uint[] values = new uint[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = left._vector.GetUInt(i) + right._vector.GetUInt(i);
                            }

                            return Create(values);
                        }

                        Vector128<uint>[] blocks128 = null;
                        Vector256<uint>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 8)
                        {
                            //remainingSubOperations >> 3 = remainingSubOperations / 8
                            blocks256 = new Vector256<uint>[remainingSubOperations >> 3];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256UInt(i),
                                    right._vector.GetVector256UInt(i));
                            }

                            processedSubOperations = blocks256.Length << 3;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 4)
                        {
                            //remainingSubOperations >> 2 = remainingSubOperations / 4
                            blocks128 = new Vector128<uint>[remainingSubOperations >> 2];

                            for (int i = 0, j = processedSubOperations >> 2; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128UInt(j),
                                    right._vector.GetVector128UInt(j));
                            }

                            processedSubOperations += blocks128.Length << 2;
                            remainingSubOperations -= blocks128.Length << 2;
                        }

                        if (remainingSubOperations > 1)
                        {
                            uint[] values = new uint[remainingSubOperations];

                            values[0] = left._vector.GetUInt(processedSubOperations) +
                                        right._vector.GetUInt(processedSubOperations++);
                            values[1] = left._vector.GetUInt(processedSubOperations) +
                                        right._vector.GetUInt(processedSubOperations++);

                            if (remainingSubOperations == 3)
                            {
                                values[2] = left._vector.GetUInt(processedSubOperations) +
                                            right._vector.GetUInt(processedSubOperations);
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                left._vector.GetUInt(processedSubOperations) +
                                right._vector.GetUInt(processedSubOperations));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(int))
            {
                switch (count)
                {
                    case 8 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256Int(0), right._vector.GetVector256Int(0)));
                    case < 8 and > 4 when IntrinsicSupport.IsSse2Supported:
                        Vector128<int> lower128 = Sse2.Add(left._vector.GetVector128Int(0),
                            right._vector.GetVector128Int(0));

                        int[] upperValues = new int[count - 4];
                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 4;

                        if (remaining >= 2)
                        {
                            upperValues[0] = left._vector.GetInt(4) + right._vector.GetInt(4);
                            upperValues[1] = left._vector.GetInt(5) + right._vector.GetInt(5);
                            position = 2;
                            remaining -= 2;
                            arrayPosition = 6;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] =
                                left._vector.GetInt(arrayPosition) + right._vector.GetInt(arrayPosition);
                        }

                        return Create(count, lower128, upperValues);
                    case 4 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128Int(0), right._vector.GetVector128Int(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 2 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create(left._vector.GetInt(0) + right._vector.GetInt(0));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            int[] values = new int[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = left._vector.GetInt(i) + right._vector.GetInt(i);
                            }

                            return Create(values);
                        }

                        Vector128<int>[] blocks128 = null;
                        Vector256<int>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 8)
                        {
                            //remainingSubOperations >> 3 = remainingSubOperations / 8
                            blocks256 = new Vector256<int>[remainingSubOperations >> 3];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256Int(i),
                                    right._vector.GetVector256Int(i));
                            }

                            processedSubOperations = blocks256.Length << 3;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 4)
                        {
                            //remainingSubOperations >> 2 = remainingSubOperations / 4
                            blocks128 = new Vector128<int>[remainingSubOperations >> 2];

                            for (int i = 0, j = processedSubOperations >> 2; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128Int(j),
                                    right._vector.GetVector128Int(j));
                            }

                            processedSubOperations += blocks128.Length << 2;
                            remainingSubOperations -= blocks128.Length << 2;
                        }

                        if (remainingSubOperations > 1)
                        {
                            int[] values = new int[remainingSubOperations];

                            values[0] = left._vector.GetInt(processedSubOperations) +
                                        right._vector.GetInt(processedSubOperations++);
                            values[1] = left._vector.GetInt(processedSubOperations) +
                                        right._vector.GetInt(processedSubOperations++);

                            if (remainingSubOperations == 3)
                            {
                                values[2] = left._vector.GetInt(processedSubOperations) +
                                            right._vector.GetInt(processedSubOperations);
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                left._vector.GetInt(processedSubOperations) +
                                right._vector.GetInt(processedSubOperations));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(ulong))
            {
                switch (count)
                {
                    case 4 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256ULong(0), right._vector.GetVector256ULong(0)));
                    case 3 when IntrinsicSupport.IsSse2Supported:
                        Vector128<ulong> lower128 = Sse2.Add(left._vector.GetVector128ULong(0),
                            right._vector.GetVector128ULong(0));

                        ulong upperValue = left._vector.GetULong(3) + right._vector.GetULong(3);

                        return Create(count, lower128, upperValue);
                    case 2 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128ULong(0), right._vector.GetVector128ULong(0)));
                    case 1:
                        return Create(left._vector.GetULong(0) + right._vector.GetULong(0));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            ulong[] values = new ulong[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = left._vector.GetULong(i) + right._vector.GetULong(i);
                            }

                            return Create(values);
                        }

                        Vector128<ulong>[] blocks128 = null;
                        Vector256<ulong>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 4)
                        {
                            //remainingSubOperations >> 2 = remainingSubOperations / 4
                            blocks256 = new Vector256<ulong>[remainingSubOperations >> 2];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256ULong(i),
                                    right._vector.GetVector256ULong(i));
                            }

                            processedSubOperations = blocks256.Length << 2;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 2)
                        {
                            //remainingSubOperations >> 1 = remainingSubOperations / 2
                            blocks128 = new Vector128<ulong>[remainingSubOperations >> 1];

                            for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128ULong(j),
                                    right._vector.GetVector128ULong(j));
                            }

                            processedSubOperations += blocks128.Length << 1;
                            remainingSubOperations -= blocks128.Length << 1;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                left._vector.GetULong(processedSubOperations) +
                                right._vector.GetULong(processedSubOperations));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(long))
            {
                switch (count)
                {
                    case 4 when IntrinsicSupport.IsAvx2Supported:
                        return Create(count,
                            Avx2.Add(left._vector.GetVector256Long(0), right._vector.GetVector256Long(0)));
                    case 3 when IntrinsicSupport.IsSse2Supported:
                        Vector128<long> lower128 = Sse2.Add(left._vector.GetVector128Long(0),
                            right._vector.GetVector128Long(0));

                        long upperValue = left._vector.GetLong(3) + right._vector.GetLong(3);

                        return Create(count, lower128, upperValue);
                    case 2 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128Long(0), right._vector.GetVector128Long(0)));
                    case 1:
                        return Create(left._vector.GetLong(0) + right._vector.GetLong(0));
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            long[] values = new long[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = left._vector.GetLong(i) + right._vector.GetLong(i);
                            }

                            return Create(values);
                        }

                        Vector128<long>[] blocks128 = null;
                        Vector256<long>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvx2Supported && remainingSubOperations >= 4)
                        {
                            //remainingSubOperations >> 2 = remainingSubOperations / 4
                            blocks256 = new Vector256<long>[remainingSubOperations >> 2];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx2.Add(left._vector.GetVector256Long(i),
                                    right._vector.GetVector256Long(i));
                            }

                            processedSubOperations = blocks256.Length << 2;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 2)
                        {
                            //remainingSubOperations >> 1 = remainingSubOperations / 2
                            blocks128 = new Vector128<long>[remainingSubOperations >> 1];

                            for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128Long(j),
                                    right._vector.GetVector128Long(j));
                            }

                            processedSubOperations += blocks128.Length << 1;
                            remainingSubOperations -= blocks128.Length << 1;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                left._vector.GetLong(processedSubOperations) +
                                right._vector.GetLong(processedSubOperations));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(float))
            {
                switch (count)
                {
                    case 8 when IntrinsicSupport.IsAvxSupported:
                        return Create(count,
                            Avx.Add(left._vector.GetVector256Float(0), right._vector.GetVector256Float(0)));
                    case < 8 and > 4 when IntrinsicSupport.IsSseSupported:
                        Vector128<float> lower128 = Sse.Add(left._vector.GetVector128Float(0),
                            right._vector.GetVector128Float(0));

                        float[] upperValues = new float[count - 4];

                        int remaining = upperValues.Length;
                        int position = 0;
                        int arrayPosition = 4;

                        //TODO Figure out if this is any faster than the for loop that was here
                        if (remaining >= 2)
                        {
                            upperValues[0] = left._vector.GetFloat(8) + right._vector.GetFloat(8);
                            upperValues[1] = left._vector.GetFloat(9) + right._vector.GetFloat(9);
                            position = 2;
                            remaining -= 2;
                            arrayPosition = 10;
                        }

                        if (remaining == 1)
                        {
                            upperValues[position] =
                                left._vector.GetFloat(arrayPosition) + right._vector.GetFloat(arrayPosition);
                        }

                        return Create(count, lower128, upperValues);
                    case 4 when IntrinsicSupport.IsSseSupported:
                        return Create(count,
                            Sse.Add(left._vector.GetVector128Float(0),
                                right._vector.GetVector128Float(0)));
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 2 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        return Create(left._vector.GetFloat(0) + right._vector.GetFloat(0));
                    default:
                        //Assumption is made that no Sse support means no Avx support
                        if (!IntrinsicSupport.IsSseSupported)
                        {
                            float[] values = new float[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = left._vector.GetFloat(i) + right._vector.GetFloat(i);
                            }

                            return Create(values);
                        }

                        Vector128<float>[] blocks128 = null;
                        Vector256<float>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvxSupported && remainingSubOperations >= 8)
                        {
                            //remainingSubOperations >> 3 = remainingSubOperations / 8
                            blocks256 = new Vector256<float>[remainingSubOperations >> 3];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx.Add(left._vector.GetVector256Float(i),
                                    right._vector.GetVector256Float(i));
                            }

                            processedSubOperations = blocks256.Length << 3;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 4)
                        {
                            //remainingSubOperations >> 2 = remainingSubOperations / 4
                            blocks128 = new Vector128<float>[remainingSubOperations >> 2];

                            for (int i = 0, j = processedSubOperations >> 2; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse.Add(left._vector.GetVector128Float(j),
                                    right._vector.GetVector128Float(j));
                            }

                            processedSubOperations += blocks128.Length << 2;
                            remainingSubOperations -= blocks128.Length << 2;
                        }

                        if (remainingSubOperations > 1)
                        {
                            float[] values = new float[remainingSubOperations];

                            values[0] = left._vector.GetFloat(processedSubOperations) +
                                        right._vector.GetFloat(processedSubOperations++);
                            values[1] = left._vector.GetFloat(processedSubOperations) +
                                        right._vector.GetFloat(processedSubOperations++);

                            if (remainingSubOperations == 3)
                            {
                                values[2] = left._vector.GetFloat(processedSubOperations) +
                                            right._vector.GetFloat(processedSubOperations);
                            }

                            return Create(count, blocks256, blocks128, values);
                        }
                        else if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                left._vector.GetFloat(processedSubOperations) +
                                right._vector.GetFloat(processedSubOperations));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else if (typeof(T) == typeof(double))
            {
                switch (count)
                {
                    case 4 when IntrinsicSupport.IsAvxSupported:
                        return Create(count,
                            Avx.Add(left._vector.GetVector256Double(0), right._vector.GetVector256Double(0)));
                    case 3 when IntrinsicSupport.IsSse2Supported:
                        Vector128<double> lower128 = Sse2.Add(left._vector.GetVector128Double(0),
                            right._vector.GetVector128Double(0));

                        double upperValue = left._vector.GetDouble(3) + right._vector.GetDouble(3);

                        return Create(count, lower128, upperValue);
                    case 2 when IntrinsicSupport.IsSse2Supported:
                        return Create(count,
                            Sse2.Add(left._vector.GetVector128Double(0),
                                right._vector.GetVector128Double(0)));
                    case 1:
                        return Create(left._vector.GetDouble(0) + right._vector.GetDouble(0));
                    default:
                        //Assumption is made that no Sse2 support means no Avx support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            double[] values = new double[count];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = left._vector.GetDouble(i) + right._vector.GetDouble(i);
                            }

                            return Create(values);
                        }

                        Vector128<double>[] blocks128 = null;
                        Vector256<double>[] blocks256 = null;

                        int remainingSubOperations = count;
                        int processedSubOperations = 0;

                        if (IntrinsicSupport.IsAvxSupported && remainingSubOperations >= 4)
                        {
                            //remainingSubOperations >> 2 = remainingSubOperations / 4
                            blocks256 = new Vector256<double>[remainingSubOperations >> 2];

                            for (int i = 0; i < blocks256.Length; i++)
                            {
                                blocks256[i] = Avx.Add(left._vector.GetVector256Double(i),
                                    right._vector.GetVector256Double(i));
                            }

                            processedSubOperations = blocks256.Length << 2;
                            remainingSubOperations -= processedSubOperations;
                        }

                        if (remainingSubOperations >= 2)
                        {
                            //remainingSubOperations >> 1 = remainingSubOperations / 2
                            blocks128 = new Vector128<double>[remainingSubOperations >> 1];

                            for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                            {
                                blocks128[i] = Sse2.Add(left._vector.GetVector128Double(j),
                                    right._vector.GetVector128Double(j));
                            }

                            processedSubOperations += blocks128.Length << 1;
                            remainingSubOperations -= blocks128.Length << 1;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count, blocks256, blocks128,
                                left._vector.GetDouble(processedSubOperations) +
                                right._vector.GetDouble(processedSubOperations));
                        }

                        return Create(count, blocks256, blocks128, value: null);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }


        //This is the result of 128/(Unsafe.SizeOf<T>*8)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    }
}