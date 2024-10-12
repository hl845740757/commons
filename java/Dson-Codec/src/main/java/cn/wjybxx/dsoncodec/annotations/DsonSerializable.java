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

package cn.wjybxx.dsoncodec.annotations;

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.base.EnumUtils;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonCodecRegistry;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.TypeInfo;

import java.lang.annotation.*;

/**
 * 用于标注一个类的对象可序列化为Dson文档结构
 *
 * <h3>注解处理器</h3>
 * 对于带有该注解的类：
 * 1. 如果是枚举，必须实现{@link EnumLite}并提供静态非private的{@code forNumber(int)}方法 - {@link EnumUtils#mapping(EnumLite[])}。
 * 2. 如果是普通类，必须提供<b>非私有无参构造方法</b>，或提供非私有的{@link DsonObjectReader}的单参构造方法。
 * 3. 对于普通类，所有托管给生成代码读的字段，必须提供setter或直接写权限。
 * 4. 对于普通类，所有托管给生成代码写的字段，必须提供getter或直接读权限。
 * 5. 如果字段通过{@link DsonProperty#readProxy()}指定了读代理，则不要求setter权限
 * 6. 如果字段通过{@link DsonProperty#writeProxy()}指定了写代理，则不要求getter权限
 * <p>
 * 普通类钩子方法：
 * 1. 如果类提供了静态的{@code newInstance(DsonObjectReader, TypeInfo)}方法，将自动调用 -- 优先级高于构造方法，可处理final字段。
 * 2. 如果类提供了非私有的{@code ClassName(DsonObjectReader, TypeInfo)}的构造方法，将自动调用 -- 该方法可用于final和忽略字段。
 * 3. 如果类提供了非私有的{@code afterDecode(ConverterOptions)}方法，且在options中启用，则自动调用 -- 通常用于数据转换，或构建缓存字段。
 * 4. 如果类提供了非私有的{@code beforeEncode(ConverterOptions)}方法，且在options中启用，则自动调用 -- 通常用于数据转换。
 * 5. 如果类提供了非私有的{@code readObject(DsonObjectReader)}方法，将自动调用 -- 该方法可用于忽略字段。
 * 6. 如果类提供了非私有的{@code writeObject(DsonObjectWriter)}方法，将自动调用 -- 该方法可用于final和忽略字段。
 * 7. 如果是通过{@link DsonCodecLinkerBean}配置的类，这些方法都需要转换为静态方法。
 *
 * <pre> {@code
 *      public void beforeEncode(ConverterOptions options){}
 *      public void writeObject(DsonObjectWriter writer){}
 *
 *      public static Bean newInstance(DsonObjectReader reader, TypeInfo encoderType){}
 *      public void readObject(DsonObjectReader reader){}
 *      public void afterDecode(ConverterOptions options){}
 *
 *      public void writeField1(DsonObjectWriter writer, String dsonName){}
 *      public void readField1(DsonObjectReader reader, String dsonName){}
 * }</pre>
 * ps：如果要更好的支持泛型，似乎应该将TypeInfo传入...
 *
 * <h3>泛型类</h3>
 * 1.Apt为泛型类生成Codec时，会固定生成一个{@code rawTypeInfo}字段。
 * 2.Apt为泛型类生成Codec时，会固定生成一个带有{@link TypeInfo}的构造函数。
 *
 * <h3>序列化的字段</h3>
 * 1. 默认序列化public和或包含public getter的字段；默认忽略{@code transient}修饰或{@link DsonIgnore}注解的字段。
 * 2. {@link DsonIgnore}也可以用于将{@code transient}字段加入编解码。
 * 3. 如果你提供了WriteObjet和ReadObject方法，你可以在其中写入忽略字段。
 *
 * <h3>多态字段</h3>
 * 1. 如果对象的运行时类型存在于{@link DsonCodecRegistry}中，则总是可以精确解析，因此不需要特殊处理。
 * 2. 否则用户需要指定实现类或读代理实现精确解析，请查看{@link DsonProperty}注解。
 *
 * <h3>final字段</h3>
 * 考虑到性能和安全性，final字段必须通过解析构造函数解析。
 *
 * <h3>读写忽略字段</h3>
 * 用户可以通过构造解码器和写对象方法实现。
 *
 * <h3>扩展</h3>
 * Q: 是否可以不使用注解，也能序列化？
 * A: 如果不使用注解，需要手动实现{@link DsonCodec}，并将其添加到注册表中。
 * （也可以加入到Scanner的扫描路径）
 *
 * <h3>一些建议</h3>
 * 1. 一般而言，建议使用该注解并遵循相关规范，由注解处理器生成的类负责解析，而不是手动实现{@link DsonCodec}。
 * 2. 并不建议都实现为javabean格式。
 * *
 * <h3>辅助类类名</h3>
 * 生成的辅助类为{@code XXXCodec}
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Retention(RetentionPolicy.RUNTIME)
@Target(ElementType.TYPE)
public @interface DsonSerializable {

    /**
     * 序列化时的类型名。
     * 1.第一个元素为默认名。
     * 2.支持多个以支持别名。
     * 3.APT不解析该字段 - Codec无需持有该信息。
     * <p>
     * Q：为什么是个数组？
     * A：这允许定义别名，以支持简写 -- 比如：'@Vector3' 可以简写为 '@V3'；而数字id通常不需要该支持。
     */
    String[] className() default {};

    /** 序列化时的缩进格式 */
    ObjectStyle style() default ObjectStyle.INDENT;

    /**
     * 单例对象获取实例的静态方法
     * 1.如果该属性不为空，则表示对象是单例；序列化时不写入字段，反序列化时直接返回单例。
     * 2.用户可以通过实现Codec实现单例和特殊多例的解析，这里只是对常见情况提供快捷方式。
     * 3.如果是由{@link DsonCodecLinkerBean}配置的类，则指向配置类中的静态方法。
     */
    String singleton() default "";

    /**
     * 声明不需要自动序列化的字段（自身或超类的）
     * 注意：skip仅仅表示不自动读，被跳过的字段仍然会占用字段编号和name！
     * <p>
     * 该属性主要用户处理继承得来的不能直接序列化的字段，以避免编译时检查不通过（无法自动序列化）。
     * 跳过这些字段后，你可以在解析构造方法、readObject、writeObject方法中处理。
     * <pre>
     * {@code
     *   skipFields = {
     *       size,
     *       HashMap.size, // 外部类或内部类无命名冲突时，可简写
     *       java.util.HashMap.size, // 外部类名冲突时使用全限定名
     *       java.util.Map.Entry.key // 内部类名冲突时使用全限定名
     *   }
     * }
     * </pre>
     * 1. 如果fieldName不包含点号，则认为是{@code FieldName}格式，即只通过字段名定位。
     * 2. 如果fieldName包含1个点号，则认为是{@code ClassSimpleName.FieldName}格式，即通过类的简单名定位。
     * 3. 如果fieldName包含多个点号，则认为是{@code ClassCanonicalName.FieldName}格式，即通过类的全限定名定位（内部类也使用点号分割）。
     */
    String[] skipFields() default {};

    /**
     * 为生成的文件添加的注解
     * 比如：可以添加{@link DsonCodecScanIgnore}以使得生成的代码在扫描Codec时被忽略。
     */
    Class<? extends Annotation>[] annotations() default {};

}