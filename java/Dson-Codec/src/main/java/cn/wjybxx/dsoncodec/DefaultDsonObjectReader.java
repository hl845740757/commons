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

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.text.DsonTexts;
import cn.wjybxx.dson.types.Binary;
import cn.wjybxx.dson.types.ExtDateTime;
import cn.wjybxx.dson.types.ObjectPtr;
import cn.wjybxx.dson.types.Timestamp;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.time.LocalDateTime;
import java.util.*;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
final class DefaultDsonObjectReader implements DsonObjectReader {

    private final DsonConverter converter;
    private final DsonCollectionReader reader;

    DefaultDsonObjectReader(DsonConverter converter, DsonCollectionReader reader) {
        this.converter = converter;
        this.reader = reader;
    }

    // region 简单值

    @Override
    public int readInt(String name) {
        return readName(name) ? DsonCodecHelper.readInt(reader, name) : 0;
    }

    @Override
    public long readLong(String name) {
        return readName(name) ? DsonCodecHelper.readLong(reader, name) : 0;
    }

    @Override
    public float readFloat(String name) {
        return readName(name) ? DsonCodecHelper.readFloat(reader, name) : 0;
    }

    @Override
    public double readDouble(String name) {
        return readName(name) ? DsonCodecHelper.readDouble(reader, name) : 0;
    }

    @Override
    public boolean readBoolean(String name) {
        return readName(name) && DsonCodecHelper.readBool(reader, name);
    }

    @Override
    public String readString(String name) {
        return readName(name) ? DsonCodecHelper.readString(reader, name) : null;
    }

    @Override
    public void readNull(String name) {
        if (readName(name)) {
            DsonCodecHelper.readNull(reader, name);
        }
    }

    @Override
    public Binary readBinary(String name) {
        return readName(name) ? DsonCodecHelper.readBinary(reader, name) : null;
    }

    @Override
    public ObjectPtr readPtr(String name) {
        return readName(name) ? DsonCodecHelper.readPtr(reader, name) : null;
    }

    @Override
    public LocalDateTime readDateTime(String name) {
        if (readName(name)) { // java不是结构体可能返回null
            ExtDateTime extDateTime = DsonCodecHelper.readDateTime(reader, name);
            return extDateTime == null ? null : extDateTime.toDateTime();
        }
        return null;
    }

    @Override
    public ExtDateTime readExtDateTime(String name) {
        return readName(name) ? DsonCodecHelper.readDateTime(reader, name) : null;
    }

    @Override
    public Timestamp readTimestamp(String name) {
        return readName(name) ? DsonCodecHelper.readTimestamp(reader, name) : null;
    }

    // endregion

    // region object处理

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> T readObject(String name, TypeInfo declaredType, Supplier<? extends T> factory) {
        Class<T> rawType = (Class<T>) declaredType.rawType;
        if (!readName(name)) { // 字段不存在，返回默认值
            return (T) DsonConverterUtils.getDefaultValue(rawType);
        }
        DsonType dsonType = reader.getCurrentDsonType();
        if (dsonType == DsonType.NULL) { // null直接返回
            reader.readNull(name);
            return (T) DsonConverterUtils.getDefaultValue(rawType);
        }
        // TODO 引用支持
        // DsonValue接收原始数据
        if (DsonValue.class.isAssignableFrom(rawType)) {
            return (T) Dsons.readDsonValue(reader);
        }
        // 容器类型只能通过codec解码
        if (dsonType.isContainer()) {
            String clsName = getClassName(reader.getCurrentValue());
            DsonCodecImpl<T> decoder = (DsonCodecImpl<T>) findObjectDecoder(declaredType, factory, clsName);
            if (decoder == null) {
                throw DsonCodecException.incompatible(declaredType.rawType, clsName);
            }
            return decoder.readObject(this, declaredType, factory);
        } else {
            // 非容器类型 -- Dson内建结构，Enum，Const等
            DsonCodecImpl<T> decoder = (DsonCodecImpl<T>) converter.codecRegistry().getDecoder(declaredType);
            if (decoder != null) {
                return decoder.readObject(this, declaredType, factory);
            }
            // 默认类型转换-声明类型可能是个抽象类型，eg：Number
            return (T) DsonCodecHelper.readDsonValueValue(reader, name);
        }
    }

    private static String getClassName(DsonValue dsonValue) {
        DsonHeader<String> header;
        if (dsonValue instanceof DsonObject<?>) {
            header = dsonValue.asObject().getHeader();
        } else {
            header = dsonValue.asArray().getHeader();
        }
        DsonValue boxedValue = header.get(DsonHeader.NAMES_CLASS_NAME);
        return boxedValue != null ? boxedValue.asString() : null;
    }

    private DsonCodecImpl<?> findObjectDecoder(TypeInfo declaredType, Supplier<?> factory, String clsName) {
        // factory不为null时，直接按照声明类型查找 -- factory创建的实例可能和写入的真实类型不兼容
        if (factory != null) {
            return converter.codecRegistry().getDecoder(declaredType);
        }
        // 尝试按真实类型读 -- TODO 这里是否考虑继承泛型参数?对方应当写入了泛型参数才是
        if (!ObjectUtils.isBlank(clsName)) {
            TypeMeta typeMeta = converter.typeMetaRegistry().ofName(clsName);
            if (typeMeta != null && declaredType.rawType.isAssignableFrom(typeMeta.typeInfo.rawType)) {
                return converter.codecRegistry().getDecoder(typeMeta.typeInfo);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return converter.codecRegistry().getDecoder(declaredType);
    }

    // endregion

    // region 流程

    @Override
    public DsonConverter converter() {
        return converter;
    }

    @Override
    public ConverterOptions options() {
        return converter.options();
    }

    @Override
    public DsonContextType getContextType() {
        return reader.getContextType();
    }

    @Override
    public DsonType readDsonType() {
        return reader.isAtType() ? reader.readDsonType() : reader.getCurrentDsonType();
    }

    @Override
    public String readName() {
        return reader.isAtName() ? reader.readName() : reader.getCurrentName();
    }

    @Override
    public boolean readName(String name) {
        DsonReader reader = this.reader;
        // array
        if (reader.getContextType().isArrayLike()) {
            if (reader.isAtValue()) {
                return true;
            }
            if (reader.isAtType()) {
                return reader.readDsonType() != DsonType.END_OF_OBJECT;
            }
            return reader.getCurrentDsonType() != DsonType.END_OF_OBJECT;
        }
        // object
        if (reader.isAtValue()) {
            if (name == null || reader.getCurrentName().equals(name)) {
                return true;
            }
            reader.skipValue();
        }
        Objects.requireNonNull(name, "name");
        if (reader.isAtType()) {
            // 用户尚未调用readDsonType，可指定下一个key的值
            Context context = (Context) reader.attachment();
            if (context.contains(name)) {
                context.setNext(name);
                reader.readDsonType();
                reader.readName();
                return true;
            }
            return false;
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            return name.equals(reader.readName()); // 不抛出异常
        }
    }

    @Override
    @Nonnull
    public DsonType getCurrentDsonType() {
        return reader.getCurrentDsonType();
    }

    @Override
    public String getCurrentName() {
        return reader.getCurrentName();
    }

    @Override
    public int readStartObject() {
        DsonCollectionReader reader = this.reader;
        reader.readStartObject();
        //
        if (reader.peekDsonType() == DsonType.HEADER) {
            reader.readDsonType();
            reader.skipValue();
        }
        Context context = contextPool.acquire();
        context.setKeySet(reader.getkeySet());
        reader.setKeyItr(context, DsonNull.NULL);
        reader.attach(context);
        return reader.getContainer().asObject().size();
    }

    @Override
    public void readEndObject() {
        if (reader.attach(null) instanceof Context context) {
            contextPool.release(context);
        }
        reader.skipToEndOfObject();
        reader.readEndObject();
    }

    @Override
    public int readStartArray() {
        DsonCollectionReader reader = this.reader;
        reader.readStartArray();
        //
        if (reader.peekDsonType() == DsonType.HEADER) {
            reader.readDsonType();
            reader.skipValue();
        }
        return reader.getContainer().asArray().size();
    }

    @Override
    public void readEndArray() {
        reader.skipToEndOfObject();
        reader.readEndArray();
    }

    @Override
    public void skipName() {
        reader.skipName();
    }

    @Override
    public void skipValue() {
        reader.skipValue();
    }

    @Override
    public void skipToEndOfObject() {
        reader.skipToEndOfObject();
    }

    @Override
    public byte[] readValueAsBytes(String name) {
        return readName(name) ? reader.readValueAsBytes(name) : null;
    }

    @SuppressWarnings("unchecked")
    @Override
    public <T> T decodeKey(String keyString, TypeInfo keyTypeInfo) {
        Class<?> keyDeclared = keyTypeInfo.rawType;
        if (keyDeclared == String.class || keyDeclared == Object.class) {
            return (T) keyString;
        }
        // key一定是包装类型
        if (keyDeclared == Integer.class) {
            return (T) Integer.valueOf(keyString);
        }
        if (keyDeclared == Long.class) {
            return (T) Long.valueOf(keyString);
        }
        // 处理枚举类型
        DsonCodecImpl<T> codec = (DsonCodecImpl<T>) converter.codecRegistry().getDecoder(keyTypeInfo);
        if (codec == null || !codec.isEnumCodec()) {
            throw DsonCodecException.unsupportedKeyType(keyDeclared);
        }
        T result;
        if (DsonTexts.isParsable(keyString)) {
            int number = Integer.parseInt(keyString);
            result = codec.forNumber(number);
        } else {
            result = codec.forName(keyString);
        }
        if (result == null) {
            throw DsonCodecException.enumAbsent(keyDeclared, keyString);
        }
        return result;
    }

    @Override
    public void setEnableNameIntern(@Nullable Boolean value) {
        reader.setEnableNameIntern(value);
    }

    @Override
    public void setComponentType(DsonType dsonType) {
        //
    }

    @Override
    public void setEncoderType(TypeInfo encoderType) {
        Object attachment = reader.attachment();
        if (attachment instanceof Context context) {
            context.encoderType = encoderType;
        } else {
            reader.attach(encoderType);
        }
    }

    @Override
    public TypeInfo getEncoderType() {
        Object attachment = reader.attachment();
        if (attachment instanceof Context context) {
            return context.encoderType;
        }
        return (TypeInfo) attachment;
    }

    @Override
    public void close() {
        reader.close();
    }
    // endregion

    // region context

    /**
     * {@link LinkedHashSet}还是优于{@link ArrayDeque}，
     * 虽然多数情况下我们都是按照写入的顺序读取，但当Key不存在的时候，Deque删除元素的效率很差。
     * 考虑到这块尚不稳定，因此不开放给用户设置。
     */
    private static final ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<>(
            Context::new, Context::dispose, 256);

    private static class Context implements Iterator<String> {

        final LinkedHashSet<String> keyQueue = new LinkedHashSet<>(16);
        Set<String> keySet;
        TypeInfo encoderType;


        public void setKeySet(Set<String> keySet) {
            this.keySet = keySet;
            this.keyQueue.addAll(keySet);
        }

        public void setNext(String nextName) {
            Objects.requireNonNull(nextName);
            if (keyQueue.size() > 0 && keyQueue.getFirst().equals(nextName)) {
                return;
            }
            keyQueue.addFirst(nextName);
        }

        public boolean contains(String name) {
            return keySet.contains(name);
        }

        @Override
        public boolean hasNext() {
            return !keyQueue.isEmpty();
        }

        @Override
        public String next() {
            return keyQueue.removeFirst();
        }

        public void dispose() {
            keySet = null;
            encoderType = null;
        }
    }
    // endregion
}