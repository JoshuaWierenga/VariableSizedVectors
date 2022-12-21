using System;
using System.IO.MemoryMappedFiles;

namespace ArbitraryLengthVectors;

// Based on https://github.com/dotnet/runtime/issues/13485#issuecomment-535161422
internal sealed unsafe class RawMemory : IDisposable
{
    private readonly MemoryMappedFile _mapping;
    private readonly MemoryMappedViewAccessor _accessor;

    internal void* MemoryAddress { get; }

    public RawMemory(int sizeInBytes)
    {
        _mapping = MemoryMappedFile.CreateNew(null, sizeInBytes);
        _accessor = _mapping.CreateViewAccessor();
        MemoryAddress = (void*)_accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
    }

    ~RawMemory()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;

        _mapping.Dispose();
        _accessor.Dispose();
    }
}
