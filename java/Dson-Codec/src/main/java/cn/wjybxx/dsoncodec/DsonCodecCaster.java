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

/**
 * 当要编码的对象类型不存在直接的Codec时，该方法用于转换编解码类型。
 * <p>
 * 1.该接口是从{@link DsonCodecRegistry}的逻辑中分离出来的，可更好的处理类型转换冲突。
 * 2.不必考虑效率问题，结果会被缓存。
 * 3.需保证线程安全性，因此建议实现为无状态的。
 *
 * @author wjybxx
 * date - 2024/9/28
 */
public interface DsonCodecCaster {

    /**
     * 转换编码类型
     * 1.可以向上转换，因为子类实例可以向上转型，但子类特殊数据将被丢弃。
     * 2.不可向下转换，因为超类实例不能向下转型。
     * 3.泛型参数的转换由用户处理，尽量转换为具有相同泛型参数的类型。
     * 4.转换后的类型必须存在对应的Codec和TypeMeta。
     * 5.集合类型通常转换为其对应的接口类型。
     *
     * @param type 要转换的类，泛型类的话是泛型定义类
     * @return 要转换的编码类型；null表示找不到合适的类型，将继续查找下一个
     */
    @Nullable
    TypeInfo castEncoderType(TypeInfo type);

    /**
     * 转换解码类型
     * 1.可以返回子类的Codec，如果子类和当前类数据兼容。
     * 2.不可向上转型，因为超类Codec创建的实例不能安全向下转型。
     * 3.泛型参数的转换由用户处理，尽量转换为具有相同泛型参数的类型。
     * 4.转换后的类型必须存在对应的Codec和TypeMeta。
     *
     * @param type 要转换的类，泛型类的话是泛型定义类
     * @return 要转换的解码类型；null表示找不到合适的类型，将继续查找下一个
     */
    @Nullable
    TypeInfo castDecoderType(TypeInfo type);
}