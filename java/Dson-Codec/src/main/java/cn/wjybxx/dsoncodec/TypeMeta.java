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

import javax.annotation.concurrent.Immutable;
import java.util.List;
import java.util.Objects;

/**
 * 类型的元数据
 * 不使用Schema这样的东西，是因为Schema包含的信息太多，难以手动维护。
 * 另外，Schema是属于Codec的一部分，是低层次的数据，而TypeMeta是更高层的配置。
 * <p>
 * 1.1个Class可以有多个ClassName(即允许别名)，以支持简写；但一个ClassName只能映射到一个Class。
 * 2.在文档型编解码中，可读性是比较重要的，因此不要一味追求简短。
 *
 * @author wjybxx
 * date - 2023/7/29
 */
@Immutable
public final class TypeMeta {

    /** 关联的类型 */
    public final TypeInfo<?> typeInfo;
    /** 文本编码时的输出格式 */
    public final ObjectStyle style;
    /** 支持的类型名 */
    public final List<String> clsNames;

    private TypeMeta(TypeInfo<?> typeInfo, ObjectStyle style, List<String> clsNames) {
        if (clsNames.isEmpty()) throw new IllegalArgumentException("clsNames is empty");
        this.typeInfo = Objects.requireNonNull(typeInfo);
        this.style = Objects.requireNonNull(style);
        this.clsNames = List.copyOf(clsNames);
    }

    public String mainClsName() {
        return clsNames.get(0);
    }

    public static TypeMeta of(Class<?> clazz, ObjectStyle style) {
        return new TypeMeta(TypeInfo.of(clazz), style, List.of(clazz.getSimpleName()));
    }

    public static TypeMeta of(Class<?> clazz, ObjectStyle style, String clsName) {
        return new TypeMeta(TypeInfo.of(clazz), style, List.of(clsName));
    }

    public static TypeMeta of(Class<?> clazz, ObjectStyle style, String... clsNames) {
        return new TypeMeta(TypeInfo.of(clazz), style, List.of(clsNames));
    }

    public static TypeMeta of(Class<?> clazz, ObjectStyle style, List<String> clsNames) {
        return new TypeMeta(TypeInfo.of(clazz), style, List.copyOf(clsNames));
    }

    //

    public static TypeMeta of(TypeInfo<?> typeInfo, ObjectStyle style, String clsName) {
        return new TypeMeta(typeInfo, style, List.of(clsName));
    }

    public static TypeMeta of(TypeInfo<?> typeInfo, ObjectStyle style, String... clsNames) {
        return new TypeMeta(typeInfo, style, List.of(clsNames));
    }

    public static TypeMeta of(TypeInfo<?> typeInfo, ObjectStyle style, List<String> clsNames) {
        return new TypeMeta(typeInfo, style, List.copyOf(clsNames));
    }

    @Override
    public String toString() {
        return "TypeMeta{" +
                "type=" + typeInfo +
                ", style=" + style +
                ", classNames=" + clsNames +
                '}';
    }
}