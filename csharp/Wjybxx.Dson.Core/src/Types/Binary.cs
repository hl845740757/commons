#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 二进制数据
/// （该类难以实现不可变对象，虽然我们可以封装为ByteArray，但许多接口都是基于byte[]的，封装会导致难以与其它接口协作。）
/// （用户应当避免修改该对象的数据，把它当做不可变对象使用。）
/// </summary>
public sealed class Binary
{
    public static readonly Binary EMPTY = new Binary(Array.Empty<byte>());

    private readonly byte[] _data;
    private int _hash;

    private Binary(byte[] data) {
        this._data = data ?? throw new ArgumentNullException(nameof(data));
        this._hash = 0;
    }

    /// <summary>
    /// 创建一个拷贝
    /// </summary>
    /// <returns></returns>
    public Binary DeepCopy() {
        return new Binary((byte[])_data.Clone());
    }

    /// <summary>
    /// 获取指定下标字节
    /// </summary>
    /// <param name="index"></param>
    public byte this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data[index];
    }

    /// <summary>
    /// 字节数组长度
    /// </summary>
    public int Length => _data.Length;


    #region equals

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        return obj is Binary other && ArrayUtil.Equals(_data, other._data);
    }

    public override int GetHashCode() {
        int r = _hash;
        if (r == 0) {
            r = _hash = ArrayUtil.HashCode(_data);
        }
        return r;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_data)}: {DsonInternals.ToHexString(_data)}";
    }

    #region MyRegion

    /// <summary>
    /// 转换为只读的Span -- 可用于IO
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> AsSpan() => new ReadOnlySpan<byte>(_data);

    /// <summary>
    /// 转换为字节数组
    /// </summary>
    /// <returns></returns>
    public byte[] ToByteArray() => (byte[])_data.Clone();

    /// <summary>
    /// 转换为16进制字符串
    /// </summary>
    /// <returns></returns>
    public string ToHexString() => DsonInternals.ToHexString(_data);

    /// <summary>
    /// 获取底层的字节数组，一般业务不应该访问，否则可能破坏不可变约束
    /// </summary>
    public byte[] UnsafeBuffer => _data;

    public static Binary UnsafeWrap(byte[] value) {
        return new Binary(value);
    }

    public static Binary FromHexString(string hexString) {
        return new Binary(DsonInternals.DecodeHex(hexString));
    }

    public static Binary FromHexString(StringBuilder hexString) {
        return new Binary(DsonInternals.DecodeHex(hexString));
    }

    public static Binary CopyFrom(byte[] bytes) {
        return CopyFrom(bytes, 0, bytes.Length);
    }

    public static Binary CopyFrom(byte[] src, int offset, int size) {
        byte[] copy = ArrayUtil.CopyOf(src, offset, size);
        return new Binary(copy);
    }

    public void CopyTo(byte[] target, int offset) {
        Array.Copy(_data, 0, target, offset, _data.Length);
    }

    public void CopyTo(int selfOffset, byte[] target, int offset, int size) {
        Array.Copy(_data, selfOffset, target, offset, size);
    }

    #endregion
}
}