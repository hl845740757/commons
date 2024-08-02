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

package cn.wjybxx.dsoncodec;

import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * 生成的代码会继承该类
 *
 * @author wjybxx
 * date 2023/4/4
 */
public abstract class AbstractDsonCodec<T> implements DsonCodec<T> {

    // region override
    // 重写以方便apt定位方法

    @Nonnull
    @Override
    public abstract Class<T> getEncoderClass();

    @Override
    public boolean autoStartEnd() {
        return true;
    }

    @Override
    public boolean isWriteAsArray() {
        return DsonConverterUtils.isEncodeAsArray(getEncoderClass());
    }
    // endregion

    // region write

    @Override
    public void writeObject(DsonObjectWriter writer, T instance, TypeInfo<?> typeInfo, ObjectStyle style) {
        if (writer.options().enableBeforeEncode) {
            beforeEncode(writer, instance, typeInfo, style);
        }
        writeFields(writer, instance, typeInfo, style);
    }

    /**
     * 用于执行用户的{@code beforeEncode}钩子方法。
     * 通常用于数据转换。
     */
    protected void beforeEncode(DsonObjectWriter writer, T instance, TypeInfo<?> typeInfo, ObjectStyle style) {

    }

    public abstract void writeFields(DsonObjectWriter writer, T instance, TypeInfo<?> typeInfo, ObjectStyle style);

    // endregion


    // region read

    @Override
    public T readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends T> factory) {
        final T instance = factory != null ? factory.get() : newInstance(reader, typeInfo);
        readFields(reader, instance, typeInfo);
        if (reader.options().enableAfterDecode) {
            afterDecode(reader, instance, typeInfo);
        }
        return instance;
    }

    /**
     * 创建一个对实例。
     * 1.如果是一个抽象类，应该抛出异常。
     * 2.该方法可解决final字段问题。
     *
     * @return 可以是子类实例
     */
    protected abstract T newInstance(DsonObjectReader reader, TypeInfo<?> typeInfo);

    /**
     * 从输入流中读取所有序列化的字段到指定实例上。
     *
     * @param instance 可以是子类实例
     */
    public abstract void readFields(DsonObjectReader reader, T instance, TypeInfo<?> typeInfo);

    /**
     * 用于执行用户的{@code afterDecode}钩子方法。
     * 通常用于数据转换。
     */
    protected void afterDecode(DsonObjectReader reader, T instance, TypeInfo<?> typeInfo) {

    }

    // endregion
}