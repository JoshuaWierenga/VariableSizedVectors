//Port of Vector<T> from https://github.com/dotnet/runtime/blob/76a50c6/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector_1.cs which is licensed under the MIT License.

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
    //TODO Support larger vectors, first with operations via software fallback and then via optimised elementwise operations
    public readonly struct VectorDouble : IEquatable<VectorDouble>, IFormattable
    {
        private readonly Register _vector;

        public readonly byte Count;
        private readonly bool MultiSizeVector;

        public static VectorDouble Zero
        {
            get
            {
                return new(0, true);
            }
        }

        public static VectorDouble One
        {
            get
            {
                return new(1, true);
            }
        }

        internal static VectorDouble AllBitsSet
        {
            get
            {
                return new(BitConverter.Int64BitsToDouble(-1), true);
            }
        }

        private VectorDouble(double value, bool allSizes)
        {
            _vector = new Register(value);
            Count = 4;
            MultiSizeVector = true;
        }

        private VectorDouble(Vector128<double> values)
        {
            _vector = new Register(values);
            Count = 2;
            MultiSizeVector = false;
        }

        private VectorDouble(Vector256<double> values, byte count)
        {
            _vector = new Register(values);
            Count = count;
            MultiSizeVector = false;
        }

        public VectorDouble(double value)
        {
            _vector = new Register(value);
            Count = 1;
            MultiSizeVector = false;
        }

        public VectorDouble(double[] values) : this(values, 0) { }

        public VectorDouble(double[] values, int index)
        {
            if (values is null)
            {
                throw new NullReferenceException();
            }

            if (index < 0 || values.Length <= index || values.Length - index > 4)
            {
                throw new IndexOutOfRangeException();
            }

            Count = (byte)(values.Length - index);

            _vector = Unsafe.ReadUnaligned<Register>(ref Unsafe.As<double, byte>(ref values[index]));

            MultiSizeVector = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(ReadOnlySpan<byte> values)
        {
            //64 is sizeof(double)*4
            if (values.Length >= 64)
            {
                throw new IndexOutOfRangeException();
            }

            Count = (byte)(values.Length / 8);

            _vector = Unsafe.ReadUnaligned<Register>(ref MemoryMarshal.GetReference(values));

            MultiSizeVector = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(ReadOnlySpan<double> values)
        {
            if (values.Length > 4)
            {
                throw new IndexOutOfRangeException();
            }

            Count = (byte)values.Length;

            _vector = Unsafe.ReadUnaligned<Register>(
                ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(values)));

            MultiSizeVector = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(Span<double> values) : this((ReadOnlySpan<double>)values) { }

        public readonly void CopyTo(Span<byte> destination)
        {
            //8 is sizeof(double)
            if ((uint)destination.Length != (uint)(Count * 8))
            {
                throw new ArgumentException();
            }

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), _vector);
        }

        public readonly void CopyTo(Span<double> destination)
        {
            if ((uint)destination.Length != (uint)Count)
            {
                throw new ArgumentException();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)), _vector);
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

            if (destination.Length - startIndex > 4)
            {
                throw new ArgumentException();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref destination[startIndex]), _vector);
        }

        public readonly unsafe double this[int index] =>
            index switch
            {
                0 => _vector.double_0,
                1 when Count >= 2 => _vector.double_1,
                2 when Count >= 3 => _vector.double_2,
                3 when Count >= 4 => _vector.double_3,
                _ => throw new IndexOutOfRangeException()
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
            (obj is VectorDouble other) && Equals(other);

        public readonly bool Equals(VectorDouble other) => this == other;

        public override readonly int GetHashCode()
        {
            if (Count == 1)
            {
                return _vector.double_0.GetHashCode();
            }

            HashCode hashCode = default;

            if (Count == 2)
            {
                hashCode.Add(_vector.double_0);
                hashCode.Add(_vector.double_1);
            }
            else
            {
                hashCode.Add(_vector.double_0);
                hashCode.Add(_vector.double_1);
                hashCode.Add(_vector.double_2);
                if (Count == 4)
                {
                    hashCode.Add(_vector.double_3);
                }
            }

            return hashCode.ToHashCode();
        }

        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

        public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            StringBuilder sb = new();
            sb.Append('<');

            switch (Count)
            {
                case 1:
                    sb.Append(_vector.double_0.ToString(format, formatProvider));
                    break;
                case 2:
                    sb.Append(_vector.double_0.ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector.double_1.ToString(format, formatProvider));
                    break;
                case 3:
                case 4:
                    sb.Append(_vector.double_0.ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector.double_1.ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector.double_2.ToString(format, formatProvider));
                    if (Count == 4)
                    {
                        sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                        sb.Append(' ');
                        sb.Append(_vector.double_3.ToString(format, formatProvider));
                    }
                    break;
            }

            sb.Append('>');
            return sb.ToString();
        }

        public readonly bool TryCopyTo(Span<byte> destination)
        {
            //8 is sizeof(double)
            if ((uint)destination.Length < (uint)Count * 8)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), _vector);
            return true;
        }

        public readonly bool TryCopyTo(Span<double> destination)
        {
            if ((uint)destination.Length < (uint)Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)), _vector);
            return true;
        }

        //Operators
        public static VectorDouble operator +(VectorDouble left, VectorDouble right)
        {
            byte size;

            if (left.MultiSizeVector)
            {
                size = right.Count;
            }
            else if (right.MultiSizeVector)
            {
                size = left.Count;
            }
            else if (left.Count != right.Count)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left.Count;
            }

            return size switch
            {
                //Full size vector instructions
                2 when Sse2.IsSupported => new VectorDouble(Sse2.Add(left._vector.vector_128_0,
                    right._vector.vector_128_0)),
                3 when Avx.IsSupported => new VectorDouble(Avx.Add(left._vector.vector_256, right._vector.vector_256),
                    size),
                4 when Avx.IsSupported => new VectorDouble(Avx.Add(left._vector.vector_256, right._vector.vector_256),
                    size),

                //Full size vector instructions
                //Partial size vector instructions
                3 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Add(left._vector.vector_128_0, right._vector.vector_128_0),
                        Vector128.CreateScalarUnsafe(left._vector.double_2 + right._vector.double_2)), size),
                4 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Add(left._vector.vector_128_0, right._vector.vector_128_0),
                        Sse2.Add(left._vector.vector_128_1, right._vector.vector_128_1)), size),

                //Software fallback
                1 => new VectorDouble(left._vector.double_0 + right._vector.double_0),
                2 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 + right._vector.double_0,
                        left._vector.double_1 + right._vector.double_1
                    }, 0),
                3 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 + right._vector.double_0,
                        left._vector.double_1 + right._vector.double_1,
                        left._vector.double_2 + right._vector.double_2
                    }, 0),
                4 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 + right._vector.double_0,
                        left._vector.double_1 + right._vector.double_1,
                        left._vector.double_2 + right._vector.double_2,
                        left._vector.double_3 + right._vector.double_3
                    }, 0),

                _ => throw new IndexOutOfRangeException()
            };
        }

        public static VectorDouble operator -(VectorDouble left, VectorDouble right)
        {
            byte size;

            if (left.MultiSizeVector)
            {
                size = right.Count;
            }
            else if (right.MultiSizeVector)
            {
                size = left.Count;
            }
            else if (left.Count != right.Count)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left.Count;
            }

            return size switch
            {
                //Full size vector instructions
                2 when Sse2.IsSupported => new VectorDouble(Sse2.Subtract(left._vector.vector_128_0, right._vector.vector_128_0)),
                3 when Avx.IsSupported => new VectorDouble(Avx.Subtract(left._vector.vector_256, right._vector.vector_256), size),
                4 when Avx.IsSupported => new VectorDouble(Avx.Subtract(left._vector.vector_256, right._vector.vector_256), size),

                //Partial size vector instructions
                3 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Subtract(left._vector.vector_128_0, right._vector.vector_128_0),
                        Vector128.CreateScalarUnsafe(left._vector.double_2 - right._vector.double_2)), size),
                4 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Subtract(left._vector.vector_128_0, right._vector.vector_128_0),
                        Sse2.Subtract(left._vector.vector_128_1, right._vector.vector_128_1)), size),

                1 => new VectorDouble(left._vector.double_0 - right._vector.double_0),
                2 => new VectorDouble(
                    new[] { left._vector.double_0 - right._vector.double_0, left._vector.double_1 - right._vector.double_1 }, 0),
                3 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 - right._vector.double_0, left._vector.double_1 - right._vector.double_1,
                        left._vector.double_2 - right._vector.double_2
                    }, 0),
                4 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 - right._vector.double_0, left._vector.double_1 - right._vector.double_1,
                        left._vector.double_2 - right._vector.double_2, left._vector.double_3 - right._vector.double_3
                    }, 0),

                _ => throw new IndexOutOfRangeException()
            };
        }

        //TODO Add Dot/Transposed Multiplication, Cross?
        //Element Wise Multiplication
        public static VectorDouble operator *(VectorDouble left, VectorDouble right)
        {
            byte size;

            if (left.MultiSizeVector)
            {
                size = right.Count;
            }
            else if (right.MultiSizeVector)
            {
                size = left.Count;
            }
            else if (left.Count != right.Count)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left.Count;
            }

            return size switch
            {
                //Full size vector instructions
                2 when Sse2.IsSupported => new VectorDouble(Sse2.Multiply(left._vector.vector_128_0, right._vector.vector_128_0)),
                3 when Avx.IsSupported => new VectorDouble(Avx.Multiply(left._vector.vector_256, right._vector.vector_256), size),
                4 when Avx.IsSupported => new VectorDouble(Avx.Multiply(left._vector.vector_256, right._vector.vector_256), size),

                //Partial size vector instructions
                3 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Multiply(left._vector.vector_128_0, right._vector.vector_128_0),
                        Vector128.CreateScalarUnsafe(left._vector.double_2 * right._vector.double_2)), size),
                4 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Multiply(left._vector.vector_128_0, right._vector.vector_128_0),
                        Sse2.Multiply(left._vector.vector_128_1, right._vector.vector_128_1)), size),

                1 => new VectorDouble(left._vector.double_0 * right._vector.double_0),
                2 => new VectorDouble(
                    new[] { left._vector.double_0 * right._vector.double_0, left._vector.double_1 * right._vector.double_1 }, 0),
                3 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 * right._vector.double_0, left._vector.double_1 * right._vector.double_1,
                        left._vector.double_2 * right._vector.double_2
                    }, 0),
                4 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 * right._vector.double_0, left._vector.double_1 * right._vector.double_1,
                        left._vector.double_2 * right._vector.double_2, left._vector.double_3 * right._vector.double_3
                    }, 0),

                _ => throw new IndexOutOfRangeException()
            };
        }

        //Element wise multiplication
        public static VectorDouble operator *(VectorDouble value, double factor)
        {
            if (value.Count == 1)
            {
                return new VectorDouble(value._vector.double_0 * factor);
            }

            //TODO Add Sse2/Avx support? Requires broadcasting factor to a vector and then multiplying with full/partial size vector instruction support
            return value.Count switch
            {
                1 => new VectorDouble(value._vector.double_0 * factor),
                2 => new VectorDouble(
                    new[] { value._vector.double_0 * factor, value._vector.double_1 * factor }, 0),
                3 => new VectorDouble(
                    new[]
                    {
                        value._vector.double_0 * factor, value._vector.double_1 * factor,value._vector.double_2 * factor
                    }, 0),
                4 => new VectorDouble(
                    new[]
                    {
                        value._vector.double_0 * factor, value._vector.double_1 * factor,value._vector.double_2 * factor,
                        value._vector.double_3 * factor
                    }, 0),

                _ => throw new IndexOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorDouble operator *(double factor, VectorDouble value) => value * factor;

        //Element wise division
        public static VectorDouble operator /(VectorDouble left, VectorDouble right)
        {
            byte size;

            if (left.MultiSizeVector)
            {
                size = right.Count;
            }
            else if (right.MultiSizeVector)
            {
                size = left.Count;
            }
            else if (left.Count != right.Count)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left.Count;
            }

            return size switch
            {
                //Full size vector instructions
                2 when Sse2.IsSupported => new VectorDouble(Sse2.Divide(left._vector.vector_128_0, right._vector.vector_128_0)),
                3 when Avx.IsSupported => new VectorDouble(Avx.Divide(left._vector.vector_256, right._vector.vector_256), size),
                4 when Avx.IsSupported => new VectorDouble(Avx.Divide(left._vector.vector_256, right._vector.vector_256), size),

                //Partial size vector instructions
                3 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Divide(left._vector.vector_128_0, right._vector.vector_128_0),
                        Vector128.CreateScalarUnsafe(left._vector.double_2 / right._vector.double_2)), size),
                4 when Sse2.IsSupported => new VectorDouble(
                    Vector256.Create(Sse2.Divide(left._vector.vector_128_0, right._vector.vector_128_0),
                        Sse2.Divide(left._vector.vector_128_1, right._vector.vector_128_1)), size),

                1 => new VectorDouble(left._vector.double_0 / right._vector.double_0),
                2 => new VectorDouble(
                    new[] { left._vector.double_0 * right._vector.double_0, left._vector.double_1 * right._vector.double_1 }, 0),
                3 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 / right._vector.double_0, left._vector.double_1 / right._vector.double_1,
                        left._vector.double_2 / right._vector.double_2
                    }, 0),
                4 => new VectorDouble(
                    new[]
                    {
                        left._vector.double_0 / right._vector.double_0, left._vector.double_1 / right._vector.double_1,
                        left._vector.double_2 / right._vector.double_2, left._vector.double_3 / right._vector.double_3
                    }, 0),

                _ => throw new IndexOutOfRangeException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorDouble operator -(VectorDouble value) => Zero - value;

        //TODO Add bitwise operations when integer types are supported

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VectorDouble left, VectorDouble right)
        {
            int size;

            if (left.MultiSizeVector)
            {
                size = right.Count;
            }
            else if (right.MultiSizeVector)
            {
                size = left.Count;
            }
            else if (left.Count != right.Count)
            {
                throw new IndexOutOfRangeException();
            }
            else
            {
                size = left.Count;
            }

            switch (size)
            {
                //Full size vector instructions
                case 2 when Sse2.IsSupported:
                    {
                        Vector128<double> result = Sse2.CompareEqual(left._vector.vector_128_0, right._vector.vector_128_0);
                        return Sse2.MoveMask(result) == 0b11;
                    }
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    {
                        Vector256<double> result = Avx.Compare(left._vector.vector_256, right._vector.vector_256,
                            FloatComparisonMode.OrderedEqualNonSignaling);
                        return Avx.MoveMask(result) == 0b1111;
                    }

                //Partial size vector instructions
                case 3 when Sse2.IsSupported:
                    {
                        Vector128<double> result = Sse2.CompareEqual(left._vector.vector_128_0, right._vector.vector_128_0);
                        return Sse2.MoveMask(result) == 0b11 && left._vector.double_2.Equals(right._vector.double_2);

                    }
                case 4 when Sse2.IsSupported:
                    {
                        Vector128<double> result1 = Sse2.CompareEqual(left._vector.vector_128_0, right._vector.vector_128_0);
                        Vector128<double> result2 = Sse2.CompareEqual(left._vector.vector_128_1, right._vector.vector_128_1);
                        return Sse2.MoveMask(result1) == 0b11 && Sse2.MoveMask(result2) == 0b11;
                    }

                case 1 when left._vector.double_0.Equals(right._vector.double_0):
                case 2 when left._vector.double_0.Equals(right._vector.double_0) &&
                            left._vector.double_1.Equals(right._vector.double_1):
                case 3 when left._vector.double_0.Equals(right._vector.double_0) &&
                            left._vector.double_1.Equals(right._vector.double_1) &&
                            left._vector.double_2.Equals(right._vector.double_2):
                case 4 when left._vector.double_0.Equals(right._vector.double_0) &&
                            left._vector.double_1.Equals(right._vector.double_1) &&
                            left._vector.double_2.Equals(right._vector.double_2) &&
                            left._vector.double_3.Equals(right._vector.double_3):
                    return true;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VectorDouble left, VectorDouble right) => !(left == right);
    }
}