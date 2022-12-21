using System.Runtime.InteropServices;

namespace ArbitraryLengthVectors;

[StructLayout(LayoutKind.Explicit)]
internal unsafe struct Register
{
    [FieldOffset(0)]
    internal void* Items;
}
