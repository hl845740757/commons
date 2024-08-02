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

    static class ArrayOutput implements DsonOutput {

        private final byte[] buffer;
        private final int rawOffset;
        private final int rawLimit;

        private int bufferPos;
        private int bufferPosLimit;

        ArrayOutput(byte[] buffer, int offset, int length) {
            ByteBufferUtils.checkBuffer(buffer, offset, length);
            this.buffer = buffer;
            this.rawOffset = offset;
            this.rawLimit = offset + length;

            this.bufferPos = offset;
            this.bufferPosLimit = offset + length;
        }

        //region check

        private int CheckNewBufferPos(int newBufferPos) {
            if (newBufferPos < rawOffset || newBufferPos > bufferPosLimit) {
                throw new DsonIOException("BytesLimited, LimitPos: %d, position: %d, newPosition: %d"
                        .formatted(bufferPosLimit, bufferPos, newBufferPos));
            }
            return newBufferPos;
        }

        //endregion

        //region basic

        @Override
        public void writeRawByte(int value) {
            CheckNewBufferPos(bufferPos + 1);
            buffer[bufferPos++] = (byte) value;
        }

        @Override
        public void writeRawByte(byte value) {
            CheckNewBufferPos(bufferPos + 1);
            buffer[bufferPos++] = value;
        }

        @Override
        public void writeFixed16(int value) {
            try {
                int newPos = CodedUtils.writeFixed16(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeInt32(int value) {
            try {
                int newPos = CodedUtils.writeInt32(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeUint32(int value) {
            try {
                int newPos = CodedUtils.writeUint32(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeSint32(int value) {
            try {
                int newPos = CodedUtils.writeSint32(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeFixed32(int value) {
            try {
                int newPos = CodedUtils.writeFixed32(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeInt64(long value) {
            try {
                int newPos = CodedUtils.writeInt64(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeUint64(long value) {
            try {
                int newPos = CodedUtils.writeUint64(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeSint64(long value) {
            try {
                int newPos = CodedUtils.writeSint64(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeFixed64(long value) {
            try {
                int newPos = CodedUtils.writeFixed64(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeFloat(float value) {
            try {
                int newPos = CodedUtils.writeFloat(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeDouble(double value) {
            try {
                int newPos = CodedUtils.writeDouble(buffer, bufferPos, value);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeBool(boolean value) {
            try {
                int newPos = CodedUtils.writeUint32(buffer, bufferPos, value ? 1 : 0);
                bufferPos = CheckNewBufferPos(newPos);
            } catch (Exception e) {
                throw DsonIOException.wrap(e, "buffer overflow");
            }
        }

        @Override
        public void writeString(String value) {
            try {
                long maxByteCount = (value.length() * 3L);
                int maxByteCountVarIntSize = CodedUtils.computeRawVarInt64Size(maxByteCount);
                int minByteCountVarIntSize = CodedUtils.computeRawVarInt32Size(value.length());
                if (maxByteCountVarIntSize == minByteCountVarIntSize) {
                    // len占用的字节数是可提前确定的，因此无需额外的字节数计算，可直接编码
                    int newPos = bufferPos + minByteCountVarIntSize;
                    int byteCount = Utf8Util.utf8Encode(value, buffer, newPos, bufferPosLimit - newPos);
                    CodedUtils.writeUint32(buffer, bufferPos, byteCount);
                    bufferPos = CheckNewBufferPos(newPos + byteCount);
                } else {
                    // 注意，这里写的编码后的字节长度；而不是字符串长度 -- 提前计算UTF8的长度是很有用的方法
                    int byteCount = Utf8Util.utf8Length(value);
                    int newPos = CodedUtils.writeUint32(buffer, bufferPos, byteCount);
                    if (byteCount > 0) {
                        CheckNewBufferPos(newPos + byteCount);
                        Utf8Util.utf8Encode(value, buffer, newPos, bufferPosLimit - newPos);
                    }
                    bufferPos = (newPos + byteCount);
                }
            } catch (Exception e) {
                throw DsonIOException.wrap(e);
            }
        }

        @Override
        public void writeRawBytes(byte[] data, int offset, int length) {
            ByteBufferUtils.checkBuffer(data, offset, length);
            CheckNewBufferPos(bufferPos + length);

            System.arraycopy(data, offset, buffer, bufferPos, length);
            bufferPos += length;
        }
        // endregion

        // region sp

        @Override
        public int spaceLeft() {
            return bufferPosLimit - bufferPos;
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

        }
    }

}