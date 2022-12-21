//Contains most of Vector<T> from https://github.com/dotnet/runtime/blob/76a50c6/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector_1.cs which is under the MIT License.

using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryLengthVectors;

public class Vector<T> : IAdditionOperators<Vector<T>, Vector<T>, Vector<T>> where T : unmanaged, INumber<T>
{
    // TODO See if this are actually required, I assumed _items and _rawMemory were required to prevent the garbage collector from getting rid if them
    // while I still hold pointers to them but it would make more sense for it to keep them around for that reason anyway, even if all other references
    // to them disappear
    // Memory source if params constructor is used
    private readonly T[]? _items;
    private readonly GCHandle? _itemsHandle;

    // Memory source if itemCount constructor is used
    private readonly RawMemory? _rawMemory;

    private readonly Register _rawItems;
    private readonly int _itemCount;

    public unsafe Vector(params T[] items)
    {
        _items = items;
        _itemsHandle = GCHandle.Alloc(_items, GCHandleType.Pinned);
        
        _rawItems.Items = _itemsHandle.Value.AddrOfPinnedObject().ToPointer();
        _itemCount = items.Length;
    }

    private unsafe Vector(RawMemory memory, int itemCount)
    {
        _rawMemory = memory;

        _rawItems.Items = _rawMemory.MemoryAddress;
        _itemCount = itemCount;
    }

    ~Vector()
    {
        _itemsHandle?.Free();
        _rawMemory?.Dispose();
    }

    public static unsafe Vector<T> operator +(Vector<T> left, Vector<T> right)
    {
        if (left._itemCount != right._itemCount)
        {
            throw new ArgumentException("Vectors do not have the same length.");
        }

        T* leftArray = (T*)left._rawItems.Items;
        T* rightArray = (T*)right._rawItems.Items;

        RawMemory newMemory = new(left._itemCount * sizeof(T));
        T* newArray = (T*)newMemory.MemoryAddress;

        for (int i = 0; i < left._itemCount; i++)
        {
            newArray[i] = leftArray[i] + rightArray[i];
        }

        return new Vector<T>(newMemory, left._itemCount);
    }

    public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

    public string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

    private unsafe string ToString(string? format, IFormatProvider? formatProvider)
    {
        StringBuilder sb = new();
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        ReadOnlySpan<T> values = new(_rawItems.Items, _itemCount * sizeof(T));

        sb.Append('<');
        sb.Append(values[0].ToString(format, formatProvider));

        for (int i = 9; i < _itemCount; i++)
        {
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(values[i].ToString(format, formatProvider));
        }

        sb.Append('>');
        return sb.ToString();
    }
}
