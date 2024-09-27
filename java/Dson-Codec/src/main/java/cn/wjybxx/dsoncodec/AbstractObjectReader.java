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
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTexts;
import cn.wjybxx.dson.text.DsonToken;
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
    public ObjectLitePtr readLitePtr(String name) {
        return readName(name) ? DsonCodecHelper.readLitePtr(reader, name) : null;
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
    public <T> T readObject(String name, TypeInfo typeInfo, Supplier<? extends T> factory) {
        Class<T> declaredType = (Class<T>) typeInfo.rawType;
        if (!readName(name)) { // 顺带读取了DsonType
            return (T) DsonConverterUtils.getDefaultValue(declaredType);
        }

        DsonReader reader = this.reader;
        // 基础类型不能返回null
        if (declaredType.isPrimitive()) {
            return (T) DsonCodecHelper.readPrimitive(reader, name, declaredType);
        }
        if (declaredType == String.class) {
            return (T) DsonCodecHelper.readString(reader, name);
        }
        if (declaredType == byte[].class) {
            Binary binary = DsonCodecHelper.readBinary(reader, name);
            return (T) (binary == null ? null : binary.unsafeBuffer());
        }

        DsonType dsonType = reader.getCurrentDsonType();
        if (dsonType == DsonType.NULL) { // null直接返回
            reader.readNull(name);
            return null;
        }
        if (dsonType.isContainer()) { // 容器类型只能通过codec解码
            return readContainer(typeInfo, factory, dsonType);
        }

        // 非容器类型,Dson内建结构
        // 考虑枚举类型 -- 可转换为基础值类型的Object
        if (declaredType.isEnum()) {
            DsonCodecRegistry rootRegistry = converter.codecRegistry();
            DsonCodecImpl<T> codec = (DsonCodecImpl<T>) rootRegistry.getDecoder(typeInfo, rootRegistry, converter.genericCodecHelper());
            assert codec != null;
            return codec.readObject(this, typeInfo, factory);
        }
        // 考虑包装类型
        Class<?> unboxed = DsonConverterUtils.unboxIfWrapperType(declaredType);
        if (unboxed.isPrimitive()) {
            return (T) DsonCodecHelper.readPrimitive(reader, name, unboxed);
        }
        // 考虑DsonValue
        if (DsonValue.class.isAssignableFrom(declaredType)) {
            return declaredType.cast(Dsons.readDsonValue(reader));
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        return declaredType.cast(DsonCodecHelper.readDsonValue(reader, dsonType, name));
    }

    private <T> T readContainer(TypeInfo typeInfo, Supplier<? extends T> factory, DsonType dsonType) {
        String classId = readClassId(dsonType);
        @SuppressWarnings("unchecked") DsonCodecImpl<T> codec = (DsonCodecImpl<T>) findObjectDecoder(typeInfo, factory, classId);
        if (codec == null) {
            throw DsonCodecException.incompatible(typeInfo.rawType, classId);
        }
        return codec.readObject(this, typeInfo, factory);
    }

    // endregion

    // region 流程

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

    @Override
    public void readStartObject(TypeInfo typeInfo) {
        if (reader.isAtType()) { // 顶层对象适配
            reader.readDsonType();
        }
        reader.readStartObject();
    }

    @Override
    public void readEndObject() {
        reader.skipToEndOfObject();
        reader.readEndObject();
    }

    @Override
    public void readStartArray(TypeInfo typeInfo) {
        if (reader.isAtType()) { // 顶层对象适配
            reader.readDsonType();
        }
        reader.readStartArray();
        reader.attach(typeInfo); // 数组默认总是直接attach
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
        if (!keyDeclared.isEnum()) {
            throw DsonCodecException.unsupportedKeyType(keyDeclared);
        }
        // 处理枚举类型
        DsonCodecRegistry rootRegistry = converter.codecRegistry();
        DsonCodecImpl<T> codec = (DsonCodecImpl<T>) rootRegistry.getDecoder(keyTypeInfo, rootRegistry, converter.genericCodecHelper());
        assert codec != null;
        T result;
        if (converter.options().writeEnumAsString) {
            result = codec.forName(keyString);
        } else {
            int number = Integer.parseInt(keyString);
            result = codec.forNumber(number);
        }
        if (result == null) {
            throw DsonCodecException.enumAbsent(keyDeclared, keyString);
        }
        return result;
    }

    @Override
    public void setComponentType(DsonType dsonType) {
        if (reader instanceof DsonTextReader textReader) {
            DsonToken token = DsonTexts.clsNameTokenOfType(dsonType);
            textReader.setCompClsNameToken(token);
        }
    }

    @Override
    public void close() {
        reader.close();
    }

    private String readClassId(DsonType dsonType) {
        DsonReader reader = this.reader;
        if (reader.hasWaitingStartContext()) {
            return ""; // 已读取header，当前可能触发了读代理
        }
        if (dsonType == DsonType.OBJECT) {
            reader.readStartObject();
        } else {
            reader.readStartArray();
        }
        String clsName;
        DsonType nextDsonType = reader.peekDsonType();
        if (nextDsonType == DsonType.HEADER) {
            reader.readDsonType();
            reader.readStartHeader();
            clsName = reader.readString(DsonHeader.NAMES_CLASS_NAME);
            if (clsName.lastIndexOf(' ') < 0) {
                clsName = clsName.intern(); // 池化
            }
            reader.skipToEndOfObject();
            reader.readEndHeader();
        } else {
            clsName = "";
        }
        reader.backToWaitStart();
        return clsName;
    }

    private <T> DsonCodecImpl<?> findObjectDecoder(TypeInfo declaredType, Supplier<T> factory, String clsName) {
        // factory不为null时，直接按照声明类型查找，因为factory的优先级最高
        DsonCodecRegistry rootRegistry = converter.codecRegistry();
        if (factory != null) {
            return rootRegistry.getDecoder(declaredType, rootRegistry, converter.genericCodecHelper());
        }
        // 尝试按真实类型读 -- TODO 这里是否考虑继承泛型参数?对方应当写入了泛型参数才是
        if (!ObjectUtils.isBlank(clsName)) {
            TypeMeta typeMeta = converter.typeMetaRegistry().ofName(clsName);
            if (typeMeta != null && declaredType.rawType.isAssignableFrom(typeMeta.typeInfo.rawType)) {
                return rootRegistry.getDecoder(typeMeta.typeInfo, rootRegistry, converter.genericCodecHelper());
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return rootRegistry.getDecoder(declaredType, rootRegistry, converter.genericCodecHelper());
    }

    // endregion
}