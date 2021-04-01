//Port of Vector<T> from https://github.com/dotnet/runtime/blob/76a50c6/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector_1.cs which is licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Vectors
{
    //TODO Split 3 and 4 value fallback cases
    public readonly struct VectorDouble : IEquatable<VectorDouble>
    {
        //TODO Replace with direct register access like Vector<T> uses
        //TODO Merge 1 with 2 once 3 has been added?
        private readonly Vector64<double> _vector64;
        private readonly Vector128<double> _vector128;
        private readonly Vector256<double> _vector256;

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

        //TODO is this needed or could constants just use the biggest supported size?
        private VectorDouble(double value, bool allSizes)
        {
            _vector64 = Vector64.Create(value);
            _vector128 = Vector128.Create(value);
            _vector256 = Vector256.Create(value);
            Count = 2;
            MultiSizeVector = true;
        }

        private VectorDouble(Vector128<double> values)
        {
            _vector64 = default;
            _vector128 = values;
            _vector256 = default;
            Count = 2;
            MultiSizeVector = false;
        }

        private VectorDouble(Vector256<double> values, byte count)
        {
            _vector64 = default;
            _vector128 = default;
            _vector256 = values;
            Count = count;
            MultiSizeVector = false;
        }

        public VectorDouble(double value)
        {
            _vector64 = Vector64.Create(value);
            _vector128 = default;
            _vector256 = default;
            Count = 1;
            MultiSizeVector = false;
        }

        public VectorDouble(double[] values) : this(values, 0, 0) { }

        public VectorDouble(double[] value, int index) : this(value, index, 0) { }

        private VectorDouble(double[] values, int index, byte count)
        {
            if (values is null)
            {
                throw new NullReferenceException();
            }

            if (index < 0 || values.Length <= index)
            {
                throw new IndexOutOfRangeException();
            }

            Count = count == 0 ? (byte)(values.Length - index) : count;

            switch (Count)
            {
                case 1:
                    _vector64 = Vector64.Create(values[index]);
                    _vector128 = default;
                    _vector256 = default;
                    break;
                case 2:
                    _vector64 = default;
                    _vector128 = Vector128.Create(values[index], values[index + 1]);
                    _vector256 = default;
                    break;
                case 3:
                    _vector64 = default;
                    _vector128 = default;
                    _vector256 = Vector256.Create(values[index], values[index + 1], values[index + 2], 0);
                    break;
                case 4:
                    _vector64 = default;
                    _vector128 = default;
                    _vector256 = Vector256.Create(values[index], values[index + 1], values[index + 2],
                        values[index + 3]);
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            MultiSizeVector = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(ReadOnlySpan<double> values)
        {
            Count = (byte)values.Length;

            switch (Count)
            {
                case 1:
                    _vector64 = Vector64.Create(values[0]);
                    _vector128 = default;
                    _vector256 = default;
                    Count = 1;
                    break;
                case 2:
                    _vector64 = default;
                    _vector128 = Vector128.Create(values[0], values[1]);
                    _vector256 = default;
                    Count = 2;
                    break;
                case 3:
                    _vector64 = default;
                    _vector128 = default;
                    _vector256 = Vector256.Create(values[0], values[1], values[2], 0);
                    break;
                case 4:
                    _vector64 = default;
                    _vector128 = default;
                    _vector256 = Vector256.Create(values[0], values[1], values[2], values[3]);
                    Count = 4;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            MultiSizeVector = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VectorDouble(Span<double> values) : this((ReadOnlySpan<double>)values) { }

        //TODO Add CopyTo overloads and index once more than one item is supported

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
            (obj is VectorDouble other) && Equals(other);

        public readonly bool Equals(VectorDouble other) => this == other;

        public override readonly int GetHashCode()
        {
            if (Count == 1)
            {
                return _vector64.ToScalar().GetHashCode();
            }

            HashCode hashCode = default;

            if (Count == 2)
            {
                hashCode.Add(_vector128.GetElement(0));
                hashCode.Add(_vector128.GetElement(1));
            }
            else
            {
                hashCode.Add(_vector256.GetElement(0));
                hashCode.Add(_vector256.GetElement(1));
                hashCode.Add(_vector256.GetElement(2));
                if (Count == 4)
                {
                    hashCode.Add(_vector256.GetElement(3));
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
                    sb.Append(_vector64.ToScalar().ToString(format, formatProvider));
                    break;
                case 2:
                    sb.Append(_vector128.GetElement(0).ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector128.GetElement(1).ToString(format, formatProvider));
                    break;
                case 3:
                case 4:
                    sb.Append(_vector256.GetElement(0).ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector256.GetElement(1).ToString(format, formatProvider));
                    sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                    sb.Append(' ');
                    sb.Append(_vector256.GetElement(2).ToString(format, formatProvider));
                    if (Count == 4)
                    {
                        sb.Append(NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator);
                        sb.Append(' ');
                        sb.Append(_vector256.GetElement(3).ToString(format, formatProvider));
                    }
                    break;
            }

            sb.Append('>');
            return sb.ToString();
        }

        //TODO Add TryCopyTo overloads once more than one item is supported

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

            switch (size)
            {
                case 1:
                    return new VectorDouble(left._vector64.ToScalar() + right._vector64.ToScalar());
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Add(left._vector128, right._vector128));
                case 2:
                    return new VectorDouble(new[]
                    {
                        left._vector128.GetElement(0) + right._vector128.GetElement(0),
                        left._vector128.GetElement(1) + right._vector128.GetElement(1)
                    }, 0, size);
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Add(left._vector256, right._vector256), size);
                case 3:
                case 4:
                    return new VectorDouble(new[]
                    {
                        left._vector256.GetElement(0) + right._vector256.GetElement(0),
                        left._vector256.GetElement(1) + right._vector256.GetElement(1),
                        left._vector256.GetElement(2) + right._vector256.GetElement(2),
                        left._vector256.GetElement(3) + right._vector256.GetElement(3)
                    }, 0, size);
                default:
                    throw new IndexOutOfRangeException();
            }
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

            switch (size)
            {
                case 1:
                    return new VectorDouble(left._vector64.ToScalar() - right._vector64.ToScalar());
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Subtract(left._vector128, right._vector128));
                case 2:
                    return new VectorDouble(new[]
                    {
                        left._vector128.GetElement(0) - right._vector128.GetElement(0),
                        left._vector128.GetElement(1) - right._vector128.GetElement(1)
                    }, 0, size);
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Subtract(left._vector256, right._vector256), size);
                case 3:
                case 4:
                    return new VectorDouble(new[]
                    {
                        left._vector256.GetElement(0) - right._vector256.GetElement(0),
                        left._vector256.GetElement(1) - right._vector256.GetElement(1),
                        left._vector256.GetElement(2) - right._vector256.GetElement(2),
                        left._vector256.GetElement(3) - right._vector256.GetElement(3)
                    }, 0, size);
                default:
                    throw new IndexOutOfRangeException();
            }
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

            switch (size)
            {
                case 1:
                    return new VectorDouble(left._vector64.ToScalar() * right._vector64.ToScalar());
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Multiply(left._vector128, right._vector128));
                case 2:
                    return new VectorDouble(new[]
                    {
                        left._vector128.GetElement(0) * right._vector128.GetElement(0),
                        left._vector128.GetElement(1) * right._vector128.GetElement(1)
                    }, 0, size);
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Multiply(left._vector256, right._vector256), size);
                case 3:
                case 4:
                    return new VectorDouble(new[]
                    {
                        left._vector256.GetElement(0) * right._vector256.GetElement(0),
                        left._vector256.GetElement(1) * right._vector256.GetElement(1),
                        left._vector256.GetElement(2) * right._vector256.GetElement(2),
                        left._vector256.GetElement(3) * right._vector256.GetElement(3)
                    }, 0, size);
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        //Element wise multiplication
        public static VectorDouble operator *(VectorDouble value, double factor)
        {
            if (value.Count == 1)
            {
                return new VectorDouble(value._vector64.ToScalar() * factor);
            }

            //TODO Add Sse2/Avx support? Requires broadcasting factor to a vector and then multiplying
            switch (value.Count)
            {
                case 1:
                    return new VectorDouble(value._vector64.ToScalar() * factor);
                case 2:
                    return new VectorDouble(new[]
                    {
                        value._vector128.GetElement(0) * factor,
                        value._vector128.GetElement(1) * factor
                    }, 0, value.Count);
                case 3:
                case 4:
                    return new VectorDouble(new[]
                    {
                        value._vector256.GetElement(0) * factor,
                        value._vector256.GetElement(1) * factor,
                        value._vector256.GetElement(2) * factor,
                        value._vector256.GetElement(3) * factor
                    }, 0, value.Count);
                default:
                    throw new IndexOutOfRangeException();
            }
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

            switch (size)
            {
                case 1:
                    return new VectorDouble(left._vector64.ToScalar() / right._vector64.ToScalar());
                case 2 when Sse2.IsSupported:
                    return new VectorDouble(Sse2.Divide(left._vector128, right._vector128));
                case 2:
                    return new VectorDouble(new[]
                    {
                        left._vector128.GetElement(0) / right._vector128.GetElement(0),
                        left._vector128.GetElement(1) / right._vector128.GetElement(1)
                    }, 0, size);
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    return new VectorDouble(Avx.Divide(left._vector256, right._vector256), size);
                case 3:
                case 4:
                    return new VectorDouble(new[]
                    {
                        left._vector256.GetElement(0) / right._vector256.GetElement(0),
                        left._vector256.GetElement(1) / right._vector256.GetElement(1),
                        left._vector256.GetElement(2) / right._vector256.GetElement(2),
                        left._vector256.GetElement(3) / right._vector256.GetElement(3)
                    }, 0, size);
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VectorDouble operator -(VectorDouble value) => Zero - value;

        //TODO Add, double {&, |, ^} double is not supported, can be faked using int64 like in https://github.com/dotnet/runtime/blob/bce310d872c211049506f90149d498ab26ca6f81/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector_1.cs#L461
        /*public static VectorDouble operator &(VectorDouble left, VectorDouble right)
        {
            return new(left._vector64.ToScalar() & right._vector64.ToScalar());
        }

        public static VectorDouble operator |(VectorDouble left, VectorDouble right)
        {
            return new(left._vector64.ToScalar() | right._vector64.ToScalar());
        }

        public static VectorDouble operator ^(VectorDouble left, VectorDouble right)
        {
            return new(left._vector64.ToScalar() ^ right._vector64.ToScalar());
        }

        //Todo Inline, add VectorDouble.AllBitsSet?
        public static VectorDouble operator ~(VectorDouble value)
        {
            return new(Vector64<double>.AllBitsSet.ToScalar() ^ value._vector64.ToScalar());
        }*/

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
                case 2 when Sse2.IsSupported:
                    {
                        Vector128<double> result = Sse2.CompareEqual(left._vector128, right._vector128);
                        return Sse2.MoveMask(result) == 0b11;
                    }
                case 3 when Avx.IsSupported:
                case 4 when Avx.IsSupported:
                    {
                        Vector256<double> result = Avx.Compare(left._vector256, right._vector256, FloatComparisonMode.OrderedEqualNonSignaling);
                        return Avx.MoveMask(result) == 0b1111;
                    }
                case 1 when left._vector64.ToScalar().Equals(right._vector64.ToScalar()):
                case 2 when left._vector128.GetElement(0).Equals(right._vector128.GetElement(0)) &&
                            left._vector128.GetElement(1).Equals(right._vector128.GetElement(1)):
                case 4 when left._vector256.GetElement(0).Equals(right._vector256.GetElement(0)) &&
                            left._vector256.GetElement(1).Equals(right._vector256.GetElement(1)) &&
                            left._vector256.GetElement(2).Equals(right._vector256.GetElement(2)) &&
                            left._vector256.GetElement(3).Equals(right._vector256.GetElement(3)):
                    return true;
                default:
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VectorDouble left, VectorDouble right) => !(left == right);
    }
}