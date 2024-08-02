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

namespace Wjybxx.Dson.IO
{
/// <summary>
/// 1. 数字采用小端编码
/// 2. String采用UTF8编码
/// </summary>
public interface IDsonOutput : IDisposable
{
    #region Basic

    void WriteRawByte(byte value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteRawByte(int value) {
        WriteRawByte((byte)value);
    }

    void WriteFixed16(int value);

    void WriteInt32(int value);

    void WriteUint32(int value);

    void WriteSint32(int value);

    void WriteFixed32(int value);

    void WriteInt64(long value);

    void WriteUint64(long value);

    void WriteSint64(long value);

    void WriteFixed64(long value);

    /// <summary>
    /// 该接口固定写入4个字节
    /// </summary>
    /// <param name="value"></param>
    void WriteFloat(float value);

    /// <summary>
    /// 该接口固定写入8个字节
    /// </summary>
    /// <param name="value"></param>
    void WriteDouble(double value);

    /// <summary>
    /// 该接口固定写入一个字节
    /// </summary>
    /// <param name="value"></param>
    void WriteBool(bool value);

    /// <summary>
    /// 该接口先以Uint32格式写入String以UTF8编码后的字节长度，再写入String以UTF8编码后的内容
    /// </summary>
    /// <param name="value">要写入的字符串</param>
    void WriteString(string value);

    /// <summary>
    /// 仅写入内容，不会写入数组的长度
    /// </summary>
    /// <param name="data">要写入的字节数组</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteRawBytes(byte[] data) {
        WriteRawBytes(data, 0, data.Length);
    }

    /// <summary>
    /// 仅写入内容，不会写入数组的长度
    /// </summary>
    void WriteRawBytes(byte[] data, int offset, int length);

    #endregion

    #region Special

    /// <summary>
    /// 剩余可写空间
    /// </summary>
    int SpaceLeft { get; }

    /// <summary>
    /// 当前写索引(也等于写入的字节数)
    /// </summary>
    int Position { get; set; }

    /// <summary>
    /// 在指定位置写入一个byte
    /// </summary>
    /// <param name="pos">写索引</param>
    /// <param name="value">value</param>
    void SetByte(int pos, byte value);

    /// <summary>
    /// 在指定索引位置以Fixed16格式写入一个int值
    /// </summary>
    /// <param name="pos">写索引</param>
    /// <param name="value">value</param>
    void SetFixed16(int pos, int value);

    /// <summary>
    /// 在指定索引位置以Fixed32格式写入一个int值
    /// 该方法可能有较大的开销，不宜频繁使用
    /// </summary>
    /// <param name="pos">写索引</param>
    /// <param name="value">value</param>
    void SetFixed32(int pos, int value);

    void Flush();

    #endregion
}
}