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

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * 主要用于为引入的外部库中的Bean自动生成Codec。
 * 1. 默认序列化【目标Bean】的所有可序列化字段，只特殊处理【LinkerBean】中声明的字段。
 * 2. 字段之间通过名字匹配，字段的类型需声明为定义字段的类，以方便未来解决冲突。
 * 3. 【LinkerBean】字段上的{@link DsonProperty}、{@link DsonIgnore}将被映射到【目标Bean】。
 * 4. 字段的读写代理将映射到【LinkerBean】中的静态方法。
 * 5. {@link DsonSerializable}中提到的钩子方法也将映射到【LinkerBean】中的静态方法。
 * <pre>{@code
 *  class MyBeanLinker {
 *      MyBean field1; // 表示OuterClass的field1字段
 *      MyBean field2;
 *
 *      public static void beforeEncode(MyBean instance, ConverterOptions options){}
 *      public static void writeObject(MyBean instance, DsonObjectWriter writer){}
 *
 *      public static Bean newInstance(DsonObjectReader reader, TypeInfo typeInfo){}
 *      public static void readObject(MyBean instance, DsonObjectReader reader){}
 *      public static void afterDecode(MyBean instance, ConverterOptions options){}
 *
 *      public static void writeField1(MyBean instance, DsonObjectWriter writer, String dsonName){}
 *      public static void readField1(MyBean instance, DsonObjectReader reader, String dsonName){}
 *  }
 * }</pre>
 * <p>
 * Q：与{@link DsonCodecLinker}的区别？
 * A：该注解用于支持复杂的Codec配置，一个Bean描述一个Bean，而且支持复杂的字段读写代理和序列化钩子。
 *
 * @author wjybxx
 * date - 2024/4/11
 */
@Target(ElementType.TYPE)
@Retention(RetentionPolicy.SOURCE)
public @interface DsonCodecLinkerBean {

    /** 映射的类 */
    Class<?> value();

    /** 生成的codec的输出目录 -- 默认值为当前配置类的包名！ */
    String outputPackage() default "";

    /** 为类型附加的信息 */
    DsonSerializable props() default @DsonSerializable;

}