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
using Wjybxx.Commons;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 二进制数据
/// </summary>
public struct Binary : IEquatable<Binary>
{
    private readonly byte[] _data;
    private int _hash;

    private Binary(byte[] data) {
        this._data = data ?? throw new ArgumentNullException(nameof(data));
        this._hash = 0;
    }

    /** 创建一个拷贝 */
    public readonly Binary DeepCopy() {
        return new Binary((byte[])_data.Clone());
    }

    /// <summary>
    /// 获取指定下标字节
    /// </summary>
    /// <param name="index"></param>
    public byte this[int index] => _data[index];

    /// <summary>
    /// 字节数组长度
    /// </summary>
    public int Length => _data.Length;

    /// <summary>
    /// default构造的情况下_data为null
    /// </summary>
    public bool IsNull => _data == null;

    #region equals

    public bool Equals(Binary other) {
        if (_data == other._data) {
            return true;
        }
        if (IsNull || other.IsNull) {
            return false;
        }
        // 可以直接比较内存bit
        ReadOnlySpan<byte> first = _data;
        ReadOnlySpan<byte> second = other._data;
        return first.SequenceEqual(second);
    }

    public override bool Equals(object? obj) {
        return obj is Binary other && Equals(other);
    }

    public override int GetHashCode() {
        int r = _hash;
        if (r == 0) {
            r = this._hash = HashCode(_data);
        }
        return r;
    }

    private static int HashCode(byte[] data) {
        int r = 1;
        for (int i = 0; i < data.Length; i++) {
            r = r * 31 + data[i];
        }
        return r;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_data)}: {CommonsLang3.ToHexString(_data)}";
    }

    #region MyRegion

    /// <summary>
    /// 转换为字节数组
    /// </summary>
    /// <returns></returns>
    public byte[] ToByteArray() => (byte[])_data.Clone();

    /// <summary>
    /// 转换为16进制字符串
    /// </summary>
    /// <returns></returns>
    public string ToHexString() => CommonsLang3.ToHexString(_data);

    /// <summary>
    /// 获取底层的字节数组，一般业务不应该访问，否则可能破坏不可变约束
    /// </summary>
    public byte[] UnsafeBuffer => _data;

    public static Binary UnsafeWrap(byte[] value) {
        return new Binary(value);
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