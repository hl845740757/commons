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

package cn.wjybxx.dsoncodec.codecs;

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Supplier;

/**
 * 集合解码器，动态构造
 *
 * @author wjybxx
 * date 2023/4/4
 */
@DsonCodecScanIgnore
public class CollectionCodec<E> implements DsonCodec<Collection<E>> {

    protected final TypeInfo typeInfo;
    protected final Supplier<? extends Collection<E>> factory;
    private final FactoryKind factoryKind;

    public CollectionCodec(TypeInfo typeInfo) {
        this(typeInfo, null);
    }

    @SuppressWarnings("unchecked")
    public CollectionCodec(TypeInfo typeInfo, Supplier<? extends Collection<E>> factory) {
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends Collection<E>>) typeInfo.rawType);
        }
        this.typeInfo = typeInfo;
        this.factory = factory;
        this.factoryKind = factory == null ? computeFactoryKind(typeInfo) : FactoryKind.Unknown;
    }

    private static FactoryKind computeFactoryKind(TypeInfo typeInfo) {
        Class<?> clazz = typeInfo.rawType;
        if (Set.class.isAssignableFrom(clazz)) {
            return FactoryKind.LinkedHashSet;
        }
        if (Deque.class.isAssignableFrom(clazz)) {
            return FactoryKind.ArrayDeque;
        }
        return FactoryKind.Unknown;
    }

    private enum FactoryKind {
        Unknown,
        LinkedHashSet,
        ArrayDeque,
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return typeInfo;
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    protected Collection<E> newCollection(TypeInfo declaredType) {
        if (factory != null) return factory.get();
        // EnumSet只有通过DeclaredType构建才稳妥 -- 因为这里的Codec可能是泛型原型
        if (declaredType.rawType == EnumSet.class) {
            TypeInfo elementTypeInfo = declaredType.genericArgs.get(0);
            return EnumSet.noneOf((Class) elementTypeInfo.rawType);
        }
        return switch (factoryKind) {
            case ArrayDeque -> new ArrayDeque<>();
            case LinkedHashSet -> new LinkedHashSet<>();
            default -> new ArrayList<>();
        };
    }

    protected Collection<E> toImmutable(Collection<E> result) {
        if (result instanceof LinkedHashSet<E> linkedHashSet) {
            return Collections.unmodifiableSet(linkedHashSet);
        }
        if (result instanceof EnumSet<?>) {
            Set<? extends E> enumSet = (Set<? extends E>) result;
            return Collections.unmodifiableSet(enumSet);
        }
        if (result instanceof Set<E> set) {
            return CollectionUtils.toImmutableLinkedHashSet(set);
        }
        return List.copyOf(result);
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Collection<E> inst, TypeInfo declaredType, ObjectStyle style) {
        // 理论上declaredType只影响当前inst是否写入类型，因此应当优先从inst的真实类型中查询E的类型...
        // 另外，typeInfo就是根据【运行时类型】和【declaredType】生成的
        TypeInfo elementTypeInfo = typeInfo.genericArgs.get(0);

        for (Object e : inst) {
            writer.writeObject(null, e, elementTypeInfo, null);
        }
    }

    @Override
    public Collection<E> readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends Collection<E>> factory) {
        TypeInfo elementTypeInfo = typeInfo.genericArgs.get(0);

        Collection<E> result = factory != null ? factory.get() : newCollection(declaredType);
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, elementTypeInfo));
        }
        return reader.options().readAsImmutable ? toImmutable(result) : result;
    }

}