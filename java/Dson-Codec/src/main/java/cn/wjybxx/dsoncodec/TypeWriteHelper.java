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

import cn.wjybxx.base.CollectionUtils;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;

/**
 * 用于实现不同Converter不同的数据
 * <p>
 * Q：为什么不绑定{@link TypeWritePolicy}？
 * A：因为我们允许Converter切换Options而保留其它数据 -- {@link DsonConverter#withOptions(ConverterOptions)}。
 *
 * @author wjybxx
 * date - 2024/10/15
 */
public final class TypeWriteHelper {

    /**
     * 这里包含用户配置的【非泛型类型之间】和【泛型原型之间】的关系，
     * 以及运行时测出的【非泛型类型之间】和【泛型原型之间】的关系，
     * 我们不缓存{@link TypeInfo}之间的结果，因为泛型类占比不会太高，且泛型参数个数通常不多，
     * 因此进行equals比较通常很高效。
     */
    private final ConcurrentMap<ClassPair, Boolean> cacheDic = new ConcurrentHashMap<>(100);

    public TypeWriteHelper(Map<ClassPair, Boolean> configs) {
        cacheDic.putAll(configs);
    }

    /**
     * 测试是否需要写入对象类型信息
     *
     * @param encoderType  运行时类型
     * @param declaredType 字段的声明类型
     * @return 是否可跳过
     */
    public boolean isOptimizable(TypeInfo encoderType, TypeInfo declaredType) {
        if (encoderType.equals(declaredType)) return true;
        if (declaredType.rawType == Object.class) return false;

        if (encoderType.hasGenericArgs()) {
            if (!declaredType.hasGenericArgs()) {
                return false;
            }
            // 都是泛型，如果泛型原型之间配置了可优化，则泛型参数相同时可优化
            ClassPair key = new ClassPair(encoderType.rawType, declaredType.rawType);
            Boolean r = cacheDic.get(key);
            if (r != null) {
                return r && CollectionUtils.sequenceEqual(declaredType.genericArgs, encoderType.genericArgs);
            }
            return false;
        } else {
            if (declaredType.hasGenericArgs()) {
                return false;
            }
            // 都不是泛型，如果配置了可优化，则可优化
            ClassPair key = new ClassPair(encoderType.rawType, declaredType.rawType);
            Boolean r = cacheDic.get(key);
            return r != null && r;
        }
    }

}
