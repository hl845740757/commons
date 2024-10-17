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
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/10/11
 */
@SuppressWarnings("rawtypes")
public final class GenericCodecInfo {

    public final TypeInfo typeInfo; // 泛型原型类
    public final Class<? extends DsonCodec> codecType; // 对应的Codec
    public final Supplier<?> factory; // 工厂

    private GenericCodecInfo(TypeInfo typeInfo, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        this.typeInfo = typeInfo;
        this.codecType = codecType;
        this.factory = factory;
    }

    /**
     * @param typeInfo  泛型擦除后的类型信息
     * @param codecType 编解码器的类型
     */
    public static GenericCodecInfo create(TypeInfo typeInfo, Class<? extends DsonCodec> codecType) {
        return createImpl(typeInfo, codecType, null);
    }

    /**
     * 通过实现类创建一个Item
     *
     * @param typeInfo  泛型擦除后的类型信息
     * @param codecType 编解码器的类型
     * @param implType  创建的实例类型 -- 需包含无参构造函数，自动转factory
     */
    public static GenericCodecInfo create(TypeInfo typeInfo, Class<? extends DsonCodec> codecType, Class<?> implType) {
        if (implType == null) {
            return createImpl(typeInfo, codecType, null);
        }
        if (!typeInfo.rawType.isAssignableFrom(implType)) {
            throw new IllegalArgumentException("bad implType");
        }
        Supplier<?> factory = DsonConverterUtils.tryNoArgConstructorToSupplier(implType);
        if (factory == null) {
            throw new IllegalArgumentException("bad implType");
        }
        return createImpl(typeInfo, codecType, factory);
    }

    /**
     * 通过工厂创建一个item
     *
     * @param typeInfo  泛型擦除后的类型信息
     * @param codecType 编解码器的类型
     * @param factory   创建的实例类型 -- 需包含无参构造函数，自动转factory
     */
    public static GenericCodecInfo create(TypeInfo typeInfo, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        if (factory != null && !typeInfo.rawType.isInstance(factory.get())) {
            throw new IllegalArgumentException("bad factory");
        }
        return createImpl(typeInfo, codecType, factory);
    }

    private static GenericCodecInfo createImpl(TypeInfo typeInfo, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        TypeVariable<? extends Class<?>>[] typeParameters = typeInfo.rawType.getTypeParameters();
        if (typeParameters.length == 0) {
            throw new IllegalArgumentException("rawType is not genericType, type:" + typeInfo.rawType);
        }
        if (typeParameters.length != typeInfo.genericArgs.size()) {
            throw new IllegalArgumentException("rawType.GenericTypeArguments.Length != typeInfo.genericArgs.Length, type: " + typeInfo.rawType);
        }
        if (typeParameters.length != codecType.getTypeParameters().length) {
            throw new IllegalArgumentException("rawType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length, type: " + typeInfo.rawType);
        }
        return new GenericCodecInfo(typeInfo, codecType, factory);
    }
}
