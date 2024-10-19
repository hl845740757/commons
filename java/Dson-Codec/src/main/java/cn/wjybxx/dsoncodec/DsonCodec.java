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
import cn.wjybxx.dsoncodec.annotations.DsonSerializable;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * 自定义POJO对象编解码实现接口，该接口与{@link DsonCodecImpl}协同工作，为典型的桥接模式。
 * <p>
 * 实体类序列化工具类，每一个{@link DsonCodec}只负责一个固定类型的解析。
 * 生成的代码会实现该接口，用户手动实现编解码时也实现该接口。
 *
 * <br>-------------------------------什么时候手写实现？-----------------------<br>
 * 1. 一般而言，建议使用注解{@link DsonSerializable}，并遵循相关规范，由注解处理器生成的类负责解析，而不是手写实现{@link DsonCodec}。
 * 一旦手写实现，必须持久的进行维护。
 * 2. 如果对象存在复杂的构造过程的时候，考虑手动实现。
 * 3. 对象存在final字段时，考虑手动实现（最好只负责final字段解析）
 * 4. 单例和池化对象，如果想解析时避免创建新的对象，可以自实现Codec处理。
 *
 * <br>-------------------------------实现时要注意什么？----------------------<br>
 * 1. 必须保证线程安全，最好是无状态的。
 * 2. 最好实现为目标类的静态内部类，且最好是private级别，不要暴露给外层。
 * 3. 需提供无参构造方法(可以private，否则不会被扫描到) - 反射创建对象。
 *
 * <br>-------------------------------泛型相关----------------------<br>
 * 1. 泛型codec的encoderType是根据对象的[真实类型]和[声明类型]动态生成的。
 * 2  泛型codec必须包含接收{@link TypeInfo}的构造函数，还可以接收一个Factory。
 * <p>
 * {@code Struct(TypeInfo encoderType)}
 * {@code Struct(TypeInfo encoderType, Supplier factory)}
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface DsonCodec<T> {

    /**
     * 获取负责编解码的类对象
     * 如果可以，在数据兼容的情况下，尽量将泛型'T'声明为抽象类或接口，然后通过动态的TypeInfo来绑定编解码类型。
     * EncoderType必须存在对应的{@link TypeMeta}
     */
    @Nonnull
    TypeInfo getEncoderType();

    /**
     * 将对象写入输出流。
     * 将对象及其所有超类定义的所有要序列化的字段写入输出流。
     * <p>
     * 注意：
     * 1.name在外部已写入，因此基础类型写入value时name传null。
     * 2.declaredType只影响外层类型信息是否写入，而不应向下传递。以字典为例，KV是否需要写入类型信息，取决于encoderType中的信息。
     *
     * @param declaredType 声明类型，用于判断是否写入类型信息
     * @param style        外部期望的输出格式
     */
    void writeObject(DsonObjectWriter writer, T inst, TypeInfo declaredType, ObjectStyle style);

    /**
     * 从输入流中解析指定对象。
     * 它应该创建对象，并反序列化该类及其所有超类定义的所有要序列化的字段。
     * <p>
     * 注意：name在外部已读取，因此基础类型读取value时name传null。
     *
     * @param factory 实例工厂
     */
    T readObject(DsonObjectReader reader, Supplier<? extends T> factory);

    /**
     * 该方法用于告知{@link DsonCodecImpl}是否自动调用以下方法:
     * {@link DsonObjectWriter#writeStartObject(String, ObjectStyle, TypeInfo, TypeInfo)} ()}
     * {@link DsonObjectWriter#writeEndObject()}
     * {@link DsonObjectReader#readStartObject(String)}
     * {@link DsonObjectReader#readEndObject()}
     * <p>
     * Q：禁用该属性有什么用？
     * A: 对于写；你可以将当前转换为另一个对象，然后再使用对应的codec进行编码；对于读：你可以使用另一个codec来解码当前二进制对象。
     * 即：关闭该属性可以实现读替换(readReplace)和写替换(writeReplace)功能。
     * 另外，还可以自行决定是写为Array还是Object。
     */
    default boolean autoStartEnd() {
        return true;
    }

    /**
     * 当前对象是否按照数组格式编码
     * 1.默认情况下，Map是被看做普通的数组的
     * 2.该属性只有{@link #autoStartEnd()} 为true的时候有效。
     * 3.实现类无需处理缓存，该方法只会调用一次.
     */
    default boolean isWriteAsArray() {
        return DsonConverterUtils.isEncodeAsArray(getEncoderType().rawType);
    }
}