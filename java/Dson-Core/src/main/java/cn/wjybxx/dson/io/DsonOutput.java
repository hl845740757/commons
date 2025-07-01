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
 * 1.通过{@link DsonOutputs}的静态方法创建实例
 * 2.输出流中不需要记录值类型
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface DsonOutput extends AutoCloseable {

    // region basic
    void writeRawByte(byte value);

    void writeFixed16(int value);

    //
    void writeUInt32(int value);

    void writeSInt32(int value);

    void writeFixed32(int value);
    //

    void writeUInt64(long value);

    void writeSInt64(long value);

    void writeFixed64(long value);
    //

    /** 该接口固定写入4个字节 */
    void writeFloat(float value);

    /** 以变长编码格式写入float */
    void writeVarFloat(float value);

    /** 该接口固定写入8个字节 */
    void writeDouble(double value);

    /** 以变长编码格式写入double */
    void writeVarDouble(double value);

    /** 该接口固定写入一个字节 */
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

    /** 当前写索引位置 - 已写字节数 */
    int getPosition();

    /**
     * 设置写索引位置
     *
     * @throws IllegalArgumentException 如果设置到目标位置
     */
    void setPosition(final int writerIndex);

    /**
     * 在指定索引位置以Fixed16格式写入一个int值
     * (不会修改当前索引)
     */
    void setFixedInt16(final int writerIndex, int value);

    /**
     * 在指定索引位置以Fixed32格式写入一个int值
     * (不会修改当前索引)
     */
    void setFixedInt32(final int writerIndex, int value);

    /** 剩余可写空间 */
    int spaceLeft();

    /**
     * 不需要再回滚到前面的位置
     * 由于我们存在SetPosition和随机写逻辑，为避免用户一直缓存数据，我们通过该接口告诉实现类，可以释放一部分缓存
     */
    void writeComplete(int safePosition);

    /** 刷新缓冲区 */
    void flush();

    // endregion

    @Override
    void close();
}
