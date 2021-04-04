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
    //TODO Retest copy methods
    public readonly struct Vector<T> : IEquatable<Vector<T>>, IFormattable
        where T : struct
    {
        private readonly Register<T> _vector;

        public readonly int Length => _vector.Values.Length;

        //TODO Fix, need different constant for each type
        //public static VectorDouble<T> Zero { get; } = new(0, true);

        //TODO Fix
        //public static VectorDouble<T> One { get; } = new(1, true);

        //TODO This needs a new way to get all 1's
        //internal static VectorDouble<T> AllBitsSet { get; } = new(BitConverter.Int64BitsToDouble(-1), true);

        private Vector(T value, bool allSizes) => _vector = new Register<T>(value, true);

        private Vector(Register<T> register) => _vector = register;

        //TODO Rewrite for whatever is needed now as 128 + Unsafe.SizeOf<T>() =/= 192 at all times, only for Double, Int64 and UInt64
        //Use Vector64<T>? If so we need another set of cases to handle mmx
        //Used for Sse2 fallback of hardcoded 192 bit double vectors
        //private Vector(Vector128<T> block128, T value) => _vector = new Register<T>(block128, value);

        //Used for Sse2 fallback of hardcoded 256 bit vectors
        private Vector(Vector128<T> firstBlock128, Vector128<T> secondBlock128) =>
            _vector = new Register<T>(firstBlock128, secondBlock128);

        public Vector(T value) => _vector = new Register<T>(value);

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
            _vector = new Register<T>(values[index..]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<byte> values) =>
            _vector = new Register<T>(MemoryMarshal.Cast<byte, T>(values).ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<T> values) => _vector = new Register<T>(values.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(Span<T> values) : this((ReadOnlySpan<T>)values) { }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(Vector128<U> values, int count) where U : struct =>
            new(Register<T>.Create(values, count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(Vector256<U> values, int count) where U : struct =>
            new(Register<T>.Create(values, count));

        private static Vector<T> Create<U>(U value) where U : struct => new(Unsafe.As<U, T>(ref value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<T> Create<U>(int count, U? value = null, Vector128<U>[] blocks128 = null,
            Vector256<U>[] blocks256 = null) where U : struct =>
            new(Register<T>.Create(count, value, blocks128, blocks256));

        private static Vector<T> Create<U>(U[] values) where U : struct => new(Unsafe.As<U[], T[]>(ref values), 0);

        public readonly void CopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)(_vector.Values.Length << BitShiftSizeOf()))
            {
                throw new ArgumentException();
            }

            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]),
                (uint)(_vector.Values.Length << BitShiftSizeOf()));
        }

        public readonly void CopyTo(Span<T> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Values.Length)
            {
                throw new ArgumentException();
            }

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]),
                (uint)(_vector.Values.Length << BitShiftSizeOf()));
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

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]),
                (uint)(_vector.Values.Length << BitShiftSizeOf()));
        }

        public readonly unsafe T this[int index] => _vector[index];

        public readonly Vector<T> Slice(int start, int length) => new(_vector.Values.AsSpan().Slice(start, length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
            (obj is Vector<T> other) && Equals(other);

        public readonly bool Equals(Vector<T> other) => this == other;

        public override readonly int GetHashCode()
        {
            if (_vector.Values.Length == 1)
            {
                return _vector[0].GetHashCode();
            }

            HashCode hashCode = default;

            for (int i = 0; i < _vector.Values.Length; i++)
            {
                hashCode.Add(_vector[i]);
            }

            return hashCode.ToHashCode();
        }

        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

        public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            StringBuilder sb = new();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            sb.Append('<');
            sb.Append(((IFormattable)_vector[0]).ToString(format, formatProvider));

            //TODO Decide if the hardcoded cases should be inside each other with if statements like case 3-4 or kept separate as they are now
            switch (_vector.Values.Length)
            {
                case 1:
                    break;
                case 2:
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)_vector[1]).ToString(format, formatProvider));
                    break;
                case 3:
                case 4:
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)_vector[1]).ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(((IFormattable)_vector[2]).ToString(format, formatProvider));
                    if (_vector.Values.Length == 4)
                    {
                        sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                        sb.Append(' ');
                        sb.Append(((IFormattable)_vector[3]).ToString(format, formatProvider));
                    }
                    break;

                default:
                    for (int i = 1; i < _vector.Values.Length; i++)
                    {
                        sb.Append(separator);
                        sb.Append(' ');
                        sb.Append(((IFormattable)_vector[i]).ToString(format, formatProvider));
                    }
                    break;
            }

            sb.Append('>');
            return sb.ToString();
        }

        public readonly bool TryCopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Values.Length << BitShiftSizeOf())
            {
                return false;
            }

            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]),
                (uint)(_vector.Values.Length << BitShiftSizeOf()));
            return true;
        }

        public readonly bool TryCopyTo(Span<T> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Values.Length)
            {
                return false;
            }

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]),
                (uint)(_vector.Values.Length << BitShiftSizeOf()));
            return true;
        }

        //Operators
        public static Vector<T> operator +(Vector<T> left, Vector<T> right)
        {
            int count;

            if (left._vector.MultiSize)
            {
                count = right._vector.Values.Length;
            }
            else if (right._vector.MultiSize)
            {
                count = left._vector.Values.Length;
            }
            else if (left._vector.Values.Length != right._vector.Values.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                count = left._vector.Values.Length;
            }

            return Add(left, right, count);
        }

        public static Vector<T> operator -(Vector<T> left, Vector<T> right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Values.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Values.Length;
            }
            else if (left._vector.Values.Length != right._vector.Values.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Values.Length;
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
                    left._vector[2] - right._vector[2]);*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    Sse2.Subtract(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector[0] - right._vector[0]);
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] - right._vector[0],
                        left._vector[1] - right._vector[1]
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] - right._vector[0],
                        left._vector[1] - right._vector[1],
                        left._vector[2] - right._vector[2]
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] - right._vector[0],
                        left._vector[1] - right._vector[1],
                        left._vector[2] - right._vector[2],
                        left._vector[3] - right._vector[3]
                    }, 0);*/
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector[i] - right._vector[i];
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

            if (left._vector.MultiSize)
            {
                size = right._vector.Values.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Values.Length;
            }
            else if (left._vector.Values.Length != right._vector.Values.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Values.Length;
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
                    left._vector[2] * right._vector[2]);*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    Sse2.Multiply(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector[0] * right._vector[0]);
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] * right._vector[0],
                        left._vector[1] * right._vector[1]
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] * right._vector[0],
                        left._vector[1] * right._vector[1],
                        left._vector[2] * right._vector[2]
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] * right._vector[0],
                        left._vector[1] * right._vector[1],
                        left._vector[2] * right._vector[2],
                        left._vector[3] * right._vector[3]
                    }, 0);*/
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector[i] * right._vector[i];
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
            switch (value._vector.Values.Length)
            {
                case 1:
                //return new Vector<T>(value._vector[0] * factor);
                case 2:
                /*return new Vector<T>(
                    new[] { value._vector[0] * factor, value._vector[1] * factor }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        value._vector[0] * factor, value._vector[1] * factor,
                        value._vector[2] * factor
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        value._vector[0] * factor, value._vector[1] * factor,
                        value._vector[2] * factor, value._vector[3] * factor
                    }, 0);*/
                default:
                    T[] newValues = new T[value._vector.Values.Length];
                    for (int i = 0; i < value._vector.Values.Length; i++)
                    {
                        //newValues[i] = value._vector[i] * factor;
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

            if (left._vector.MultiSize)
            {
                size = right._vector.Values.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Values.Length;
            }
            else if (left._vector.Values.Length != right._vector.Values.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Values.Length;
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
                    left._vector[2] / right._vector[2]);*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    Sse2.Divide(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector[0] / right._vector[0]);
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] / right._vector[0],
                        left._vector[1] / right._vector[1]
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] / right._vector[0],
                        left._vector[1] / right._vector[1],
                        left._vector[2] / right._vector[2]
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] / right._vector[0],
                        left._vector[1] / right._vector[1],
                        left._vector[2] / right._vector[2],
                        left._vector[3] / right._vector[3]
                    }, 0);*/
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector[i] / right._vector[i];
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
        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> value) => Zero - value;*/

        //TODO Add bitwise operations when integer types are supported

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector<T> left, Vector<T> right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Values.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Values.Length;
            }
            else if (left._vector.Values.Length != right._vector.Values.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Values.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                /*{
                    Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                    return Sse2.MoveMask(result) == 0b11;
                }*/
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                /*{
                    Vector256<double> result = Avx.Compare(left._vector.ToVector256(0), right._vector.ToVector256(0),
                        FloatComparisonMode.OrderedEqualNonSignaling);
                    return Avx.MoveMask(result) == 0b1111;
                }*/

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                /*{
                    Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                    return Sse2.MoveMask(result) == 0b11 && left._vector[2].Equals(right._vector[2]);
                }*/
                case 4 when Sse2.IsSupported:
                /*{
                    Vector128<double> result1 = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                    Vector128<double> result2 = Sse2.CompareEqual(left._vector.ToVector128(1), right._vector.ToVector128(1));
                    return Sse2.MoveMask(result1) == 0b11 && Sse2.MoveMask(result2) == 0b11;
                }*/

                //Software fallback
                case 1 when left._vector[0].Equals(right._vector[0]):
                case 2 when left._vector[0].Equals(right._vector[0]) &&
                            left._vector[1].Equals(right._vector[1]):
                case 3 when left._vector[0].Equals(right._vector[0]) &&
                            left._vector[1].Equals(right._vector[1]) &&
                            left._vector[2].Equals(right._vector[2]):
                case 4 when left._vector[0].Equals(right._vector[0]) &&
                            left._vector[1].Equals(right._vector[1]) &&
                            left._vector[2].Equals(right._vector[2]) &&
                            left._vector[3].Equals(right._vector[3]):
                    return true;
                case > 4:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        for (int i = 0; i < size; i++)
                        {
                            if (!left._vector[i].Equals(right._vector[i]))
                            {
                                return false;
                            }
                        }
                    }

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

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
                        processedSubOperations += count256 << 2;
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
                        processedSubOperations += count128 << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        return left._vector[processedSubOperations].Equals(right._vector[processedSubOperations]);
                    }

                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector<T> left, Vector<T> right) => !(left == right);


        //TODO Ensure constants are set correctly, is it worth making the constant for each type a variable in each if case?
        //There are functions in Register<T> to calculate them but that feels unnecessary since those are for generic cases
        //TODO Support 16 though 120 bit operations(i.e. 2-15 (s)bytes, 2-7 (u)shorts and 2-3 (u)ints/floats)
        //TODO Support 136 though 248 bit operations(i.e. 17-31 (s)bytes, 9-15 (u)shorts, 5-7 (u)ints/floats and 3 (u)longs/doubles)
        //TODO Support arbitrary length operations where after processing all 256 and 128 bit groups, the number of
        //remaining numbers is > 1, currently in the first two cases all numbers are ignored and in the last one only
        //the first remaining number is considered
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
                        return Create(
                            Avx2.Add(left._vector.ToVector256<byte>(0), right._vector.ToVector256<byte>(0)), count);
                    case 16 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<byte>(0), right._vector.ToVector128<byte>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 8 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((byte)(object)left._vector[0] + (byte)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            byte[] values64 = new byte[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (byte)((byte)(object)left._vector[i] + (byte)(object)right._vector[i]);
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<byte>(i),
                                    right._vector.ToVector256<byte>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<byte>(j),
                                    right._vector.ToVector128<byte>(j));
                            }

                            processedSubOperations += blocks128.Length << 4;
                            remainingSubOperations -= blocks128.Length << 4;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (byte)((byte)(object)left._vector[processedSubOperations] +
                                        (byte)(object)right._vector[processedSubOperations]), blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(sbyte))
            {
                switch (count)
                {
                    case 32 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<sbyte>(0), right._vector.ToVector256<sbyte>(0)), count);
                    case 16 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<sbyte>(0), right._vector.ToVector128<sbyte>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 8 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((sbyte)(object)left._vector[0] + (sbyte)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            sbyte[] values64 = new sbyte[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (sbyte)((sbyte)(object)left._vector[i] + (sbyte)(object)right._vector[i]);
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<sbyte>(i),
                                    right._vector.ToVector256<sbyte>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<sbyte>(j),
                                    right._vector.ToVector128<sbyte>(j));
                            }

                            processedSubOperations += blocks128.Length << 4;
                            remainingSubOperations -= blocks128.Length << 4;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (sbyte)((sbyte)(object)left._vector[processedSubOperations] +
                                        (sbyte)(object)right._vector[processedSubOperations]), blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(ushort))
            {
                switch (count)
                {
                    case 16 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<ushort>(0), right._vector.ToVector256<ushort>(0)), count);
                    case 8 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<ushort>(0), right._vector.ToVector128<ushort>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 4 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((ushort)(object)left._vector[0] + (ushort)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            ushort[] values64 = new ushort[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (ushort)((ushort)(object)left._vector[i] + (ushort)(object)right._vector[i]);
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<ushort>(i),
                                    right._vector.ToVector256<ushort>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<ushort>(j),
                                    right._vector.ToVector128<ushort>(j));
                            }

                            processedSubOperations += blocks128.Length << 3;
                            remainingSubOperations -= blocks128.Length << 3;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (ushort)((ushort)(object)left._vector[processedSubOperations] +
                                         (ushort)(object)right._vector[processedSubOperations]), blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(short))
            {
                switch (count)
                {
                    case 16 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<short>(0), right._vector.ToVector256<short>(0)), count);
                    case 8 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<short>(0), right._vector.ToVector128<short>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 4 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((short)(object)left._vector[0] + (short)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            short[] values64 = new short[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (short)((short)(object)left._vector[i] + (short)(object)right._vector[i]);
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<short>(i),
                                    right._vector.ToVector256<short>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<short>(j),
                                    right._vector.ToVector128<short>(j));
                            }

                            processedSubOperations += blocks128.Length << 3;
                            remainingSubOperations -= blocks128.Length << 3;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (short)((short)(object)left._vector[processedSubOperations] +
                                         (short)(object)right._vector[processedSubOperations]), blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(uint))
            {
                switch (count)
                {
                    case 8 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<uint>(0), right._vector.ToVector256<uint>(0)), count);
                    case 4 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<uint>(0), right._vector.ToVector128<uint>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 2 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((uint)(object)left._vector[0] + (uint)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            uint[] values64 = new uint[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (uint)(object)left._vector[i] + (uint)(object)right._vector[i];
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<uint>(i),
                                    right._vector.ToVector256<uint>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<uint>(j),
                                    right._vector.ToVector128<uint>(j));
                            }

                            processedSubOperations += blocks128.Length << 2;
                            remainingSubOperations -= blocks128.Length << 2;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (uint)(object)left._vector[processedSubOperations] +
                                (uint)(object)right._vector[processedSubOperations], blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(int))
            {
                switch (count)
                {
                    case 8 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<int>(0), right._vector.ToVector256<int>(0)), count);
                    case 4 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<int>(0), right._vector.ToVector128<int>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 2 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((int)(object)left._vector[0] + (int)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            int[] values64 = new int[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (int)(object)left._vector[i] + (int)(object)right._vector[i];
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<int>(i),
                                    right._vector.ToVector256<int>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<int>(j),
                                    right._vector.ToVector128<int>(j));
                            }

                            processedSubOperations += blocks128.Length << 2;
                            remainingSubOperations -= blocks128.Length << 2;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (int)(object)left._vector[processedSubOperations] +
                                (int)(object)right._vector[processedSubOperations], blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(ulong))
            {
                switch (count)
                {
                    case 4 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<ulong>(0), right._vector.ToVector256<ulong>(0)), count);
                    case 2 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<ulong>(0), right._vector.ToVector128<ulong>(0)), count);
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((ulong)(object)left._vector[0] + (ulong)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            ulong[] values64 = new ulong[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (ulong)(object)left._vector[i] + (ulong)(object)right._vector[i];
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<ulong>(i),
                                    right._vector.ToVector256<ulong>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<ulong>(j),
                                    right._vector.ToVector128<ulong>(j));
                            }

                            processedSubOperations += blocks128.Length << 1;
                            remainingSubOperations -= blocks128.Length << 1;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (ulong)(object)left._vector[processedSubOperations] +
                                (ulong)(object)right._vector[processedSubOperations], blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(long))
            {
                switch (count)
                {
                    case 4 when IntrinsicSupport.IsAvx2Supported:
                        return Create(
                            Avx2.Add(left._vector.ToVector256<long>(0), right._vector.ToVector256<long>(0)), count);
                    case 2 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<long>(0), right._vector.ToVector128<long>(0)), count);
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((long)(object)left._vector[0] + (long)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx2 support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            long[] values64 = new long[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (long)(object)left._vector[i] + (long)(object)right._vector[i];
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx2.Add(left._vector.ToVector256<long>(i),
                                    right._vector.ToVector256<long>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<long>(j),
                                    right._vector.ToVector128<long>(j));
                            }

                            processedSubOperations += blocks128.Length << 1;
                            remainingSubOperations -= blocks128.Length << 1;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (long)(object)left._vector[processedSubOperations] +
                                (long)(object)right._vector[processedSubOperations], blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(float))
            {
                switch (count)
                {
                    case 8 when IntrinsicSupport.IsAvxSupported:
                        return Create(
                            Avx.Add(left._vector.ToVector256<float>(0), right._vector.ToVector256<float>(0)), count);
                    case 4 when IntrinsicSupport.IsSseSupported:
                        return Create(
                            Sse.Add(left._vector.ToVector128<float>(0), right._vector.ToVector128<float>(0)), count);
                    //TODO Support Vector64<T> on Arm
                    //TODO Is it worth extending Vector64<T>s to Vector128<T>s to use Sse2 on x86 since MMX is not supported?
                    /*case 2 when AdvSimd.IsSupported:
                        break;*/
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((float)(object)left._vector[0] + (float)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse support means no Avx support
                        if (!IntrinsicSupport.IsSseSupported)
                        {
                            float[] values64 = new float[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (float)(object)left._vector[i] + (float)(object)right._vector[i];
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx.Add(left._vector.ToVector256<float>(i),
                                    right._vector.ToVector256<float>(i));
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
                                blocks128[i] = Sse.Add(left._vector.ToVector128<float>(j),
                                    right._vector.ToVector128<float>(j));
                            }

                            processedSubOperations += blocks128.Length << 2;
                            remainingSubOperations -= blocks128.Length << 2;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (float)(object)left._vector[processedSubOperations] +
                                (float)(object)right._vector[processedSubOperations], blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else if (typeof(T) == typeof(double))
            {
                switch (count)
                {
                    case 4 when IntrinsicSupport.IsAvxSupported:
                        return Create(
                            Avx.Add(left._vector.ToVector256<double>(0), right._vector.ToVector256<double>(0)), count);
                    case 2 when IntrinsicSupport.IsSse2Supported:
                        return Create(
                            Sse2.Add(left._vector.ToVector128<double>(0), right._vector.ToVector128<double>(0)), count);
                    case 1:
                        //Note: This amount of casts is stupid, is there a better way? Create type specific add functions?
                        //I tried that but those must have one or more argument be a Vector<T> not a concrete type
                        //TODO At least try to deal with this by redesigning the Register<T> class again to expose arrays
                        //of each type, this will also remove overhead in getting VectorX<T>'s provided there is a ToVectorX
                        //function for each type.
                        return Create((float)(object)left._vector[0] + (float)(object)right._vector[0]);
                    //Temporary fallback, there are missing x86 cases that could be added for extra performance
                    default:
                        //Assumption is made that no Sse2 support means no Avx support
                        if (!IntrinsicSupport.IsSse2Supported)
                        {
                            double[] values64 = new double[count];

                            for (int i = 0; i < values64.Length; i++)
                            {
                                values64[i] = (double)(object)left._vector[i] + (double)(object)right._vector[i];
                            }

                            return Create(values64);
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
                                blocks256[i] = Avx.Add(left._vector.ToVector256<double>(i),
                                    right._vector.ToVector256<double>(i));
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
                                blocks128[i] = Sse2.Add(left._vector.ToVector128<double>(j),
                                    right._vector.ToVector128<double>(j));
                            }

                            processedSubOperations += blocks128.Length << 1;
                            remainingSubOperations -= blocks128.Length << 1;
                        }

                        if (remainingSubOperations == 1)
                        {
                            return Create(count,
                                (double)(object)left._vector[processedSubOperations] +
                                (double)(object)right._vector[processedSubOperations], blocks128, blocks256);
                        }

                        return Create(count, null, blocks128, blocks256);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }


        //This is the result of log_2(Unsafe.SizeOf<T>) and is designed to be used
        //as "a << SizeOf<T>()" wherever "a * Unsafe.SizeOf<T>()" might be used
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BitShiftSizeOf()
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