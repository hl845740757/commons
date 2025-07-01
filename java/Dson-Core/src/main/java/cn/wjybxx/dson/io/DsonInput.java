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
 * 通过{@link DsonInputs}的静态方法创建实例
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface DsonInput extends AutoCloseable {

    // region basic
    byte readRawByte();

    int readFixed16();

    //
    int readUInt32();

    int readSInt32();

    int readFixed32();

    //
    long readUInt64();

    long readSInt64();

    long readFixed64();

    //

    /** 该接口固定读取4字节 */
    float readFloat();

    /** 读取变长编码的float */
    float readVarFloat();

    /** 该接口固定读取8字节 */
    double readDouble();

    /** 读取变长编码的double */
    double readVarDouble();

    /** 该接口固定只读取一个字节；字节对应值不为0则表示true */
    boolean readBool();

    /** 该接口先读取一个uint32编码的长度，再读取相应字节数 */
    String readString();

    /** @param count 要读取的字节数 */
    byte[] readRawBytes(int count);

    /** @param n 要跳过的字节数 */
    void skipRawBytes(int n);

    // endregion

    // region advance

    /** 当前读索引位置 - 已读字节数 */
    int getPosition();

    /**
     * 设置读索引位置
     *
     * @throws IllegalArgumentException 如果设置到目标位置
     */
    void setPosition(int readerIndex);

    /**
     * 从指定位置读取一个byte
     * (不会修改当前索引)
     */
    byte getByte(int readerIndex);

    /**
     * 从指定位置读取一个UInt32类型值 -- 该接口预留以后读取subType
     * (不会修改当前索引)
     */
    int getUInt32(int readerIndex);

    /**
     * 限制接下来可读取的字节数(读取容器对象时调用)
     *
     * @param byteLimit 可用字节数
     * @return oldLimit 前一次设置的限制点；业务层避免使用
     */
    int pushLimit(int byteLimit);

    /**
     * 恢复字节数限制
     *
     * @param oldLimit 前一次设置的限制点
     */
    void popLimit(int oldLimit);

    /** @return 剩余可用的字节数 */
    int getBytesUntilLimit();

    /** @return 是否达到输入流的末端 */
    boolean isAtEnd();

    /**
     * 不需要再回滚到前面的位置
     * 由于我们存在SetPosition和随机读逻辑，为避免用户一直缓存数据，我们通过该接口告诉实现类，可以释放一部分缓存
     */
    void readComplete(int safePosition);

    // endregion

    @Override
    void close();
}