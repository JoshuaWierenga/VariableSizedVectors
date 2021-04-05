using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Vectors
{
    internal readonly unsafe struct RegisterDebugView
    {
        private readonly Register _value;
        private readonly int _byteLength;
        private readonly int _vector128Length;

        public RegisterDebugView(Register value)
        {
            _value = value;
            _byteLength = _value.Length * _value.ElementSize;
            _vector128Length = _byteLength / 16;
        }

#if DEBUG
        public byte[] ByteView
        {
            get
            {
                byte[] items = new byte[_byteLength];
                Unsafe.CopyBlockUnaligned(ref items[0], ref Unsafe.AsRef<byte>(_value.pUInt8Values),
                    (uint)_byteLength);
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                sbyte[] items = new sbyte[_byteLength];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<sbyte, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pInt8Values), (uint)_byteLength);
                return items;
            }
        }

        public ushort[] UShortView
        {
            get
            {
                int length = _byteLength / sizeof(ushort);
                ushort[] items = new ushort[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<ushort, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt16Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public short[] ShortView
        {
            get
            {
                int length = _byteLength / sizeof(short);
                short[] items = new short[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<short, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt16Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public uint[] UIntView
        {
            get
            {
                int length = _byteLength / sizeof(uint);
                uint[] items = new uint[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<uint, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pUInt32Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public int[] IntView
        {
            get
            {
                int length = _byteLength / sizeof(int);
                int[] items = new int[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt32Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public ulong[] UlongView
        {
            get
            {
                int length = _byteLength / sizeof(ulong);
                ulong[] items = new ulong[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<ulong, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pUInt64Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public long[] LongView
        {
            get
            {
                int length = _byteLength / sizeof(long);
                long[] items = new long[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<long, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt64Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public float[] FloatView
        {
            get
            {
                int length = _byteLength / sizeof(float);
                float[] items = new float[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<float, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pBinary32Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                int length = _byteLength / sizeof(double);
                double[] items = new double[length];
                if (length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pBinary64Values), (uint)_byteLength);
                }
                return items;
            }
        }


        public Vector128<byte>[] Vector128ByteView
        {
            get
            {
                Vector128<byte>[] items = new Vector128<byte>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<byte>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pUInt8Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<sbyte>[] Vector128SByteView
        {
            get
            {
                Vector128<sbyte>[] items = new Vector128<sbyte>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<sbyte>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt8Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<ushort>[] Vector128UShortView
        {
            get
            {
                Vector128<ushort>[] items = new Vector128<ushort>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<ushort>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt16Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<short>[] Vector128ShortView
        {
            get
            {
                Vector128<short>[] items = new Vector128<short>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<short>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt16Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<uint>[] Vector128UIntView
        {
            get
            {
                Vector128<uint>[] items = new Vector128<uint>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<uint>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pUInt32Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<int>[] Vector128IntView
        {
            get
            {
                Vector128<int>[] items = new Vector128<int>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt32Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<ulong>[] Vector128UlongView
        {
            get
            {
                Vector128<ulong>[] items = new Vector128<ulong>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<ulong>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pUInt64Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<long>[] Vector128LongView
        {
            get
            {
                Vector128<long>[] items = new Vector128<long>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<long>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pInt64Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<float>[] Vector128FloatView
        {
            get
            {
                Vector128<float>[] items = new Vector128<float>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pBinary32Values), (uint)_byteLength);
                }
                return items;
            }
        }

        public Vector128<double>[] Vector128DoubleView
        {
            get
            {
                Vector128<double>[] items = new Vector128<double>[_vector128Length];
                if (_vector128Length != 0)
                {
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref items[0]),
                        ref Unsafe.AsRef<byte>(_value.pBinary64Values), (uint)_byteLength);
                }
                return items;
            }
        }
#endif
    }
}