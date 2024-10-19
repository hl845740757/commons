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

package cn.wjybxx.dson;

import cn.wjybxx.base.io.StringBuilderWriter;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.ext.Projection;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.text.*;

import javax.annotation.Nullable;
import java.io.Reader;

/**
 * dson的辅助工具类，二进制流工具类{@link DsonLites}
 *
 * @author wjybxx
 * date - 2023/4/19
 */
@SuppressWarnings("unused")
public final class Dsons {

    /** {@link DsonType}占用的比特位 */
    public static final int DSON_TYPE_BITES = 5;
    /** {@link DsonType}的最大类型编号 */
    public static final int DSON_TYPE_MAX_VALUE = 31;

    /** {@link WireType}占位的比特位数 */
    public static final int WIRETYPE_BITS = 3;
    public static final int WIRETYPE_MASK = (1 << WIRETYPE_BITS) - 1;
    /** wireType看做数值时的最大值 */
    public static final int WIRETYPE_MAX_VALUE = 7;

    /** 完整类型信息占用的比特位数 */
    public static final int FULL_TYPE_BITS = DSON_TYPE_BITES + WIRETYPE_BITS;
    public static final int FULL_TYPE_MASK = (1 << FULL_TYPE_BITS) - 1;

    /** 二进制数据的最大长度 */
    public static final int MAX_BINARY_LENGTH = Integer.MAX_VALUE - 6;

    public static String internField(String fieldName) {
        // 长度异常的数据不池化
        return fieldName.length() <= 32 ? fieldName.intern() : fieldName;
    }

    // fullType

    /**
     * @param dsonType 数据类型
     * @param wireType 特殊编码类型
     * @return fullType 完整类型
     */
    public static int makeFullType(DsonType dsonType, WireType wireType) {
        return (dsonType.getNumber() << WIRETYPE_BITS) | wireType.getNumber();
    }

    /**
     * @param dsonType 数据类型 5bits[0~31]
     * @param wireType 特殊编码类型 3bits[0~7]
     * @return fullType 完整类型
     */
    public static int makeFullType(int dsonType, int wireType) {
        return (dsonType << WIRETYPE_BITS) | wireType;
    }

    public static int dsonTypeOfFullType(int fullType) {
        return fullType >>> WIRETYPE_BITS;
    }

    public static int wireTypeOfFullType(int fullType) {
        return (fullType & WIRETYPE_MASK);
    }

    // region check
    public static int checkSubType(int type) {
        if (type < 0) {
            throw new IllegalArgumentException("type cant be negative");
        }
        return type;
    }

    public static void checkBinaryLength(int length) {
        if (length > MAX_BINARY_LENGTH) {
            throw new IllegalArgumentException("the length of data must between[0, %d], but found: %d"
                    .formatted(MAX_BINARY_LENGTH, length));
        }
    }

    public static void checkHasValue(int value, boolean hasVal) {
        if (!hasVal && value != 0) {
            throw new IllegalArgumentException();
        }
    }

    public static void checkHasValue(long value, boolean hasVal) {
        if (!hasVal && value != 0) {
            throw new IllegalArgumentException();
        }
    }

    public static void checkHasValue(double value, boolean hasVal) {
        if (!hasVal && value != 0) {
            throw new IllegalArgumentException();
        }
    }
    // endregion

    // region read/write

    /**
     * 读取顶层集合
     * 会将独立的header合并到容器中，会将分散的元素读取存入数组
     */
    public static DsonArray<String> readCollection(DsonReader reader) {
        final DsonArray<String> collection = new DsonArray<>(4);
        DsonType dsonType;
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, collection.getHeader());
            } else if (dsonType == DsonType.OBJECT) {
                collection.add(readObject(reader));
            } else if (dsonType == DsonType.ARRAY) {
                collection.add(readArray(reader));
            } else {
                throw DsonIOException.invalidTopDsonType(dsonType);
            }
        }
        return collection;
    }

    /**
     * 写入顶层集合
     * 顶层容器的header和元素将被展开，而不是嵌套在数组中
     */
    public static void writeCollection(DsonWriter writer, DsonArray<String> collection) {
        if (!collection.getHeader().isEmpty()) {
            writeHeader(writer, collection.getHeader());
        }
        for (DsonValue dsonValue : collection) {
            if (dsonValue.getDsonType() == DsonType.OBJECT) {
                writeObject(writer, dsonValue.asObject(), ObjectStyle.INDENT);
            } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
                writeArray(writer, dsonValue.asArray(), ObjectStyle.INDENT);
            } else {
                throw DsonIOException.invalidTopDsonType(dsonValue.getDsonType());
            }
        }
    }

    /** @param dsonValue 顶层对象；可以是Header */
    public static void writeTopDsonValue(DsonWriter writer, DsonValue dsonValue) {
        writeTopDsonValue(writer, dsonValue, ObjectStyle.INDENT);
    }

    /**
     * @param dsonValue 顶层对象；可以是Header
     * @param style     文本编码时的格式
     */
    public static void writeTopDsonValue(DsonWriter writer, DsonValue dsonValue, ObjectStyle style) {
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            writeObject(writer, dsonValue.asObject(), style);
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            writeArray(writer, dsonValue.asArray(), style);
        } else if (dsonValue.getDsonType() == DsonType.HEADER) {
            writeHeader(writer, dsonValue.asHeader());
        } else {
            throw DsonIOException.invalidTopDsonType(dsonValue.getDsonType());
        }
    }

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue readTopDsonValue(DsonReader reader) {
        return readTopDsonValue(reader, null);
    }

    /**
     * @param fileHeader 用于接收文件头信息
     * @return 如果到达文件尾部，则返回null；如果读取到header，则存储给定参数中，并返回给定对象
     */
    public static DsonValue readTopDsonValue(DsonReader reader, DsonHeader<String> fileHeader) {
        DsonType dsonType = reader.readDsonType();
        if (dsonType == DsonType.END_OF_OBJECT) {
            return null;
        }
        if (dsonType == DsonType.OBJECT) {
            return readObject(reader);
        } else if (dsonType == DsonType.ARRAY) {
            return readArray(reader);
        } else if (dsonType == DsonType.HEADER) {
            return readHeader(reader, fileHeader);
        }
        throw DsonIOException.invalidTopDsonType(dsonType);
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeObject(DsonWriter writer, DsonObject<String> dsonObject, ObjectStyle style) {
        writer.writeStartObject(style);
        if (!dsonObject.getHeader().isEmpty()) {
            writeHeader(writer, dsonObject.getHeader());
        }
        dsonObject.forEach((name, dsonValue) -> writeDsonValue(writer, dsonValue, name));
        writer.writeEndObject();
    }

    public static DsonObject<String> readObject(DsonReader reader) {
        DsonObject<String> dsonObject = new DsonObject<>();
        DsonType dsonType;
        String name;
        DsonValue value;
        reader.readStartObject();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonObject.getHeader());
            } else {
                name = reader.readName();
                value = readDsonValue(reader);
                dsonObject.put(name, value);
            }
        }
        reader.readEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeArray(DsonWriter writer, DsonArray<String> dsonArray, ObjectStyle style) {
        writer.writeStartArray(style);
        if (!dsonArray.getHeader().isEmpty()) {
            writeHeader(writer, dsonArray.getHeader());
        }
        for (DsonValue dsonValue : dsonArray) {
            writeDsonValue(writer, dsonValue, null);
        }
        writer.writeEndArray();
    }

    public static DsonArray<String> readArray(DsonReader reader) {
        DsonArray<String> dsonArray = new DsonArray<>();
        DsonType dsonType;
        DsonValue value;
        reader.readStartArray();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonArray.getHeader());
            } else {
                value = readDsonValue(reader);
                dsonArray.add(value);
            }
        }
        reader.readEndArray();
        return dsonArray;
    }

    public static void writeHeader(DsonWriter writer, DsonHeader<String> header) {
        if (header.size() == 1) {
            DsonValue clsName = header.get(DsonHeader.NAMES_CLASS_NAME);
            if (clsName != null) { // header只包含clsName时打印为简单模式
                writer.writeSimpleHeader(clsName.asString());
                return;
            }
        }
        writer.writeStartHeader(ObjectStyle.FLOW);
        header.forEach((key, value) -> writeDsonValue(writer, value, key));
        writer.writeEndHeader();
    }

    public static DsonHeader<String> readHeader(DsonReader reader, @Nullable DsonHeader<String> header) {
        if (header == null) header = new DsonHeader<>();
        DsonType dsonType;
        String name;
        DsonValue value;
        reader.readStartHeader();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            assert dsonType != DsonType.HEADER;
            name = reader.readName();
            value = readDsonValue(reader);
            header.put(name, value);
        }
        reader.readEndHeader();
        return header;
    }

    public static void writeDsonValue(DsonWriter writer, DsonValue dsonValue, @Nullable String name) {
        if (writer.isAtName()) {
            writer.writeName(name);
        }
        switch (dsonValue.getDsonType()) {
            case INT32 -> writer.writeInt32(name, dsonValue.asInt32(), WireType.VARINT, NumberStyle.TYPED); // 必须能精确反序列化
            case INT64 -> writer.writeInt64(name, dsonValue.asInt64(), WireType.VARINT, NumberStyle.TYPED);
            case FLOAT -> writer.writeFloat(name, dsonValue.asFloat(), NumberStyle.TYPED);
            case DOUBLE -> writer.writeDouble(name, dsonValue.asDouble(), NumberStyle.SIMPLE);
            case BOOL -> writer.writeBool(name, dsonValue.asBool());
            case STRING -> writer.writeString(name, dsonValue.asString(), StringStyle.AUTO);
            case NULL -> writer.writeNull(name);
            case BINARY -> writer.writeBinary(name, dsonValue.asBinary());
            case POINTER -> writer.writePtr(name, dsonValue.asPointer());
            case LITE_POINTER -> writer.writeLitePtr(name, dsonValue.asLitePointer());
            case DATETIME -> writer.writeDateTime(name, dsonValue.asDateTime());
            case TIMESTAMP -> writer.writeTimestamp(name, dsonValue.asTimestamp());
            case HEADER -> writeHeader(writer, dsonValue.asHeader());
            case ARRAY -> writeArray(writer, dsonValue.asArray(), ObjectStyle.INDENT);
            case OBJECT -> writeObject(writer, dsonValue.asObject(), ObjectStyle.INDENT);
            case END_OF_OBJECT -> throw new AssertionError();
        }
    }

    public static DsonValue readDsonValue(DsonReader reader) {
        DsonType dsonType = reader.getCurrentDsonType();
        reader.skipName();
        final String name = "";
        return switch (dsonType) {
            case INT32 -> new DsonInt32(reader.readInt32(name));
            case INT64 -> new DsonInt64(reader.readInt64(name));
            case FLOAT -> new DsonFloat(reader.readFloat(name));
            case DOUBLE -> new DsonDouble(reader.readDouble(name));
            case BOOL -> new DsonBool(reader.readBool(name));
            case STRING -> new DsonString(reader.readString(name));
            case NULL -> {
                reader.readNull(name);
                yield DsonNull.NULL;
            }
            case BINARY -> new DsonBinary(reader.readBinary(name));
            case POINTER -> new DsonPointer(reader.readPtr(name));
            case LITE_POINTER -> new DsonLitePointer(reader.readLitePtr(name));
            case DATETIME -> new DsonDateTime(reader.readDateTime(name));
            case TIMESTAMP -> new DsonTimestamp(reader.readTimestamp(name));
            case HEADER -> {
                DsonHeader<String> header = new DsonHeader<>();
                readHeader(reader, header);
                yield header;
            }
            case OBJECT -> readObject(reader);
            case ARRAY -> readArray(reader);
            case END_OF_OBJECT -> throw new AssertionError();
        };
    }

    // endregion

    // region 拷贝

    /** 深度拷贝为可变对象 */
    public static DsonValue mutableDeepCopy(DsonValue dsonValue) {
        return mutableDeepCopy(dsonValue, 0);
    }

    /**
     * 深度拷贝为可变对象
     *
     * @param stack 当前栈深度
     */
    private static DsonValue mutableDeepCopy(DsonValue dsonValue, int stack) {
        if (stack > 100) throw new IllegalStateException("Check for circular references");
        switch (dsonValue.getDsonType()) {
            case OBJECT -> {
                DsonObject<String> src = dsonValue.asObject();
                DsonObject<String> result = new DsonObject<>(src.size());
                copyKVPair(src.getHeader(), result.getHeader(), stack);
                copyKVPair(src, result, stack);
                return result;
            }
            case ARRAY -> {
                DsonArray<String> src = dsonValue.asArray();
                DsonArray<String> result = new DsonArray<>(src.size());
                copyKVPair(src.getHeader(), result.getHeader(), stack);
                copyElements(src, result, stack);
                return result;
            }
            case HEADER -> {
                DsonHeader<String> src = dsonValue.asHeader();
                DsonHeader<String> result = new DsonHeader<>();
                copyKVPair(src, result, stack);
                return result;
            }
            case BINARY -> {
                return new DsonBinary(dsonValue.asBinary().deepCopy());
            }
            default -> {
                return dsonValue;
            }
        }
    }

    private static void copyKVPair(AbstractDsonObject<String> src, AbstractDsonObject<String> dest, int stack) {
        if (!src.isEmpty()) {
            src.forEach((s, dsonValue) -> dest.put(s, mutableDeepCopy(dsonValue, stack + 1)));
        }
    }

    private static void copyElements(AbstractDsonArray src, AbstractDsonArray dest, int stack) {
        if (!src.isEmpty()) {
            src.forEach(e -> dest.add(mutableDeepCopy(e, stack + 1)));
        }
    }

    // endregion

    // region 快捷方法

    public static String toCollectionDson(DsonArray<String> collection) {
        return toCollectionDson(collection, null);
    }

    /** 该接口用于写顶层数组容器，所有元素将被展开 */
    public static String toCollectionDson(DsonArray<String> collection, DsonTextWriterSettings settings) {
        if (settings == null) settings = DsonTextWriterSettings.DEFAULT;
        StringBuilder sb = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.acquire();
        try {
            try (DsonTextWriter writer = new DsonTextWriter(settings, new StringBuilderWriter(sb))) {
                writeCollection(writer, collection);
            }
            return sb.toString();
        } finally {
            ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.release(sb);
        }
    }

    /** 该接口用于读取多顶层对象dson文本 */
    public static DsonArray<String> fromCollectionDson(String dsonString) {
        try (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString)) {
            return readCollection(reader);
        }
    }

    public static String toDson(DsonValue dsonValue) {
        return toDson(dsonValue, ObjectStyle.INDENT, DsonTextWriterSettings.DEFAULT);
    }

    public static String toDson(DsonValue dsonValue, ObjectStyle style) {
        return toDson(dsonValue, style, DsonTextWriterSettings.DEFAULT);
    }

    /** 简单转写为Dson，数组不会被展开 */
    public static String toDson(DsonValue dsonValue, ObjectStyle style, DsonTextWriterSettings settings) {
        if (!dsonValue.getDsonType().isContainerOrHeader()) {
            throw new IllegalArgumentException("invalid dsonType " + dsonValue.getDsonType());
        }
        StringBuilder sb = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.acquire();
        try {
            try (DsonTextWriter writer = new DsonTextWriter(settings, new StringBuilderWriter(sb))) {
                writeTopDsonValue(writer, dsonValue, style);
            }
            return sb.toString();
        } finally {
            ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.release(sb);
        }
    }

    /** 默认只读取第一个对象 */
    public static DsonValue fromDson(CharSequence dsonString) {
        try (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.DEFAULT, dsonString)) {
            return readTopDsonValue(reader);
        }
    }

    /**
     * 将原始Dson字符串按照给定投影信息进行投影
     *
     * @param dsonString  原始的dson字符串
     * @param projectInfo 投影描述
     */
    public static DsonValue project(String dsonString, String projectInfo) {
        return new Projection(projectInfo).project(dsonString);
    }

    /**
     * 将原始Dson字符串按照给定投影信息进行投影
     *
     * @param dsonString  原始的dson字符串
     * @param projectInfo 投影描述
     */
    public static DsonValue project(String dsonString, DsonObject<String> projectInfo) {
        return new Projection(projectInfo).project(dsonString);
    }

    /** 获取dsonValue的clsName -- dson的约定之一 */
    public static String getClassName(DsonValue dsonValue) {
        DsonHeader<?> header;
        if (dsonValue instanceof DsonObject<?> dsonObject) {
            header = dsonObject.getHeader();
        } else if (dsonValue instanceof DsonArray<?> dsonArray) {
            header = dsonArray.getHeader();
        } else {
            return null;
        }
        DsonValue wrapped = header.get(DsonHeader.NAMES_CLASS_NAME);
        return wrapped instanceof DsonString dsonString ? dsonString.getValue() : null;
    }

    /** 获取dsonValue的localId -- dson的约定之一 */
    public static String getLocalId(DsonValue dsonValue) {
        DsonHeader<?> header;
        if (dsonValue instanceof DsonObject<?> dsonObject) {
            header = dsonObject.getHeader();
        } else if (dsonValue instanceof DsonArray<?> dsonArray) {
            header = dsonArray.getHeader();
        } else {
            return null;
        }
        DsonValue wrapped = header.get(DsonHeader.NAMES_LOCAL_ID);
        return wrapped instanceof DsonString dsonString ? dsonString.getValue() : null;
    }

    // endregion

    // region 工厂方法

    public static DsonScanner newStringScanner(CharSequence dsonString) {
        return new DsonScanner(DsonCharStream.newCharStream(dsonString));
    }

    public static DsonScanner newStreamScanner(Reader reader) {
        return new DsonScanner(DsonCharStream.newBufferedCharStream(reader));
    }

    public static DsonScanner newStreamScanner(Reader reader, boolean autoClose) {
        return new DsonScanner(DsonCharStream.newBufferedCharStream(reader, autoClose));
    }

    // endregion

}