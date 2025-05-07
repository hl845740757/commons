/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dson.io;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.io.ByteBufferUtils;
import cn.wjybxx.base.pool.ArrayPool;
import cn.wjybxx.dson.internal.CodedUtils;
import cn.wjybxx.dson.internal.Utf8Util;

/**
 * 核心包去除了对Protobuf的支持，如果期望使用protobuf和netty读取数据，可引入相应的扩展包。
 *
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonOutputs {

    public static DsonOutput newInstance(byte[] buffer) {
        return new ArrayOutput(buffer, 0, buffer.length);
    }

    public static DsonOutput newInstance(byte[] buffer, int offset, int length) {
        return new ArrayOutput(buffer, offset, length);
    }

    /**
     * @param bufferPool   buffer池
     * @param initCapacity 初始空间
     * @param maxCapacity  最大空间
     */
    public static ArrayOutput newInstance(ArrayPool<byte[]> bufferPool, int initCapacity, int maxCapacity) {
        if (maxCapacity < initCapacity) {
            throw new IllegalArgumentException("initCapacity: %d, maxCapacity: %d".formatted(initCapacity, maxCapacity));
        }
        byte[] buffer = bufferPool.acquire(initCapacity);
        return new ArrayOutput(buffer, bufferPool, maxCapacity);
    }

    public static class ArrayOutput implements DsonOutput {

        private byte[] buffer;
        private final int rawOffset;
        private final int rawLimit; // 如果是池化的buffer，该值表示最大空间
        private ArrayPool<byte[]> bufferPool;

        private int bufferPos; // 当前写位置
        private int posLimit; // 当前限制位置-不可写入位置

        private ArrayOutput(byte[] buffer, int offset, int length) {
            ByteBufferUtils.checkBuffer(buffer, offset, length);
            this.buffer = buffer;
            this.rawOffset = offset;
            this.rawLimit = offset + length;

            this.bufferPos = offset;
            this.posLimit = offset + length;
        }

        private ArrayOutput(byte[] buffer, ArrayPool<byte[]> bufferPool, int maxCapacity) {
            this.buffer = buffer;
            this.bufferPool = bufferPool;
            this.rawOffset = 0;
            this.rawLimit = maxCapacity;

            this.bufferPos = 0;
            this.posLimit = buffer.length;
        }

        private void ensureCapacity(int required) {
            int minCapacity = bufferPos + required;
            if (minCapacity <= posLimit) {
                return;
            }
            if (bufferPool != null && minCapacity <= rawLimit) {
                int capacity = Math.clamp(buffer.length * 2L, minCapacity, rawLimit);
                // 注意：申请得到的buffer的长度可能大于capacity，因此可能大于maxCapacity
                byte[] newBuffer = bufferPool.acquire(capacity);
                System.arraycopy(buffer, 0, newBuffer, 0, bufferPos);
                // 勿调整顺序!
                buffer = newBuffer;
                posLimit = newBuffer.length;
                bufferPool.release(buffer);
                return;
            }
            throw new DsonIOException("BytesLimited, PosLimit: %d, position: %d, required: %d"
                    .formatted(posLimit, bufferPos, required));
        }

        //region basic

        @Override
        public void writeRawByte(int value) {
            ensureCapacity(1);
            buffer[bufferPos++] = (byte) value;
        }

        @Override
        public void writeRawByte(byte value) {
            ensureCapacity(1);
            buffer[bufferPos++] = value;
        }

        @Override
        public void writeFixed16(int value) {
            ensureCapacity(2);
            bufferPos = CodedUtils.writeFixed16(buffer, bufferPos, value);
        }

        @Override
        public void writeUInt32(int value) {
            ensureCapacity(CodedUtils.MAX_VAR_INT32_LENGTH);
            bufferPos = CodedUtils.writeUInt32(buffer, bufferPos, value);
        }

        @Override
        public void writeSInt32(int value) {
            ensureCapacity(CodedUtils.MAX_VAR_INT32_LENGTH);
            bufferPos = CodedUtils.writeSInt32(buffer, bufferPos, value);
        }

        @Override
        public void writeFixed32(int value) {
            ensureCapacity(4);
            bufferPos = CodedUtils.writeFixed32(buffer, bufferPos, value);
        }

        @Override
        public void writeUInt64(long value) {
            ensureCapacity(CodedUtils.MAX_VAR_INT64_LENGTH);
            bufferPos = CodedUtils.writeUInt64(buffer, bufferPos, value);
        }

        @Override
        public void writeSInt64(long value) {
            ensureCapacity(CodedUtils.MAX_VAR_INT64_LENGTH);
            bufferPos = CodedUtils.writeSInt64(buffer, bufferPos, value);
        }

        @Override
        public void writeFixed64(long value) {
            ensureCapacity(8);
            bufferPos = CodedUtils.writeFixed64(buffer, bufferPos, value);
        }

        @Override
        public void writeFloat(float value) {
            ensureCapacity(4);
            bufferPos = CodedUtils.writeFloat(buffer, bufferPos, value);
        }

        @Override
        public void writeVarFloat(float value) {
            ensureCapacity(CodedUtils.MAX_VAR_FLOAT32_LENGTH);
            bufferPos = CodedUtils.writeVarFloat(buffer, bufferPos, value);
        }

        @Override
        public void writeDouble(double value) {
            ensureCapacity(8);
            bufferPos = CodedUtils.writeDouble(buffer, bufferPos, value);
        }

        @Override
        public void writeVarDouble(double value) {
            ensureCapacity(CodedUtils.MAX_VAR_FLOAT64_LENGTH);
            bufferPos = CodedUtils.writeVarDouble(buffer, bufferPos, value);
        }

        @Override
        public void writeBool(boolean value) {
            ensureCapacity(1);
            bufferPos = CodedUtils.writeUInt32(buffer, bufferPos, value ? 1 : 0);
        }

        @Override
        public void writeString(String value) {
            if (ObjectUtils.isEmpty(value)) {
                ensureCapacity(1);
                buffer[bufferPos++] = 0;
                return;
            }
            // 提前计算UTF8字符串的长度会导致一定的开销 -- 但我们要保证数据不越界
            int byteCount = Utf8Util.utf8Length(value);
            ensureCapacity(CodedUtils.MAX_VAR_INT32_LENGTH + byteCount);
            bufferPos = CodedUtils.writeUInt32(buffer, bufferPos, byteCount);
            Utf8Util.utf8Encode(value, buffer, bufferPos, posLimit - bufferPos);
            bufferPos += byteCount;
        }

        @Override
        public void writeRawBytes(byte[] data, int offset, int length) {
            ByteBufferUtils.checkBuffer(data, offset, length);
            ensureCapacity(length);

            System.arraycopy(data, offset, buffer, bufferPos, length);
            bufferPos += length;
        }
        // endregion

        // region sp

        @Override
        public int spaceLeft() {
            return bufferPool != null ? rawLimit - bufferPos : posLimit - bufferPos;
        }

        @Override
        public int getPosition() {
            return bufferPos - rawOffset;
        }

        @Override
        public void setPosition(int value) {
            ByteBufferUtils.checkBuffer(rawLimit - rawOffset, value);
            bufferPos = rawOffset + value;
        }

        @Override
        public void setByte(int pos, byte value) {
            ByteBufferUtils.checkBuffer(rawLimit - rawOffset, pos, 1);
            int bufferPos = rawOffset + pos;
            buffer[bufferPos] = value;
        }

        @Override
        public void setFixedInt16(int pos, int value) {
            ByteBufferUtils.checkBuffer(rawLimit - rawOffset, pos, 4);
            int bufferPos = rawOffset + pos;
            ByteBufferUtils.setInt16LE(buffer, bufferPos, (short) value);
        }

        @Override
        public void setFixedInt32(int pos, int value) {
            ByteBufferUtils.checkBuffer(rawLimit - rawOffset, pos, 4);
            int bufferPos = rawOffset + pos;
            ByteBufferUtils.setInt32LE(buffer, bufferPos, value);
        }
        // endregion

        @Override
        public void flush() {

        }

        @Override
        public void close() {
            // 需要归还buffer
            byte[] buffer = this.buffer;
            ArrayPool<byte[]> bufferPool = this.bufferPool;
            this.buffer = null;
            this.bufferPool = null;
            //
            if (bufferPool != null) {
                bufferPool.release(buffer);
            }
        }

        public byte[] getBuffer() {
            return buffer;
        }
    }

}