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

import cn.wjybxx.dson.DsonArray;
import cn.wjybxx.dson.DsonObject;
import cn.wjybxx.dson.DsonValue;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.io.Reader;
import java.io.Writer;
import java.util.function.Supplier;

/**
 * 文档转换器
 * 将对象转换为文档或类文档结构，比如：Json/Bson/Yaml/Lua，主要用于持久化存储
 * <p>
 * 1.文档是指人类可读的文本结构，以可读性为主，与文档型数据库并不直接关联。
 * 2.在设计上，文档编解码器对效率和压缩比例的追求较低，API会考虑易用性，因此{@link DsonObjectReader}提供随机读的API。
 * 3.在设计上，是为了持久化的，为避免破坏用户数据，因此对于数组类型的数据结构不存储其类型信息，因此缺少静态类型信息的地方将无法准确解码。
 * 4.文档型结构不支持key为复杂Object，默认只支持{@link Integer}{@link Long}{@link String}，即基础的整数（转字符串）和直接字符串，
 * 5.对于枚举，为保持跨语言兼容性，枚举默认编码为{@code int32}或{@code string}，因此枚举不能被装箱，否则无法精确反序列化。
 * 至于是否支持枚举key和字符串之间的转换，与具体的实现有关。
 *
 * @author wjybxx
 * date 2023/4/4
 */
@ThreadSafe
public interface DsonConverter extends Converter {

    // region 文本编解码

    /**
     * 将一个对象写入源
     * 如果对象的运行时类型和{@link TypeInfo#rawType}一致，则会省去编码结果中的类型信息，
     *
     * @param declaredType 对象的类型信息
     * @param style        缩进格式
     */
    @Nonnull
    String writeAsDson(Object value, TypeInfo declaredType, ObjectStyle style);

    /**
     * 从数据源中读取一个对象
     *
     * @param source       数据源
     * @param declaredType 要读取的目标类型信息，部分实现支持投影
     */
    <T> T readFromDson(CharSequence source, TypeInfo declaredType, @Nullable Supplier<? extends T> factory);

    /**
     * 将一个对象写入指定writer
     * (默认不关闭writer)
     *
     * @param declaredType 对象的类型信息
     * @param writer       用于接收输出
     * @param style        缩进格式
     */
    void writeAsDson(Object value, TypeInfo declaredType, Writer writer, ObjectStyle style);

    /**
     * 从数据源中读取一个对象
     * （默认不关闭reader）
     *
     * @param source       用于支持大数据源
     * @param declaredType 要读取的目标类型信息，部分实现支持投影
     */
    <T> T readFromDson(Reader source, TypeInfo declaredType, @Nullable Supplier<? extends T> factory);


    /**
     * 将一个对象写为{@link DsonObject}或{@link DsonArray}
     *
     * @param value        顶层对象必须的容器对象，Object和数组
     * @param declaredType 对象的类型信息
     * @return {@link DsonObject}或{@link DsonArray}
     */
    DsonValue writeAsDsonValue(Object value, TypeInfo declaredType);

    /**
     * @param source       {@link DsonObject}或{@link DsonArray}
     * @param declaredType 要读取的目标类型信息
     */
    <T> T readFromDsonValue(DsonValue source, TypeInfo declaredType, @Nullable Supplier<? extends T> factory);

    /**
     * 将Dson源解码为DsonValue中间对象 -- 只读取一个顶层对象。
     * 外部可以保存该对象，以提高重复反序列化的效率。
     * (默认不关闭Reader)
     *
     * @param source 数据源
     * @return 中间对象
     */
    DsonValue readAsDsonValue(Reader source);

    /**
     * 将Dson源解码为DsonValue中间对象 -- 读取全部数据，header存储在外层容器(DsonArray)上。
     * 外部可以保存该对象，以提高重复反序列化的效率。
     * (默认不关闭Reader)
     *
     * @param source 数据源
     * @return 中间对象
     */
    DsonArray<String> readAsDsonCollection(Reader source);
    // endregion

    // region 快捷方法

    default String writeAsDson(Object value) { // 默认写入类型信息
        return writeAsDson(value, TypeInfo.OBJECT, (ObjectStyle) null);
    }

    default String writeAsDson(Object value, TypeInfo declaredType) {
        return writeAsDson(value, declaredType, (ObjectStyle) null);
    }

    default Object readFromDson(CharSequence source) {
        return readFromDson(source, TypeInfo.OBJECT, null);
    }

    default <T> T readFromDson(CharSequence source, TypeInfo declaredType) {
        return readFromDson(source, declaredType, null);
    }

    default void writeAsDson(Object value, TypeInfo declaredType, Writer writer) {
        writeAsDson(value, declaredType, writer, null);
    }

    default <T> T readFromDson(Reader source, TypeInfo declaredType) {
        return readFromDson(source, declaredType, null);
    }

    default <T> T readFromDsonValue(DsonValue source, TypeInfo declaredType) {
        return readFromDsonValue(source, declaredType, null);
    }

    // endregion

    DsonCodecRegistry codecRegistry();

    TypeMetaRegistry typeMetaRegistry();

    GenericHelper genericCodecHelper();

    ConverterOptions options();

    /** 在共享其它属性的情况，创建一个持有给定options的Converter */
    DsonConverter withOptions(ConverterOptions options);
}