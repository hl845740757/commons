package cn.wjybxx.dson;

import cn.wjybxx.dson.io.DsonIOException;

import javax.annotation.Nullable;

/**
 * Dson二进制流工具类
 *
 * @author wjybxx
 * date - 2023/6/15
 */
public class DsonLites {

    /** 继承深度占用的比特位 */
    private static final int IDEP_BITS = 3;
    private static final int IDEP_MASK = (1 << IDEP_BITS) - 1;
    /**
     * 支持的最大继承深度 - 7
     * 1.idep的深度不包含Object，没有显式继承其它类的类，idep为0
     * 2.超过7层我认为是你的代码有问题，而不是框架问题
     */
    public static final int IDEP_MAX_VALUE = IDEP_MASK;

    /** 类字段最大number */
    private static final short LNUMBER_MAX_VALUE = 8191;
    /** 类字段占用的最大比特位数 - 暂不对外开放 */
    private static final int LNUMBER_MAX_BITS = 13;

    // fieldNumber

    /** 计算一个类的继承深度 */
    public static int calIdep(Class<?> clazz) {
        if (clazz.isInterface() || clazz.isPrimitive()) {
            throw new IllegalArgumentException();
        }
        if (clazz == Object.class) {
            return 0;
        }
        int r = -1; // 去除Object；简单说：Object和Object的直接子类的idep都记为0，这很有意义。
        while ((clazz = clazz.getSuperclass()) != null) {
            r++;
        }
        return r;
    }

    /**
     * @param idep    继承深度[0~7]
     * @param lnumber 字段在类本地的编号
     * @return fullNumber 字段的完整编号
     */
    public static int makeFullNumber(int idep, int lnumber) {
        return (lnumber << IDEP_BITS) | idep;
    }

    public static int lnumberOfFullNumber(int fullNumber) {
        return fullNumber >>> IDEP_BITS;
    }

    public static byte idepOfFullNumber(int fullNumber) {
        return (byte) (fullNumber & IDEP_MASK);
    }

    public static int makeFullNumberZeroIdep(int lnumber) {
        return lnumber << IDEP_BITS;
    }

    // classId
    public static long makeClassGuid(int namespace, int classId) {
        return ((long) namespace << 32) | ((long) classId & 0xFFFF_FFFFL);
    }

    public static int namespaceOfClassGuid(long guid) {
        return (int) (guid >>> 32);
    }

    public static int lclassIdOfClassGuid(long guid) {
        return (int) guid;
    }

    // region read/write

    /**
     * 读取顶层集合
     * 会将独立的header合并到容器中，会将分散的元素读取存入数组
     */
    public static DsonArray<FieldNumber> readCollection(DsonLiteReader reader) {
        final DsonArray<FieldNumber> collection = new DsonArray<>(4);
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
    public static void writeCollection(DsonLiteWriter writer, DsonArray<FieldNumber> collection) {
        if (!collection.getHeader().isEmpty()) {
            writeHeader(writer, collection.getHeader());
        }
        for (DsonValue dsonValue : collection) {
            if (dsonValue.getDsonType() == DsonType.OBJECT) {
                writeObject(writer, dsonValue.asObjectLite());
            } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
                writeArray(writer, dsonValue.asArrayLite());
            } else {
                throw DsonIOException.invalidTopDsonType(dsonValue.getDsonType());
            }
        }
    }

    /** @param dsonValue 顶层对象；可以是Header */
    public static void writeTopDsonValue(DsonLiteWriter writer, DsonValue dsonValue) {
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            writeObject(writer, dsonValue.asObjectLite());
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            writeArray(writer, dsonValue.asArrayLite());
        } else if (dsonValue.getDsonType() == DsonType.HEADER) {
            writeHeader(writer, dsonValue.asHeaderLite());
        } else {
            throw DsonIOException.invalidTopDsonType(dsonValue.getDsonType());
        }
    }

    /** @return 如果到达文件尾部，则返回null */
    public static DsonValue readTopDsonValue(DsonLiteReader reader) {
        return readTopDsonValue(reader, null);
    }

    /**
     * @param fileHeader 用于接收文件头信息;如果读取到header，则存储给定参数中，并返回给定对象
     * @return 如果到达文件尾部，则返回null
     */
    public static DsonValue readTopDsonValue(DsonLiteReader reader, DsonHeader<FieldNumber> fileHeader) {
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
    public static void writeObject(DsonLiteWriter writer, DsonObject<FieldNumber> dsonObject) {
        writer.writeStartObject();
        if (!dsonObject.getHeader().isEmpty()) {
            writeHeader(writer, dsonObject.getHeader());
        }
        dsonObject.forEach((name, dsonValue) -> writeDsonValue(writer, dsonValue, name.getFullNumber()));
        writer.writeEndObject();
    }

    public static DsonObject<FieldNumber> readObject(DsonLiteReader reader) {
        DsonObject<FieldNumber> dsonObject = new DsonObject<>();
        DsonType dsonType;
        FieldNumber name;
        DsonValue value;
        reader.readStartObject();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            if (dsonType == DsonType.HEADER) {
                readHeader(reader, dsonObject.getHeader());
            } else {
                name = FieldNumber.ofFullNumber(reader.readName());
                value = readDsonValue(reader);
                dsonObject.put(name, value);
            }
        }
        reader.readEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void writeArray(DsonLiteWriter writer, DsonArray<FieldNumber> dsonArray) {
        writer.writeStartArray();
        if (!dsonArray.getHeader().isEmpty()) {
            writeHeader(writer, dsonArray.getHeader());
        }
        for (DsonValue dsonValue : dsonArray) {
            writeDsonValue(writer, dsonValue, 0);
        }
        writer.writeEndArray();
    }

    public static DsonArray<FieldNumber> readArray(DsonLiteReader reader) {
        DsonArray<FieldNumber> dsonArray = new DsonArray<>();
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

    public static void writeHeader(DsonLiteWriter writer, DsonHeader<FieldNumber> header) {
        writer.writeStartHeader();
        header.forEach((key, value) -> writeDsonValue(writer, value, key.getFullNumber()));
        writer.writeEndHeader();
    }

    public static DsonHeader<FieldNumber> readHeader(DsonLiteReader reader, @Nullable DsonHeader<FieldNumber> header) {
        if (header == null) header = new DsonHeader<>();
        DsonType dsonType;
        FieldNumber name;
        DsonValue value;
        reader.readStartHeader();
        while ((dsonType = reader.readDsonType()) != DsonType.END_OF_OBJECT) {
            assert dsonType != DsonType.HEADER;
            name = FieldNumber.ofFullNumber(reader.readName());
            value = readDsonValue(reader);
            header.put(name, value);
        }
        reader.readEndHeader();
        return header;
    }

    public static void writeDsonValue(DsonLiteWriter writer, DsonValue dsonValue, int name) {
        if (writer.isAtName()) {
            writer.writeName(name);
        }
        switch (dsonValue.getDsonType()) {
            case INT32 -> writer.writeInt32(name, dsonValue.asInt32(), WireType.VARINT);
            case INT64 -> writer.writeInt64(name, dsonValue.asInt64(), WireType.VARINT);
            case FLOAT -> writer.writeFloat(name, dsonValue.asFloat());
            case DOUBLE -> writer.writeDouble(name, dsonValue.asDouble());
            case BOOL -> writer.writeBool(name, dsonValue.asBool());
            case STRING -> writer.writeString(name, dsonValue.asString());
            case NULL -> writer.writeNull(name);
            case BINARY -> writer.writeBinary(name, dsonValue.asBinary());
            case POINTER -> writer.writePtr(name, dsonValue.asPointer());
            case LITE_POINTER -> writer.writeLitePtr(name, dsonValue.asLitePointer());
            case DATETIME -> writer.writeDateTime(name, dsonValue.asDateTime());
            case TIMESTAMP -> writer.writeTimestamp(name, dsonValue.asTimestamp());
            case HEADER -> writeHeader(writer, dsonValue.asHeaderLite());
            case ARRAY -> writeArray(writer, dsonValue.asArrayLite());
            case OBJECT -> writeObject(writer, dsonValue.asObjectLite());
            case END_OF_OBJECT -> throw new AssertionError();
        }
    }

    public static DsonValue readDsonValue(DsonLiteReader reader) {
        DsonType dsonType = reader.getCurrentDsonType();
        reader.skipName();
        final int name = 0;
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
                DsonHeader<FieldNumber> header = new DsonHeader<>();
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
                DsonObject<FieldNumber> src = dsonValue.asObjectLite();
                DsonObject<FieldNumber> result = new DsonObject<>(src.size());
                copyKVPair(src.getHeader(), result.getHeader(), stack);
                copyKVPair(src, result, stack);
                return result;
            }
            case ARRAY -> {
                DsonArray<FieldNumber> src = dsonValue.asArrayLite();
                DsonArray<FieldNumber> result = new DsonArray<>(src.size());
                copyKVPair(src.getHeader(), result.getHeader(), stack);
                copyElements(src, result, stack);
                return result;
            }
            case HEADER -> {
                DsonHeader<FieldNumber> src = dsonValue.asHeaderLite();
                DsonHeader<FieldNumber> result = new DsonHeader<>();
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

    private static void copyKVPair(AbstractDsonObject<FieldNumber> src, AbstractDsonObject<FieldNumber> dest, int stack) {
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
}