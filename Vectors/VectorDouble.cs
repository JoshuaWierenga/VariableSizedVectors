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
    //TODO Make more use of the new mixed argument constructor for 192 and 256 bit Sse2 fallbacks
    //TODO Add Range indexer, Index?
    public readonly struct VectorDouble : IEquatable<VectorDouble>, IFormattable
    {
        private readonly RegisterDouble _vector;

        public readonly int Length => _vector.Doubles.Length;

        public static VectorDouble Zero { get; } = new(0, true);

        public static VectorDouble One { get; } = new(1, true);

        internal static VectorDouble AllBitsSet { get; } = new(BitConverter.Int64BitsToDouble(-1), true);

        private VectorDouble(double value, bool allSizes) => _vector = new RegisterDouble(value, true);

        private VectorDouble(Vector128<double> values) => _vector = new RegisterDouble(values);

        private VectorDouble(Vector256<double> values, int count) => _vector = new RegisterDouble(values, count);

        //Used for Sse2 fallback of hardcoded 192 bit double vectors
        private VectorDouble(Vector128<double> block128, double value) => _vector = new RegisterDouble(block128, value);

        //Used for Sse2 fallback of hardcoded 256 bit double vectors
        private VectorDouble(Vector128<double> firstBlock128, Vector128<double> secondBlock128) =>
            _vector = new RegisterDouble(firstBlock128, secondBlock128);

        private VectorDouble(int count, double? value = null, Vector128<double>[] blocks128 = null,
            Vector256<double>[] blocks256 = null) => _vector = new RegisterDouble(count, value, blocks128, blocks256);

        public VectorDouble(double value) => _vector = new RegisterDouble(value);

        public VectorDouble(double[] values) : this(values, 0) { }

        public VectorDouble(double[] values, int index)
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
            _vector = new RegisterDouble(values[index..]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(ReadOnlySpan<byte> values) =>
            _vector = new RegisterDouble(MemoryMarshal.Cast<byte, double>(values).ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(ReadOnlySpan<double> values) => _vector = new RegisterDouble(values.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(Span<double> values) : this((ReadOnlySpan<double>)values) { }

        public readonly void CopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)(_vector.Doubles.Length * sizeof(double)))
            {
                throw new ArgumentException();
            }

            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination),
                ref Unsafe.As<double, byte>(ref _vector.Doubles[0]), (uint)(_vector.Doubles.Length * sizeof(double)));
        }

        public readonly void CopyTo(Span<double> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Doubles.Length)
            {
                throw new ArgumentException();
            }

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)),
                ref Unsafe.As<double, byte>(ref _vector.Doubles[0]), (uint)(_vector.Doubles.Length * sizeof(double)));
        }

        public readonly void CopyTo(double[] destination) => CopyTo(destination, 0);

        public readonly unsafe void CopyTo(double[] destination, int startIndex)
        {
            if (destination is null)
            {
                throw new NullReferenceException();
            }

            if ((uint)startIndex >= (uint)destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref destination[startIndex]),
                ref Unsafe.As<double, byte>(ref _vector.Doubles[0]), (uint)(_vector.Doubles.Length * sizeof(double)));
        }

        public readonly unsafe double this[int index] => _vector[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
            (obj is VectorDouble other) && Equals(other);

        public readonly bool Equals(VectorDouble other) => this == other;

        public override readonly int GetHashCode()
        {
            if (_vector.Doubles.Length == 1)
            {
                return _vector[0].GetHashCode();
            }

            HashCode hashCode = default;

            for (int i = 0; i < _vector.Doubles.Length; i++)
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
            sb.Append(_vector[0].ToString(format, formatProvider));

            switch (_vector.Doubles.Length)
            {
                case 1:
                    break;
                case 2:
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector[1].ToString(format, formatProvider));
                    break;
                case 3:
                case 4:
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector[1].ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector[2].ToString(format, formatProvider));
                    if (_vector.Doubles.Length == 4)
                    {
                        sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                        sb.Append(' ');
                        sb.Append(_vector[3].ToString(format, formatProvider));
                    }
                    break;

                default:
                    for (int i = 1; i < _vector.Doubles.Length; i++)
                    {
                        sb.Append(separator);
                        sb.Append(' ');
                        sb.Append(_vector[i].ToString(format, formatProvider));
                    }

                    break;
            }



            sb.Append('>');
            return sb.ToString();
        }

        public readonly bool TryCopyTo(Span<byte> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Doubles.Length * sizeof(double))
            {
                return false;
            }

            Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(destination),
                ref Unsafe.As<double, byte>(ref _vector.Doubles[0]), (uint)(_vector.Doubles.Length * sizeof(double)));
            return true;
        }

        public readonly bool TryCopyTo(Span<double> destination)
        {
            if ((uint)destination.Length < (uint)_vector.Doubles.Length)
            {
                return false;
            }

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)),
                ref Unsafe.As<double, byte>(ref _vector.Doubles[0]), (uint)(_vector.Doubles.Length * sizeof(double)));
            return true;
        }

        //Operators
        public static VectorDouble operator +(VectorDouble left, VectorDouble right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Doubles.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Doubles.Length;
            }
            else if (left._vector.Doubles.Length != right._vector.Doubles.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Doubles.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Add(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Add(left._vector.ToVector256(0), right._vector.ToVector256(0)), size);

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Add(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        left._vector[2] + right._vector[2]);
                case 4 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Add(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                            Sse2.Add(left._vector.ToVector128(1), right._vector.ToVector128(1)));

                //Software fallback
                case 1:
                    return new VectorDouble(left._vector[0] + right._vector[0]);
                case 2:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] + right._vector[0],
                            left._vector[1] + right._vector[1]
                        }, 0);
                case 3:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] + right._vector[0],
                            left._vector[1] + right._vector[1],
                            left._vector[2] + right._vector[2]
                        }, 0);
                case 4:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] + right._vector[0],
                            left._vector[1] + right._vector[1],
                            left._vector[2] + right._vector[2],
                            left._vector[3] + right._vector[3]
                        }, 0);
                default:

                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        double[] values64 = new double[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            values64[i] = left._vector[i] + right._vector[i];
                        }

                        return new VectorDouble(values64);
                    }

                    Vector128<double>[] blocks128 = null;
                    Vector256<double>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<double>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            blocks256[i] = Avx.Add(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<double>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            blocks128[i] = Sse2.Add(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        return new VectorDouble(size,
                            left._vector[processedSubOperations] + right._vector[processedSubOperations], blocks128,
                            blocks256);
                    }

                    return new VectorDouble(size, value: null, blocks128, blocks256);
            }
        }

        public static VectorDouble operator -(VectorDouble left, VectorDouble right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Doubles.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Doubles.Length;
            }
            else if (left._vector.Doubles.Length != right._vector.Doubles.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Doubles.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Subtract(left._vector.ToVector256(0), right._vector.ToVector256(0)),
                        size);

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        left._vector[2] - right._vector[2]);
                case 4 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Subtract(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        Sse2.Subtract(left._vector.ToVector128(1), right._vector.ToVector128(1)));

                //Software fallback
                case 1:
                    return new VectorDouble(left._vector[0] - right._vector[0]);
                case 2:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] - right._vector[0],
                            left._vector[1] - right._vector[1]
                        }, 0);
                case 3:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] - right._vector[0],
                            left._vector[1] - right._vector[1],
                            left._vector[2] - right._vector[2]
                        }, 0);
                case 4:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] - right._vector[0],
                            left._vector[1] - right._vector[1],
                            left._vector[2] - right._vector[2],
                            left._vector[3] - right._vector[3]
                        }, 0);
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        double[] values64 = new double[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            values64[i] = left._vector[i] - right._vector[i];
                        }

                        return new VectorDouble(values64);
                    }

                    Vector128<double>[] blocks128 = null;
                    Vector256<double>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<double>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            blocks256[i] = Avx.Subtract(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<double>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            blocks128[i] = Sse2.Subtract(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        return new VectorDouble(size,
                            left._vector[processedSubOperations] - right._vector[processedSubOperations], blocks128,
                            blocks256);
                    }

                    return new VectorDouble(size, value: null, blocks128, blocks256);
            }
        }

        //TODO Add Dot/Transposed Multiplication, Cross?
        //Element Wise Multiplication
        public static VectorDouble operator *(VectorDouble left, VectorDouble right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Doubles.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Doubles.Length;
            }
            else if (left._vector.Doubles.Length != right._vector.Doubles.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Doubles.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Multiply(left._vector.ToVector256(0), right._vector.ToVector256(0)),
                        size);

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        left._vector[2] * right._vector[2]);
                case 4 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Multiply(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        Sse2.Multiply(left._vector.ToVector128(1), right._vector.ToVector128(1)));

                //Software fallback
                case 1:
                    return new VectorDouble(left._vector[0] * right._vector[0]);
                case 2:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] * right._vector[0],
                            left._vector[1] * right._vector[1]
                        }, 0);
                case 3:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] * right._vector[0],
                            left._vector[1] * right._vector[1],
                            left._vector[2] * right._vector[2]
                        }, 0);
                case 4:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] * right._vector[0],
                            left._vector[1] * right._vector[1],
                            left._vector[2] * right._vector[2],
                            left._vector[3] * right._vector[3]
                        }, 0);
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        double[] values64 = new double[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            values64[i] = left._vector[i] * right._vector[i];
                        }

                        return new VectorDouble(values64);
                    }

                    Vector128<double>[] blocks128 = null;
                    Vector256<double>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<double>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            blocks256[i] = Avx.Multiply(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<double>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            blocks128[i] = Sse2.Multiply(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        return new VectorDouble(size,
                            left._vector[processedSubOperations] * right._vector[processedSubOperations], blocks128,
                            blocks256);
                    }

                    return new VectorDouble(size, value: null, blocks128, blocks256);
            }
        }

        //Element wise multiplication
        public static VectorDouble operator *(VectorDouble value, double factor)
        {
            //TODO Add Sse2/Avx support? Requires broadcasting factor to a vector and then multiplying with full/partial size vector instruction support
            switch (value._vector.Doubles.Length)
            {
                case 1:
                    return new VectorDouble(value._vector[0] * factor);
                case 2:
                    return new VectorDouble(
                        new[] { value._vector[0] * factor, value._vector[1] * factor }, 0);
                case 3:
                    return new VectorDouble(
                        new[]
                        {
                            value._vector[0] * factor, value._vector[1] * factor,
                            value._vector[2] * factor
                        }, 0);
                case 4:
                    return new VectorDouble(
                        new[]
                        {
                            value._vector[0] * factor, value._vector[1] * factor,
                            value._vector[2] * factor, value._vector[3] * factor
                        }, 0);
                default:
                    double[] newValues = new double[value._vector.Doubles.Length];
                    for (int i = 0; i < value._vector.Doubles.Length; i++)
                    {
                        newValues[i] = value._vector[i] * factor;
                    }

                    return new VectorDouble(newValues);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorDouble operator *(double factor, VectorDouble value) => value * factor;

        //Element wise division
        public static VectorDouble operator /(VectorDouble left, VectorDouble right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Doubles.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Doubles.Length;
            }
            else if (left._vector.Doubles.Length != right._vector.Doubles.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Doubles.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)));
                case 3 when false&&Avx.IsSupported:
                case 4 when false&&Avx.IsSupported:
                    return new VectorDouble(Avx.Divide(left._vector.ToVector256(0), right._vector.ToVector256(0)),
                        size);

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        left._vector[2] / right._vector[2]);
                case 4 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Divide(left._vector.ToVector128(0), right._vector.ToVector128(0)),
                        Sse2.Divide(left._vector.ToVector128(1), right._vector.ToVector128(1)));

                //Software fallback
                case 1:
                    return new VectorDouble(left._vector[0] / right._vector[0]);
                case 2:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] * right._vector[0],
                            left._vector[1] * right._vector[1]
                        }, 0);
                case 3:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] / right._vector[0],
                            left._vector[1] / right._vector[1],
                            left._vector[2] / right._vector[2]
                        }, 0);
                case 4:
                    return new VectorDouble(
                        new[]
                        {
                            left._vector[0] / right._vector[0],
                            left._vector[1] / right._vector[1],
                            left._vector[2] / right._vector[2],
                            left._vector[3] / right._vector[3]
                        }, 0);
                default:
                    //Assumption is made that no Sse2 support means no Avx support
                    if (!Sse2.IsSupported)
                    {
                        double[] values64 = new double[size];

                        for (int i = 0; i < values64.Length; i++)
                        {
                            values64[i] = left._vector[i] / right._vector[i];
                        }

                        return new VectorDouble(values64);
                    }

                    Vector128<double>[] blocks128 = null;
                    Vector256<double>[] blocks256 = null;

                    int remainingSubOperations = size;
                    int processedSubOperations = 0;

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        blocks256 = new Vector256<double>[remainingSubOperations >> 2];

                        for (int i = 0; i < blocks256.Length; i++)
                        {
                            blocks256[i] = Avx.Divide(left._vector.ToVector256(i), right._vector.ToVector256(i));
                        }

                        remainingSubOperations -= blocks256.Length << 2;
                        processedSubOperations += blocks256.Length << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        blocks128 = new Vector128<double>[remainingSubOperations >> 1];

                        for (int i = 0, j = processedSubOperations >> 1; i < blocks128.Length; i++, j++)
                        {
                            blocks128[i] = Sse2.Divide(left._vector.ToVector128(j), right._vector.ToVector128(j));
                        }

                        remainingSubOperations -= blocks128.Length << 1;
                        processedSubOperations += blocks128.Length << 1;
                    }

                    if (remainingSubOperations == 1)
                    {
                        return new VectorDouble(size,
                            left._vector[processedSubOperations] / right._vector[processedSubOperations], blocks128,
                            blocks256);
                    }

                    return new VectorDouble(size, value: null, blocks128, blocks256);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorDouble operator -(VectorDouble value) => Zero - value;

        //TODO Add bitwise operations when integer types are supported

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VectorDouble left, VectorDouble right)
        {
            int size;

            if (left._vector.MultiSize)
            {
                size = right._vector.Doubles.Length;
            }
            else if (right._vector.MultiSize)
            {
                size = left._vector.Doubles.Length;
            }
            else if (left._vector.Doubles.Length != right._vector.Doubles.Length)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left._vector.Doubles.Length;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                    {
                        Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                        return Sse2.MoveMask(result) == 0b11;
                    }
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    {
                        Vector256<double> result = Avx.Compare(left._vector.ToVector256(0), right._vector.ToVector256(0),
                            FloatComparisonMode.OrderedEqualNonSignaling);
                        return Avx.MoveMask(result) == 0b1111;
                    }

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                    {
                        Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                        return Sse2.MoveMask(result) == 0b11 && left._vector[2].Equals(right._vector[2]);
                    }
                case 4 when Sse2.IsSupported:
                    {
                        Vector128<double> result1 = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                        Vector128<double> result2 = Sse2.CompareEqual(left._vector.ToVector128(1), right._vector.ToVector128(1));
                        return Sse2.MoveMask(result1) == 0b11 && Sse2.MoveMask(result2) == 0b11;
                    }

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
                //TODO Fix bug that causes all failures above to run this code when they should just return false
                default:
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

                    if (Avx.IsSupported && remainingSubOperations >= 4)
                    {
                        int count256 = remainingSubOperations >> 2;

                        for (int i = 0; i < count256; i++)
                        {
                            Vector256<double> result = Avx.Compare(left._vector.ToVector256(0), right._vector.ToVector256(0),
                                FloatComparisonMode.OrderedEqualNonSignaling);
                            if (Avx.MoveMask(result) != 0b1111)
                            {
                                return false;
                            }
                        }

                        remainingSubOperations -= count256 << 2;
                        processedSubOperations += count256 << 2;
                    }

                    if (remainingSubOperations >= 2)
                    {
                        int count128 = remainingSubOperations >> 1;

                        for (int i = 0; i < count128; i++)
                        {
                            Vector128<double> result = Sse2.CompareEqual(left._vector.ToVector128(0), right._vector.ToVector128(0));
                            if (Sse2.MoveMask(result) != 0b11)
                            {
                                return false;
                            }
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VectorDouble left, VectorDouble right) => !(left == right);
    }
}