using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Vectors
{
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct Register
    {
        //TODO Replace with array
        internal readonly double double_0;
        internal readonly double double_1;
        internal readonly double double_2;
        internal readonly double double_3;

        //internal Vector64<double> vector_64 => => Unsafe.As<Register, Vector64<double>>(ref this);
        internal Vector128<double> vector_128 => Unsafe.As<Register, Vector128<double>>(ref this);
        internal Vector256<double> vector_256 => Unsafe.As<Register, Vector256<double>>(ref this);

        internal Register(double value)
        {
            double_0 = value;
            double_1 = double_2 = double_3 = 0;
        }

        internal Register(Vector128<double> values)
        {
            this = Unsafe.As<Vector128<double>, Register>(ref values);
            double_2 = double_3 = 0;
        }

        internal Register(Vector256<double> values)
        {
            this = Unsafe.As<Vector256<double>, Register>(ref values);
        }
    }
}