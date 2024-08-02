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

/**
 * 接口约定以小端编码数字
 * 通过{@link DsonOutputs}的静态方法创建实例
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface DsonOutput extends AutoCloseable {

    // region basic
    void writeRawByte(byte value);

    default void writeRawByte(int value) {
        writeRawByte((byte) value);
    }

    void writeFixed16(int value);

    //
    void writeInt32(int value);

    void writeUint32(int value);

    void writeSint32(int value);

    void writeFixed32(int value);
    //

    void writeInt64(long value);

    void writeUint64(long value);

    void writeSint64(long value);

    void writeFixed64(long value);
    //

    /** 该接口固定写入4个字节 */
    void writeFloat(float value);

    /** 该接口固定写入8个字节 */
    void writeDouble(double value);

    /** 该接口固定写入1个字节 */
    void writeBool(boolean value);

    /** 该接口先以Uint32格式写入String以UTF8编码后的字节长度，再写入String以UTF8编码后的内容 */
    void writeString(String value);

    /** 仅写入内容，不会写入数组的长度 */
    default void writeRawBytes(byte[] data) {
        writeRawBytes(data, 0, data.length);
    }

    /** 仅写入内容，不会写入数组的长度 */
    void writeRawBytes(byte[] data, int offset, int length);

    // endregion

    // region advance

    /** 剩余可写空间 */
    int spaceLeft();

    /** 当前写索引位置 - 已写字节数 */
    int getPosition();

    /**
     * 设置写索引位置
     *
     * @throws IllegalArgumentException 如果设置到目标位置
     */
    void setPosition(final int writerIndex);

    /** 在指定索引位置写入一个byte */
    void setByte(final int writerIndex, byte value);

    /**
     * 在指定索引位置以Fixed16格式写入一个int值。
     * 1.该方法可能有较大的开销，不宜频繁使用
     * 2.相比先{@link #setPosition(int)}再{@link #writeFixed16(int)}的方式，该接口更容易优化实现。
     */
    default void setFixedInt16(final int writerIndex, int value) {
        int oldPosition = getPosition();
        setPosition(writerIndex);
        writeFixed16(value);
        setPosition(oldPosition);
    }

    /**
     * 在指定索引位置以Fixed32格式写入一个int值。
     * 1.该方法可能有较大的开销，不宜频繁使用
     * 2.相比先{@link #setPosition(int)}再{@link #writeFixed32(int)}的方式，该接口更容易优化实现。
     */
    default void setFixedInt32(final int writerIndex, int value) {
        int oldPosition = getPosition();
        setPosition(writerIndex);
        writeFixed32(value);
        setPosition(oldPosition);
    }

    /** 刷新缓冲区 */
    void flush();

    // endregion

    @Override
    void close();
}
