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

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.List;

/**
 * 类型元数据注册表
 * <p>
 * 注意：
 * 1. 必须保证同一个类在所有机器上的映射结果是相同的，这意味着你应该基于名字映射，而不能直接使用class对象的hash值。
 * 2. 一个类型{@link Class}的名字和唯一标识应尽量是稳定的，即同一个类的映射值在不同版本之间是相同的。
 * 3. id和类型之间应当是唯一映射的。
 * 4. 需要实现为线程安全的，建议实现为不可变对象（或事实不可变对象）
 *
 * @author wjybxx
 * date - 2023/4/26
 */
@ThreadSafe
public interface TypeMetaRegistry {

    /**
     * 通过完整的类型信息查询类型元数据
     */
    @Nullable
    TypeMeta ofType(TypeInfo<?> type);

    /**
     * 通过类型信息查询类型元数据。
     * 由于java在运行会擦除泛型信息，因此当声明类型和实际类型不一致时，我们只能根据运行时类型的原始类型查询。
     * 所以java生成的文本，C#可能无法解析(无法构造Type)；但C#生成的文本，Java可以解析。
     *
     * @param clazz 运行时类型
     */
    @Nullable
    TypeMeta ofClass(Class<?> clazz);

    /**
     * 通过字符串名字找到类型信息
     */
    TypeMeta ofName(String clsName);

    /**
     * 该方法的主要目的在于聚合多个Registry为单个Registry，以提高查询效率
     */
    List<TypeMeta> export();

}