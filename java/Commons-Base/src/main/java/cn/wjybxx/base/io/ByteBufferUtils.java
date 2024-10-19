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

package cn.wjybxx.base.io;

import cn.wjybxx.base.MathCommon;

import java.nio.Buffer;

/**
 * @author wjybxx
 * date - 2024/1/6
 */
@SuppressWarnings("unused")
public class ByteBufferUtils {

    /**
     * 检查buffer参数
     *
     * @param buffer 要检查的数组
     * @param offset 数据的起始索引
     * @param length 数据的长度
     */
    public static void checkBuffer(byte[] buffer, int offset, int length) {
        if (buffer == null) throw new NullPointerException("buffer");
        checkBuffer(buffer.length, offset, length);
    }

    /**
     * 检查buffer参数
     *
     * @param bufferLength buffer数组的长度
     * @param offset       数据的起始索引
     * @param length       数据的长度
     */
    public static void checkBuffer(int bufferLength, int offset, int length) {
        if ((offset | length | (bufferLength - (offset + length))) < 0) {
            throw new IllegalArgumentException(String.format("Array range is invalid. Buffer.length=%d, offset=%d, length=%d",
                    bufferLength, offset, length));
        }
    }

    /**
     * 检查buffer参数
     *
     * @param bufferLength buffer数组的长度
     * @param offset       数据的起始索引
     */
    public static void checkBuffer(int bufferLength, int offset) {
        if (offset < 0 || offset > bufferLength) {
            throw new IllegalArgumentException(String.format("Array range is invalid. Buffer.length=%d, offset=%d",
                    bufferLength, offset));
        }
    }

    /** JDK9+的代码跑在JDK8上的兼容问题 */
    public static void position(Buffer byteBuffer, int newOffset) {
        byteBuffer.position(newOffset);
    }

    // region byte

    public static byte getByte(byte[] buffer, int index) {
        return buffer[index];
    }

    public static void setByte(byte[] buffer, int index, byte value) {
        buffer[index] = (byte) value;
    }

    public static int getUnsignedByte(byte[] buffer, int index) {
        return buffer[index] & 0XFF;
    }

    public static void setByte(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
    }

    // endregion

    // region 大端编码

    /** 大端：向buffer中写入一个Int16 */
    public static void setInt16(byte[] buffer, int index, short value) {
        buffer[index] = (byte) (value >>> 8);
        buffer[index + 1] = (byte) value;
    }

    /** 大端：从buffer中读取一个Int16 */
    public static short getInt16(byte[] buffer, int index) {
        return (short) ((buffer[index] << 8)
                | (buffer[index + 1] & 0xff));
    }

    /** 大端：向buffer中写入一个Int32 */
    public static void setInt32(byte[] buffer, int index, int value) {
        buffer[index] = (byte) (value >>> 24);
        buffer[index + 1] = (byte) (value >>> 16);
        buffer[index + 2] = (byte) (value >>> 8);
        buffer[index + 3] = (byte) value;
    }

    /** 大端：从buffer中读取一个Int32 */
    public static int getInt32(byte[] buffer, int index) {
        return (((buffer[index] & 0xff) << 24)
                | ((buffer[index + 1] & 0xff) << 16)
                | ((buffer[index + 2] & 0xff) << 8)
                | ((buffer[index + 3] & 0xff)));
    }

    /** 大端：向buffer中写入一个Int32 */
    public static void setUInt32(byte[] buffer, int index, long value) {
        if (!MathCommon.isUInt32(value)) {
            throw new IllegalArgumentException("value: " + value);
        }
        buffer[index] = (byte) (value >>> 24);
        buffer[index + 1] = (byte) (value >>> 16);
        buffer[index + 2] = (byte) (value >>> 8);
        buffer[index + 3] = (byte) value;
    }

    /** 大端：从buffer中读取一个Int32 */
    public static long getUInt32(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 24)
                | ((buffer[index + 1] & 0xffL) << 16)
                | ((buffer[index + 2] & 0xffL) << 8)
                | ((buffer[index + 3] & 0xffL)));
    }

    /** 大端：向buffer中写入一个UInt48 */
    public static void setUInt48(byte[] buffer, int index, long value) {
        if (!MathCommon.isUInt48(value)) {
            throw new IllegalArgumentException("value: " + value);
        }
        buffer[index] = (byte) (value >>> 40);
        buffer[index + 1] = (byte) (value >>> 32);
        buffer[index + 2] = (byte) (value >>> 24);
        buffer[index + 3] = (byte) (value >>> 16);
        buffer[index + 4] = (byte) (value >>> 8);
        buffer[index + 5] = (byte) value;
    }

    /** 大端：从buffer中读取一个UInt48 */
    public static long getUInt48(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 40)
                | ((buffer[index + 1] & 0xffL) << 32)
                | ((buffer[index + 2] & 0xffL) << 24)
                | ((buffer[index + 3] & 0xffL) << 16)
                | ((buffer[index + 4] & 0xffL) << 8)
                | ((buffer[index + 5] & 0xffL)));
    }

    /** 大端：向buffer中写入一个Int64 */
    public static void setInt64(byte[] buffer, int index, long value) {
        buffer[index] = (byte) (value >>> 56);
        buffer[index + 1] = (byte) (value >>> 48);
        buffer[index + 2] = (byte) (value >>> 40);
        buffer[index + 3] = (byte) (value >>> 32);
        buffer[index + 4] = (byte) (value >>> 24);
        buffer[index + 5] = (byte) (value >>> 16);
        buffer[index + 6] = (byte) (value >>> 8);
        buffer[index + 7] = (byte) value;
    }

    /** 大端：从buffer中读取一个Int64 */
    public static long getInt64(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 56)
                | ((buffer[index + 1] & 0xffL) << 48)
                | ((buffer[index + 2] & 0xffL) << 40)
                | ((buffer[index + 3] & 0xffL) << 32)
                | ((buffer[index + 4] & 0xffL) << 24)
                | ((buffer[index + 5] & 0xffL) << 16)
                | ((buffer[index + 6] & 0xffL) << 8)
                | ((buffer[index + 7] & 0xffL)));
    }
    // endregion

    // region 小端编码

    /** 小端：向buffer中写入一个Int16 */
    public static void setInt16LE(byte[] buffer, int index, short value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
    }

    /** 小端：从buffer中读取一个Int16 */
    public static short getInt16LE(byte[] buffer, int index) {
        return (short) ((buffer[index] & 0xff)
                | (buffer[index + 1] << 8));
    }

    /** 小端：向buffer中写入一个Int32 */
    public static void setInt32LE(byte[] buffer, int index, int value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
    }

    /** 小端：从buffer中读取一个Int32 */
    public static int getInt32LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xff))
                | ((buffer[index + 1] & 0xff) << 8)
                | ((buffer[index + 2] & 0xff) << 16)
                | ((buffer[index + 3] & 0xff) << 24));
    }

    /** 小端：向buffer中写入一个Int32 */
    public static void setUInt32LE(byte[] buffer, int index, long value) {
        if (!MathCommon.isUInt32(value)) {
            throw new IllegalArgumentException("value: " + value);
        }
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
    }

    /** 小端：从buffer中读取一个Int32 */
    public static long getUInt32LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24));
    }

    /** 小端：向buffer中写入一个Int48 */
    public static void setUInt48LE(byte[] buffer, int index, long value) {
        if (!MathCommon.isUInt48(value)) {
            throw new IllegalArgumentException("value: " + value);
        }
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
        buffer[index + 4] = (byte) (value >>> 32);
        buffer[index + 5] = (byte) (value >>> 40);
    }

    /** 小端：从buffer中读取一个Int48 */
    public static long getUInt48LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40));
    }

    /** 小端：向buffer中写入一个Int64 */
    public static void setInt64LE(byte[] buffer, int index, long value) {
        buffer[index] = (byte) value;
        buffer[index + 1] = (byte) (value >>> 8);
        buffer[index + 2] = (byte) (value >>> 16);
        buffer[index + 3] = (byte) (value >>> 24);
        buffer[index + 4] = (byte) (value >>> 32);
        buffer[index + 5] = (byte) (value >>> 40);
        buffer[index + 6] = (byte) (value >>> 48);
        buffer[index + 7] = (byte) (value >>> 56);
    }

    /** 小端：从buffer中读取一个Int64 */
    public static long getInt64LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40)
                | ((buffer[index + 6] & 0xffL) << 48)
                | ((buffer[index + 7] & 0xffL) << 56));
    }

    // endregion

}