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

import java.lang.reflect.TypeVariable;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date - 2024/9/27
 */
public class GenericCodecHelper implements IGenericCodecHelper {

    private final ConcurrentHashMap<ClassPair, Boolean> inheritableResultCache = new ConcurrentHashMap<>();

    @Override
    public final boolean canInheritTypeArgs(Class<?> runtimeType, Class<?> declaredType) {
        if (runtimeType == declaredType) return true;
        if (declaredType == Object.class) return false;

        ClassPair classPair = new ClassPair(runtimeType, declaredType);
        Boolean r = inheritableResultCache.get(classPair);
        if (r != null) {
            return r;
        }
        if (runtimeType.isArray()) {
            // 数组只可以和数组匹配，不开放给用户扩展
            if (declaredType.isArray()) {
                Class<?> runtimeElementType = DsonConverterUtils.getRootComponentType(runtimeType);
                Class<?> declaredElementType = DsonConverterUtils.getRootComponentType(declaredType);
                r = canInheritTypeArgs0(runtimeElementType, declaredElementType);
            } else {
                r = false;
            }
        } else {
            r = canInheritTypeArgs0(runtimeType, declaredType);
        }
        inheritableResultCache.put(classPair, r);
        return r;
    }

    /** 如果有特殊的投影映射也可以 */
    protected boolean canInheritTypeArgs0(Class<?> runtimeType, Class<?> declaredType) {
        if (!declaredType.isAssignableFrom(runtimeType)) {
            return false;
        }
        return sequenceTypeVarNameEquals(runtimeType, declaredType);
    }

    public static boolean sequenceTypeVarNameEquals(Class<?> runtimeType, Class<?> declaredType) {
        TypeVariable<? extends Class<?>>[] thisTypeParameters = runtimeType.getTypeParameters();
        TypeVariable<? extends Class<?>>[] declaredTypeParameters = declaredType.getTypeParameters();
        if (thisTypeParameters.length != declaredTypeParameters.length) {
            return false;
        }
        // 子类声明相同的泛型变量是声明了新的变量，因此无法直接Equals比较数组元素，我们只能简单的比较名字
        for (int i = 0; i < thisTypeParameters.length; i++) {
            TypeVariable<? extends Class<?>> thisTypeParameter = thisTypeParameters[i];
            TypeVariable<? extends Class<?>> declaredTypeParameter = declaredTypeParameters[i];
            if (!thisTypeParameter.getName().equals(declaredTypeParameter.getName())) {
                return false;
            }
        }
        return true;
    }
}
