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

    protected final TypeInfo encoderType;
    protected final Supplier<? extends Collection<E>> factory;
    private final FactoryKind factoryKind;

    public CollectionCodec(TypeInfo encoderType) {
        this(encoderType, null);
    }

    @SuppressWarnings("unchecked")
    public CollectionCodec(TypeInfo encoderType, Supplier<? extends Collection<E>> factory) {
        if (encoderType.genericArgs.size() != 1) {
            throw new IllegalArgumentException("encoderType.genericArgs.size() != 1");
        }
        if (factory == null) {
            factory = DsonConverterUtils.tryNoArgConstructorToSupplier((Class<? extends Collection<E>>) encoderType.rawType);
        }
        this.encoderType = encoderType;
        this.factory = factory;
        this.factoryKind = factory == null ? computeFactoryKind(encoderType) : FactoryKind.Unknown;
    }

    private static FactoryKind computeFactoryKind(TypeInfo typeInfo) {
        Class<?> clazz = typeInfo.rawType;
        if (clazz == EnumSet.class && typeInfo.genericArgs.get(0).isEnum()) {
            return FactoryKind.EnumSet; // 考虑被擦除的情况
        }
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
        EnumSet,
        LinkedHashSet,
        ArrayDeque,
    }

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return encoderType;
    }

    /** {@link #encoderType}一定是用户declaredType的子类型，因此创建实例时不依赖declaredType */
    @SuppressWarnings({"unchecked", "rawtypes"})
    protected Collection<E> newCollection() {
        if (factory != null) return factory.get();
        return switch (factoryKind) {
            case EnumSet -> {
                TypeInfo elementTypeInfo = encoderType.genericArgs.get(0);
                yield EnumSet.noneOf((Class) elementTypeInfo.rawType);
            }
            case LinkedHashSet -> new LinkedHashSet<>();
            case ArrayDeque -> new ArrayDeque<>();
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
        TypeInfo elementTypeInfo = encoderType.genericArgs.get(0);

        for (E e : inst) {
            writer.writeObject(null, e, elementTypeInfo, null);
        }
    }

    @Override
    public Collection<E> readObject(DsonObjectReader reader, Supplier<? extends Collection<E>> factory) {
        TypeInfo elementTypeInfo = encoderType.genericArgs.get(0);

        Collection<E> result = factory != null ? factory.get() : newCollection();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            result.add(reader.readObject(null, elementTypeInfo));
        }
        return reader.options().readAsImmutable ? toImmutable(result) : result;
    }

}