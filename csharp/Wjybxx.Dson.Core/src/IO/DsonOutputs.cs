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
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.IO;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.IO
{
/// <summary>
/// DsonOutput工具类
/// </summary>
public static class DsonOutputs
{
    public static IDsonOutput NewInstance(byte[] buffer) {
        return new ArrayOutput(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 创建一个基于数组的DsonOutput实例
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset">buffer起始偏移</param>
    /// <param name="length">buffer有效长度</param>
    /// <returns></returns>
    public static IDsonOutput NewInstance(byte[] buffer, int offset, int length) {
        return new ArrayOutput(buffer, offset, length);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bufferPool">buffer池</param>
    /// <param name="initCapacity">初始空间</param>
    /// <param name="maxCapacity">最大空间</param>
    /// <returns></returns>
    public static ArrayOutput NewInstance(IArrayPool<byte> bufferPool, int initCapacity, int maxCapacity) {
        if (maxCapacity < initCapacity) {
            throw new ArgumentException($"initCapacity: {initCapacity}, maxCapacity: {maxCapacity}");
        }
        return new ArrayOutput(bufferPool, initCapacity, maxCapacity);
    }

    /// <summary>
    /// 注意：是用户持有该对象的引用，因此不能内部隐式池化。
    /// </summary>
    public class ArrayOutput : IDsonOutput
    {
        private IArrayPool<byte>? _bufferPool;
        private byte[] _buffer;
        private readonly int _rawOffset;
        private readonly int _rawLimit; // 如果是池化的buffer，该值表示最大空间

        private int _bufferPos; // 当前写位置
        private int _posLimit; // 当前限制位置-不可写入位置

        internal ArrayOutput(byte[] buffer, int offset, int length) {
            ByteBufferUtil.CheckBuffer(buffer, offset, length);
            this._buffer = buffer;
            this._rawOffset = offset;
            this._rawLimit = offset + length;

            this._bufferPos = offset;
            this._posLimit = offset + length;
        }

        internal ArrayOutput(IArrayPool<byte> bufferPool, int initCapacity, int maxCapacity) {
            byte[] buffer = bufferPool.Acquire(initCapacity);
            _bufferPool = bufferPool;
            _buffer = buffer;
            _rawOffset = 0;
            _rawLimit = maxCapacity;

            _bufferPos = 0;
            _posLimit = buffer.Length;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="required">需要的字节数</param>
        private void EnsureCapacity(int required) {
            int minCapacity = _bufferPos + required;
            if (minCapacity <= _posLimit) {
                return;
            }
            if (_bufferPool != null && minCapacity <= _rawLimit) {
                int capacity = MathCommon.Clamp(_buffer.Length * 2L, minCapacity, _rawLimit);
                // 注意：申请得到的buffer的长度可能大于capacity，因此可能大于maxCapacity
                byte[] newBuffer = _bufferPool.Acquire(capacity);
                Array.Copy(_buffer, 0, newBuffer, 0, _bufferPos);
                //
                _bufferPool.Release(_buffer);
                _buffer = newBuffer;
                _posLimit = newBuffer.Length;
                return;
            }
            throw new DsonIOException($"BytesLimited, PosLimit: {_posLimit}, position: {_bufferPos}, required: {required}");
        }

        #region basic

        public void WriteRawByte(byte value) {
            EnsureCapacity(1);
            _buffer[_bufferPos++] = value;
        }

        public void WriteFixed16(int value) {
            EnsureCapacity(2);
            _bufferPos = CodedUtil.WriteFixed16(_buffer, _bufferPos, value);
        }

        public void WriteUInt32(int value) {
            EnsureCapacity(CodedUtil.MAX_VAR_INT32_LENGTH);
            _bufferPos = CodedUtil.WriteUInt32(_buffer, _bufferPos, value);
        }

        public void WriteSInt32(int value) {
            EnsureCapacity(CodedUtil.MAX_VAR_INT32_LENGTH);
            _bufferPos = CodedUtil.WriteSInt32(_buffer, _bufferPos, value);
        }

        public void WriteFixed32(int value) {
            EnsureCapacity(4);
            _bufferPos = CodedUtil.WriteFixed32(_buffer, _bufferPos, value);
        }

        public void WriteUInt64(long value) {
            EnsureCapacity(CodedUtil.MAX_VAR_INT64_LENGTH);
            _bufferPos = CodedUtil.WriteUInt64(_buffer, _bufferPos, value);
        }

        public void WriteSInt64(long value) {
            EnsureCapacity(CodedUtil.MAX_VAR_INT64_LENGTH);
            _bufferPos = CodedUtil.WriteSInt64(_buffer, _bufferPos, value);
        }

        public void WriteFixed64(long value) {
            EnsureCapacity(8);
            _bufferPos = CodedUtil.WriteFixed64(_buffer, _bufferPos, value);
        }

        public void WriteFloat(float value) {
            EnsureCapacity(4);
            _bufferPos = CodedUtil.WriteFloat(_buffer, _bufferPos, value);
        }

        public void WriteVarFloat(float value) {
            EnsureCapacity(CodedUtil.MAX_VAR_FLOAT32_LENGTH);
            _bufferPos = CodedUtil.WriteVarFloat(_buffer, _bufferPos, value);
        }

        public void WriteDouble(double value) {
            EnsureCapacity(8);
            _bufferPos = CodedUtil.WriteDouble(_buffer, _bufferPos, value);
        }

        public void WriteVarDouble(double value) {
            EnsureCapacity(CodedUtil.MAX_VAR_FLOAT64_LENGTH);
            _bufferPos = CodedUtil.WriteVarDouble(_buffer, _bufferPos, value);
        }

        public void WriteBool(bool value) {
            EnsureCapacity(1);
            _bufferPos = CodedUtil.WriteUInt32(_buffer, _bufferPos, value ? 1 : 0);
        }

        public void WriteString(string value) {
            if (string.IsNullOrEmpty(value)) {
                EnsureCapacity(1);
                _buffer[_bufferPos++] = 0;
                return;
            }
            // 提前计算UTF8字符串的长度会导致一定的开销 -- 但我们要保证数据不越界
            int byteCount = Encoding.UTF8.GetByteCount(value);
            EnsureCapacity(CodedUtil.MAX_VAR_INT32_LENGTH + byteCount);
            _bufferPos = CodedUtil.WriteUInt32(_buffer, _bufferPos, byteCount);
            Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _bufferPos);
            _bufferPos += byteCount;
        }

        public void WriteRawBytes(byte[] data, int offset, int length) {
            ByteBufferUtil.CheckBuffer(data, offset, length);
            EnsureCapacity(length);

            Array.Copy(data, offset, _buffer, _bufferPos, length);
            _bufferPos += length;
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

        public void SetByte(int pos, byte value) {
            ByteBufferUtil.CheckBuffer(_rawLimit - _rawOffset, pos, 1);
            int bufferPos = _rawOffset + pos;
            _buffer[bufferPos] = value;
        }

        public void SetFixed16(int pos, int value) {
            ByteBufferUtil.CheckBuffer(_rawLimit - _rawOffset, pos, 2);
            int bufferPos = _rawOffset + pos;
            CodedUtil.WriteFixed16(_buffer, bufferPos, (short)value);
        }

        public void SetFixed32(int pos, int value) {
            ByteBufferUtil.CheckBuffer(_rawLimit - _rawOffset, pos, 4);
            int bufferPos = _rawOffset + pos;
            CodedUtil.WriteFixed32(_buffer, bufferPos, value);
        }

        public int SpaceLeft => _bufferPool != null
            ? -_rawLimit - _bufferPos
            : _posLimit - _bufferPos;

        public void WriteComplete(int safePosition) {

        }

        #endregion

        public void Flush() {
        }

        public void Dispose() {
            IArrayPool<byte> bufferPool = this._bufferPool;
            byte[] buffer = this._buffer;
            // 需要归还buffer
            this._buffer = null!;
            this._bufferPool = null;
            if (bufferPool != null) {
                bufferPool.Release(buffer);
            }
        }

        /// <summary>
        /// 关联的Buffer池
        /// </summary>
        public IArrayPool<byte>? BufferPool => _bufferPool;
        /// <summary>
        /// 获取底层的buffer，慎重使用
        /// </summary>
        public byte[] Buffer => _buffer;
    }
}
}