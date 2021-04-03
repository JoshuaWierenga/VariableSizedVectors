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

        //TODO This needed a new way to get all 1's
        //internal static VectorDouble<T> AllBitsSet { get; } = new(BitConverter.Int64BitsToDouble(-1), true);

        private Vector(T value, bool allSizes) => _vector = new Register<T>(value, true);

        private Vector(Vector128<T> values) => _vector = new Register<T>(values);

        private Vector(Vector256<T> values, int count) => _vector = new Register<T>(values, count);

        //TODO Rewrite for whatever is needed now as 128 + Unsafe.SizeOf<T>() =/= 192 at all times, only for Double, Int64 and UInt64
        //Use Vector64<T>? If so we need another set of cases to handle mmx
        //Used for Sse2 fallback of hardcoded 192 bit double vectors
        private Vector(Vector128<T> block128, T value) => _vector = new Register<T>(block128, value);

        //Used for Sse2 fallback of hardcoded 256 bit vectors
        private Vector(Vector128<T> firstBlock128, Vector128<T> secondBlock128) =>
            _vector = new Register<T>(firstBlock128, secondBlock128);

        private Vector(int count, T? value = null, Vector128<T>[] blocks128 = null,
            Vector256<T>[] blocks256 = null) => _vector = new Register<T>(count, value, blocks128, blocks256);

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

        public readonly void CopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)(_vector.Values.Length * sizeof(double)))
            {
                throw new ArgumentException();
            }

            //TODO Optimise multiplication to left bitshift
            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]), (uint)(_vector.Values.Length * Unsafe.SizeOf<T>()));
        }

        public readonly void CopyTo(Span<T> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Values.Length)
            {
                throw new ArgumentException();
            }

            //TODO Optimise multiplication to left bitshift
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]), (uint)(_vector.Values.Length * Unsafe.SizeOf<T>()));
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

            //TODO Optimise multiplication to left bitshift
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]), (uint)(_vector.Values.Length * Unsafe.SizeOf<T>()));
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

            //TODO Decide if the hardcoded cases should just be inside each other with for loops like case 3-4 or as they are now
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
            if ((uint)destination.Length < (uint)_vector.Values.Length * sizeof(double))
            {
                return false;
            }

            //TODO Optimise multiplication to left bitshift
            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]), (uint)(_vector.Values.Length * Unsafe.SizeOf<T>()));
            return true;
        }

        public readonly bool TryCopyTo(Span<T> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Values.Length)
            {
                return false;
            }

            //TODO Optimise multiplication to left bitshift
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)),
                ref Unsafe.As<T, byte>(ref _vector.Values[0]), (uint)(_vector.Values.Length * Unsafe.SizeOf<T>()));
            return true;
        }

        //Operators
        //TODO Add typeof check function to hardcode operations for different types
        public static Vector<T> operator +(Vector<T> left, Vector<T> right)
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
                //return new Vector<T>(Sse2.Add(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                //return new Vector<T>(Avx.Add(left._vector.ToVector256(0), right._vector.ToVector256(0)), size);

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Add(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                    left._vector[2] + right._vector[2]);*/
                case 4 when Sse2.IsSupported:
                /*return new Vector<T>(Sse2.Add(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        Sse2.Add(left._vector.ToVector128(1), right._vector.ToVector128(1)));*/

                //Software fallback
                case 1:
                //return new Vector<T>(left._vector[0] + right._vector[0]);
                case 2:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] + right._vector[0],
                        left._vector[1] + right._vector[1]
                    }, 0);*/
                case 3:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] + right._vector[0],
                        left._vector[1] + right._vector[1],
                        left._vector[2] + right._vector[2]
                    }, 0);*/
                case 4:
                /*return new Vector<T>(
                    new[]
                    {
                        left._vector[0] + right._vector[0],
                        left._vector[1] + right._vector[1],
                        left._vector[2] + right._vector[2],
                        left._vector[3] + right._vector[3]
                    }, 0);*/
                default:

                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        T[] values64 = new T[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            //values64[i] = left._vector[i] + right._vector[i];
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
                            //blocks256[i] = Avx.Add(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<T>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            //blocks128[i] = Sse2.Add(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        /*return new Vector<T>(size,
                            left._vector[processedSubOperations] + right._vector[processedSubOperations], blocks128,
                            blocks256);*/
                    }

                    return new Vector<T>(size, value: null, blocks128, blocks256);
            }
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

                    return new Vector<T>(size, value: null, blocks128, blocks256);
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

                    return new Vector<T>(size, value: null, blocks128, blocks256);
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

                    return new Vector<T>(size, value: null, blocks128, blocks256);
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
    }
}