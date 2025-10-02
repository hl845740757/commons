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
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTexts;
import cn.wjybxx.dson.types.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.time.LocalDateTime;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2023/4/23
 */
abstract class AbstractObjectReader implements DsonObjectReader {

    protected DsonConverter converter;
    protected DsonReader reader;

    AbstractObjectReader(DsonConverter converter, DsonReader reader) {
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
        // 当声明类型是DsonValue类型时，需要保留Header
        if (DsonValue.class.isAssignableFrom(rawType)) {
            return (T) Dsons.readDsonValue(reader);
        }
        // 容器类型只能通过codec解码；Flags编码为Int/String数组的情况下，需要自定义Field读写代理处理
        if (dsonType.isContainer()) {
            String clsName = null;
            if (!reader.isWaitingStart()) {
                TypeHeader header = readHeader(dsonType);
                clsName = header.clsName;
                backToWaitStart(header);
            }
            @SuppressWarnings("unchecked") DsonCodecImpl<T> codec = (DsonCodecImpl<T>) findObjectDecoder(declaredType, factory, clsName);
            if (codec == null) {
                throw DsonCodecException.incompatible(declaredType.rawType, clsName);
            }
            return codec.readObject(this, declaredType, factory);
        }
        // 非容器类型 -- Dson内建结构，Enum，Const等
        {
            DsonCodecImpl<T> codec = (DsonCodecImpl<T>) converter.codecRegistry().getDecoder(declaredType);
            if (codec != null) {
                return codec.readObject(this, declaredType, factory);
            }
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        return (T) DsonCodecHelper.readDsonValueValue(reader, name);
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
    @Nonnull
    public DsonType getCurrentDsonType() {
        return reader.getCurrentDsonType();
    }

    @Override
    public String getCurrentName() {
        return reader.getCurrentName();
    }

    protected void backToWaitStart(TypeHeader header) {
        reader.backToWaitStart();
    }

    @Override
    public int readStartObject() {
        reader.readStartObject();
        return 0;
    }

    @Override
    public void readEndObject() {
        reader.skipToEndOfObject();
        reader.readEndObject();
    }

    @Override
    public int readStartArray() {
        reader.readStartArray();
        return 0;
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
        if (reader instanceof DsonTextReader textReader) {
            textReader.setComponentType(dsonType);
        }
    }

    @Override
    public void close() {
        reader.close();
    }

    final TypeHeader readHeader(DsonType dsonType) {
        DsonReader reader = this.reader;
        if (dsonType == DsonType.OBJECT) {
            reader.readStartObject();
        } else {
            reader.readStartArray();
        }
        if (reader.peekDsonType() != DsonType.HEADER) return TypeHeader.EMPTY;
        String clsName = null;
        int count = 0;
        //
        reader.readDsonType();
        reader.readStartHeader();
        while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
            String name = reader.readName();
            if (name.equals(DsonHeader.NAMES_CLASS_NAME)) {
                clsName = reader.readString(null).intern(); // 池化
            } else if (name.equals(DsonHeader.NAMES_COUNT)) {
                count = DsonCodecHelper.readInt(reader, null); // 文本下可能是double
            } else {
                reader.skipValue();
            }
        }
        reader.skipToEndOfObject();
        reader.readEndHeader();
        return new TypeHeader(clsName, count);
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
}