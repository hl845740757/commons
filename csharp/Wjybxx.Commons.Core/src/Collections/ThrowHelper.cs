using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 集合内部工具方法
/// </summary>
internal static class ThrowHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InvalidOperationException CollectionFullException() {
        return new InvalidOperationException("Collection is full");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InvalidOperationException CollectionEmptyException() {
        return new InvalidOperationException("Collection is Empty");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyNotFoundException KeyNotFoundException(object? key) {
        return new KeyNotFoundException(key == null ? "null" : key.ToString());
    }
}
}