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

import java.util.*;

/**
 * 类型id的写入策略
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public enum ClassIdPolicy {

    /**
     * 当对象的运行时类型和声明类型相同时不写入类型信息
     * 通常我们的字段类型定义是明确的，因此可以省去大量不必要的类型信息。
     * 如果仅用于java或其它静态类型语言，建议使用该模式。
     */
    OPTIMIZED,

    /**
     * 总是写入对象的类型信息，无论运行时类型与声明类型是否相同
     * 这种方式有更好的兼容性，对跨语言友好，因为目标语言可能没有泛型，或没有注解处理器生成辅助代码等；
     * 但这种方式会大幅增加序列化后的大小，需要慎重考虑。
     */
    ALWAYS,

    /**
     * 总是不写入对象的类型信息，无论运行时类型与声明类型是否相同
     */
    NONE;

    public boolean test(Class<?> declaredType, Class<?> encoderType) {
        if (this == ClassIdPolicy.OPTIMIZED) {
            if (encoderType == declaredType) {
                return false; // 运行时类型和声明类型一致，不写入
            }
            if (declaredType == Object.class) {
                return true;
            }
            // 默认解码类型的父类型无需写入，List/Map/Set
            if (encoderType == ArrayList.class
                    && Collection.class.isAssignableFrom(declaredType)) {
                return false;
            }
            if ((encoderType == HashMap.class || encoderType == LinkedHashMap.class)
                    && Map.class.isAssignableFrom(declaredType)) {
                return false;
            }
            if ((encoderType == HashSet.class || encoderType == LinkedHashSet.class)
                    && Set.class.isAssignableFrom(declaredType)) {
                return false;
            }
            return true;
        }
        return this == ClassIdPolicy.ALWAYS;
    }

}