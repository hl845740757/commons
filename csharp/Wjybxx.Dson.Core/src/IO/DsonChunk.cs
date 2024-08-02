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
using Wjybxx.Commons.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.IO
{
/// <summary>
/// Dson数据块
/// </summary>
public sealed class DsonChunk
{
    private readonly byte[] _buffer;
    /** 有效区域的起始偏移 */
    private int _offset;
    /** 有效区域的长度 */
    private int _length;
    /** 有效区域已使用长度 */
    private int _used;

    public DsonChunk(byte[] buffer)
        : this(buffer, 0, buffer.Length) {
    }

    public DsonChunk(byte[] buffer, int offset, int length) {
        ByteBufferUtil.CheckBuffer(buffer, offset, length);
        this._buffer = buffer;
        this._offset = offset;
        this._length = length;
    }

    public byte[] Buffer => _buffer;
    public int Offset => _offset;
    public int Length => _length;

    /// <summary>
    /// 重新设置块的有效载荷部分
    /// </summary>
    /// <param name="offset">有效部分的起始偏移量</param>
    /// <param name="length">有效部分的长度</param>
    public void SetOffsetLength(int offset, int length) {
        ByteBufferUtil.CheckBuffer(_buffer, offset, length);
        this._offset = offset;
        this._length = length;
    }

    /// <summary>
    /// 设置已使用的块大小
    /// </summary>
    public int Used {
        get => _used;
        set {
            if (value < 0 || value > _length) {
                throw new ArgumentException($"used {value}, length {_length}");
            }
            this._used = value;
        }
    }

    /// <summary>
    /// 转换为Binary实例
    /// </summary>
    /// <returns></returns>
    public Binary ToBinary() {
        return Binary.CopyFrom(_buffer, _offset, _length);
    }

    /// <summary>
    /// chunk的有效载荷部分
    /// </summary>
    /// <returns></returns>
    public byte[] Payload() {
        byte[] r = new byte[_length];
        Array.Copy(_buffer, _offset, r, 0, _length);
        return r;
    }

    /// <summary>
    /// chunk使用的载荷部分
    /// </summary>
    /// <returns></returns>
    public byte[] UsedPayload() {
        byte[] r = new byte[_used];
        Array.Copy(_buffer, _offset, r, 0, _used);
        return r;
    }

    /// <summary>
    /// 转为Span
    /// </summary>
    /// <returns></returns>
    public Span<byte> AsSpan() {
        return new Span<byte>(_buffer, _offset, _length);
    }

    /// <summary>
    /// 转为ArraySegment
    /// </summary>
    /// <returns></returns>
    public ArraySegment<byte> AsSegment() {
        return new ArraySegment<byte>(_buffer, _offset, _length);
    }
}
}