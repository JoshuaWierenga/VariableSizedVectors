using System.Runtime.Intrinsics.X86;

namespace Vectors
{
    //X.IsSupported is used all over Vector<T>, this should sightly improve performance by only doing each check once
    internal static class IntrinsicSupport
    {
        internal static bool IsSseSupported { get; } = Sse.IsSupported;

        internal static bool IsSse2Supported { get; } = Sse2.IsSupported;

        internal static bool IsAvxSupported { get; } = Avx.IsSupported;

        internal static bool IsAvx2Supported { get; } = Avx2.IsSupported;
    }
}