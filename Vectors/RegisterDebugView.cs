using System;
using System.Runtime.CompilerServices;

namespace Vectors
{
    internal readonly unsafe struct RegisterDebugView
    {
        private readonly Register _value;

        public RegisterDebugView(Register value)
        {
            _value = value;
        }

#if DEBUG
        public byte[] ByteView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                byte[] items = new byte[byteLength];
                Unsafe.CopyBlockUnaligned(ref items[0], ref Unsafe.AsRef<byte>(_value.pUInt8Values), (uint)byteLength);
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                sbyte[] items = new sbyte[byteLength];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<sbyte, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pInt8Values), (uint)byteLength);
                return items;
            }
        }

        public ushort[] UShortView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(ushort));
                ushort[] items = new ushort[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<ushort, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pInt8Values), (uint)byteLength);
                return items;
            }
        }

        public short[] ShortView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(short));
                short[] items = new short[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<short, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pInt8Values), (uint)byteLength);
                return items;
            }
        }

        public uint[] UIntView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(uint));
                uint[] items = new uint[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<uint, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pUInt32Values), (uint)byteLength);
                return items;
            }
        }

        public int[] IntView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(int));
                int[] items = new int[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pInt32Values), (uint)byteLength);
                return items;
            }
        }

        public ulong[] UlongView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(ulong));
                ulong[] items = new ulong[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<ulong, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pUInt64Values), (uint)byteLength);
                return items;
            }
        }

        public long[] LongView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(long));
                long[] items = new long[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<long, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pInt64Values), (uint)byteLength);
                return items;
            }
        }

        public float[] FloatView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(float));
                float[] items = new float[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<float, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pBinary32Values), (uint)byteLength);
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                int byteLength = _value.Length * _value.ElementSize;
                int length = (int)Math.Ceiling((double)byteLength / sizeof(double));
                double[] items = new double[length];
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<double, byte>(ref items[0]),
                    ref Unsafe.AsRef<byte>(_value.pBinary64Values), (uint)byteLength);
                return items;
            }
        }
#endif
    }
}