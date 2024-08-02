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
using Wjybxx.Commons.IO;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.IO
{
/// <summary>
/// DsonInput工具类
/// </summary>
public static class DsonInputs
{
    /// <summary>
    /// 创建一个基于数组的DsonInput实例，默认buffer的整个区域为可读区间
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static IDsonInput NewInstance(byte[] buffer) {
        return new ArrayDsonInput(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 创建一个基于数组的DsonInput实例
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset">buffer起始偏移</param>
    /// <param name="length">buffer有效长度</param>
    /// <returns></returns>
    public static IDsonInput NewInstance(byte[] buffer, int offset, int length) {
        return new ArrayDsonInput(buffer, offset, length);
    }

    private class ArrayDsonInput : IDsonInput
    {
        private readonly byte[] _buffer;
        private readonly int _rawOffset;
        private readonly int _rawLimit;

        private int _bufferPos;
        private int _bufferPosLimit;

        internal ArrayDsonInput(byte[] buffer, int offset, int length) {
            ByteBufferUtil.CheckBuffer(buffer, offset, length);
            this._buffer = buffer;
            this._rawOffset = offset;
            this._rawLimit = offset + length;

            this._bufferPos = offset;
            this._bufferPosLimit = offset + length;
        }

        #region check

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CheckNewBufferPos(int newBufferPos) {
            if (newBufferPos < _rawOffset || newBufferPos > _bufferPosLimit) {
                throw new DsonIOException($"BytesLimited, LimitPos: {_bufferPosLimit}," +
                                          $" position: {_bufferPos}," +
                                          $" newPosition: {newBufferPos}");
            }
            return newBufferPos;
        }

        #endregion

        #region basic

        public byte ReadRawByte() {
            CheckNewBufferPos(_bufferPos + 1);
            return _buffer[_bufferPos++];
        }

        public int ReadFixed16() {
            try {
                int r = CodedUtil.ReadFixed16(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public int ReadInt32() {
            try {
                int r = CodedUtil.ReadInt32(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public int ReadUint32() {
            try {
                int r = CodedUtil.ReadUint32(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public int ReadSint32() {
            try {
                int r = CodedUtil.ReadSint32(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public int ReadFixed32() {
            try {
                int r = CodedUtil.ReadFixed32(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public long ReadInt64() {
            try {
                long r = CodedUtil.ReadInt64(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public long ReadUint64() {
            try {
                long r = CodedUtil.ReadUint64(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public long ReadSint64() {
            try {
                long r = CodedUtil.ReadSint64(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public long ReadFixed64() {
            try {
                long r = CodedUtil.ReadFixed64(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public float ReadFloat() {
            try {
                float r = CodedUtil.ReadFloat(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public double ReadDouble() {
            try {
                double r = CodedUtil.ReadDouble(_buffer, _bufferPos, out int newPos);
                _bufferPos = CheckNewBufferPos(newPos);
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public bool ReadBool() {
            CheckNewBufferPos(_bufferPos + 1);
            return _buffer[_bufferPos++] != 0; // c#字节是无符号，判断不等于0与Java更统一
        }

        public string ReadString() {
            try {
                int len = CodedUtil.ReadUint32(_buffer, _bufferPos, out int newPos); // 字符串长度
                CheckNewBufferPos(newPos + len); // 先检查，避免构建无效字符串

                string r = Encoding.UTF8.GetString(_buffer, newPos, len);
                _bufferPos = newPos + len;
                return r;
            }
            catch (Exception e) {
                throw DsonIOException.Wrap(e, "buffer overflow");
            }
        }

        public byte[] ReadRawBytes(int count) {
            CheckNewBufferPos(_bufferPos + count);
            byte[] bytes = new byte[count];
            Array.Copy(_buffer, _bufferPos, bytes, 0, count);
            _bufferPos += count;
            return bytes;
        }

        public void SkipRawBytes(int n) {
            if (n < 0) throw new ArgumentException(nameof(n));
            if (n == 0) return;
            _bufferPos = CheckNewBufferPos(_bufferPos + n);
        }

        #endregion

        #region Special

        public int Position {
            get => _bufferPos - _rawOffset;
            set {
                ByteBufferUtil.CheckBuffer(_rawLimit - _rawOffset, value);
                _bufferPos = _rawOffset + value;
            }
        }

        public byte GetByte(int pos) {
            ByteBufferUtil.CheckBuffer(_rawLimit - _rawOffset, pos, 1);
            int bufferPos = _rawOffset + pos;
            return _buffer[bufferPos];
        }

        public int GetFixed32(int pos) {
            ByteBufferUtil.CheckBuffer(_rawLimit - _rawOffset, pos, 4);
            int bufferPos = _rawOffset + pos;
            return ByteBufferUtil.GetInt32LE(_buffer, bufferPos);
        }

        public int PushLimit(int byteLimit) {
            if (byteLimit < 0) throw new ArgumentException(nameof(byteLimit));
            int oldPosLimit = _bufferPosLimit;
            int newPosLimit = _bufferPos + byteLimit;

            // 不可超过原始限制
            ByteBufferUtil.CheckBuffer(_rawLimit, _rawOffset, newPosLimit - _rawOffset);
            _bufferPosLimit = newPosLimit;
            return oldPosLimit;
        }

        public void PopLimit(int oldLimit) {
            // 不可超过原始限制
            ByteBufferUtil.CheckBuffer(_rawLimit, _rawOffset, oldLimit - _rawOffset);
            _bufferPosLimit = oldLimit;
        }

        public int GetBytesUntilLimit() => (_bufferPosLimit - _bufferPos);

        public bool IsAtEnd() => _bufferPos >= _bufferPosLimit;

        #endregion

        public void Dispose() {
        }
    }
}
}