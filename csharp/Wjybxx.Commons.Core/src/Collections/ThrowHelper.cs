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
    /// <summary>
    /// 集合已满
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InvalidOperationException CollectionFullException() {
        return new InvalidOperationException("Collection is full");
    }

    /// <summary>
    /// 集合为空
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InvalidOperationException CollectionEmptyException() {
        return new InvalidOperationException("Collection is Empty");
    }

    /// <summary>
    /// 找不到字典的key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyNotFoundException KeyNotFoundException(object? key) {
        return new KeyNotFoundException(key == null ? "null" : key.ToString());
    }

    /// <summary>
    /// 创建一个索引溢出异常
    /// </summary>
    /// <param name="index">索引值</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexOutOfRangeException IndexOutOfRange(int index) {
        return new IndexOutOfRangeException("Index out of range: " + index);
    }

    /// <summary>
    /// 创建一个索引溢出异常
    /// </summary>
    /// <param name="index">创建一个索引溢出异常</param>
    /// <param name="length">数组的长度</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexOutOfRangeException IndexOutOfRange(int index, int length) {
        return new IndexOutOfRangeException($"Index out of range: {index}, {length}");
    }
}
}