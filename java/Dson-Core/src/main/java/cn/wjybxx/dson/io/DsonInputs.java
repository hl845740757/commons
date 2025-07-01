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

import cn.wjybxx.base.io.ByteBufferUtils;
import cn.wjybxx.base.mutable.MutableInt;
import cn.wjybxx.dson.internal.CodedUtils;

import java.nio.charset.StandardCharsets;

/**
 * 核心包去除了对Protobuf的支持，如果期望使用protobuf和netty读取数据，可引入相应的扩展包。
 *
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonInputs {

    public static DsonInput newInstance(byte[] buffer) {
        return new ArrayDsonInput(buffer, 0, buffer.length);
    }

    public static DsonInput newInstance(byte[] buffer, int offset, int length) {
        return new ArrayDsonInput(buffer, offset, length);
    }

    static class ArrayDsonInput implements DsonInput {

        private final byte[] buffer;
        private final int rawOffset;
        private final int rawLimit;

        private int bufferPos;
        private int posLimit;
        private final MutableInt newPos = new MutableInt();

        ArrayDsonInput(byte[] buffer, int offset, int length) {
            ByteBufferUtils.checkBuffer(buffer, offset, length);
            this.buffer = buffer;
            this.rawOffset = offset;
            this.rawLimit = offset + length;

            this.bufferPos = offset;
            this.posLimit = offset + length;
        }

        // region check

        private int checkNewBufferPos(int newBufferPos) {
            if (newBufferPos < rawOffset || newBufferPos > posLimit) {
                throw new DsonIOException("BytesLimited, LimitPos: %d, position: %d, newPosition: %d"
                        .formatted(posLimit, bufferPos, newBufferPos));
            }
            return newBufferPos;
        }

        //endregion

        // region basic

        @Override
        public byte readRawByte() {
            checkNewBufferPos(bufferPos + 1);
            return buffer[bufferPos++];
        }

        @Override
        public int readFixed16() {
            try {
                int r = CodedUtils.readFixed16(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public int readUInt32() {
            try {
                int r = CodedUtils.readUInt32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public int readSInt32() {
            try {
                int r = CodedUtils.readSInt32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public int readFixed32() {
            try {
                int r = CodedUtils.readFixed32(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readUInt64() {
            try {
                long r = CodedUtils.readUInt64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readSInt64() {
            try {
                long r = CodedUtils.readSInt64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public long readFixed64() {
            try {
                long r = CodedUtils.readFixed64(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public float readFloat() {
            try {
                float r = CodedUtils.readFloat(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public float readVarFloat() {
            try {
                float r = CodedUtils.readVarFloat(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public double readDouble() {
            try {
                double r = CodedUtils.readDouble(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public double readVarDouble() {
            try {
                double r = CodedUtils.readVarDouble(buffer, bufferPos, newPos);
                bufferPos = checkNewBufferPos(newPos.getValue());
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public boolean readBool() {
            checkNewBufferPos(bufferPos + 1);
            return buffer[bufferPos++] != 0;
        }

        @Override
        public String readString() {
            try {
                int len = CodedUtils.readUInt32(buffer, bufferPos, newPos); // 字符串长度
                checkNewBufferPos(newPos.getValue() + len); // 先检查，避免构建无效字符串

                String r = new String(buffer, newPos.getValue(), len, StandardCharsets.UTF_8);
                bufferPos = newPos.getValue() + len;
                return r;
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public byte[] readRawBytes(int count) {
            checkNewBufferPos(bufferPos + count);
            byte[] bytes = new byte[count];
            System.arraycopy(buffer, bufferPos, bytes, 0, count);
            bufferPos += count;
            return bytes;
        }

        @Override
        public void skipRawBytes(int n) {
            if (n < 0) throw new IllegalArgumentException("n");
            if (n == 0) return;
            bufferPos = checkNewBufferPos(bufferPos + n);
        }
        // endregion

        //region sp

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
        public byte getByte(int pos) {
            ByteBufferUtils.checkBuffer(rawLimit - rawOffset, pos, 1);
            int bufferPos = rawOffset + pos;
            return buffer[bufferPos];
        }

        @Override
        public int getUInt32(int pos) {
            ByteBufferUtils.checkBuffer(rawLimit - rawOffset, pos, 4);
            int bufferPos = rawOffset + pos;
            return CodedUtils.readUInt32(buffer, bufferPos, newPos);
        }

        @Override
        public int pushLimit(int byteLimit) {
            if (byteLimit < 0) throw new IllegalArgumentException("byteLimit");
            int oldPosLimit = posLimit;
            int newPosLimit = bufferPos + byteLimit;

            // 不可超过原始限制
            ByteBufferUtils.checkBuffer(rawLimit, rawOffset, newPosLimit - rawOffset);
            posLimit = newPosLimit;
            return oldPosLimit;
        }

        @Override
        public void popLimit(int oldLimit) {
            // 不可超过原始限制
            ByteBufferUtils.checkBuffer(rawLimit, rawOffset, oldLimit - rawOffset);
            posLimit = oldLimit;
        }

        @Override
        public int getBytesUntilLimit() {
            return (posLimit - bufferPos);
        }

        @Override
        public boolean isAtEnd() {
            return bufferPos >= posLimit;
        }

        @Override
        public void readComplete(int safePosition) {

        }

        @Override
        public void close() {

        }
    }
}