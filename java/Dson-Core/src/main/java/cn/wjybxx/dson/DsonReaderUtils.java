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

import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.types.*;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/5/31
 */
public class DsonReaderUtils {

    /** 支持读取为bytes和直接写入bytes的数据类型 -- 这些类型不可以存储额外数据在WireType上 */
    public static final List<DsonType> VALUE_BYTES_TYPES = List.of(DsonType.STRING,
            DsonType.BINARY, DsonType.ARRAY, DsonType.OBJECT, DsonType.HEADER);

    // region number

    /**
     * 1.浮点数的前16位固定写入，因此只统计后16位
     * 2.wireType表示后导0对应的字节数
     * 3.该算法对于整数有很好的收益，对于小数收益较低
     * 4.由于编码依赖了上层的wireType比特位，因此不能写在Output接口中
     */
    public static int wireTypeOfFloat(float value) {
        int rawBits = Float.floatToRawIntBits(value);
        if ((rawBits & 0xFF) != 0) return 0;
        if ((rawBits & 0xFF00) != 0) return 1;
        return 2;
    }

    /** 小端编码，从末尾非0开始写入 */
    public static void writeFloat(DsonOutput output, float value, int wireType) {
        if (wireType == 0) {
            output.writeFloat(value);
            return;
        }

        int rawBits = Float.floatToRawIntBits(value);
        for (int i = 0; i < wireType; i++) {
            rawBits = rawBits >>> 8;
        }
        for (int i = wireType; i < 4; i++) {
            output.writeRawByte((byte) rawBits);
            rawBits = rawBits >>> 8;
        }
    }

    public static float readFloat(DsonInput input, int wireType) {
        if (wireType == 0) {
            return input.readFloat();
        }

        int rawBits = 0;
        for (int i = wireType; i < 4; i++) {
            rawBits |= (input.readRawByte() & 0XFF) << (8 * i);
        }
        return Float.intBitsToFloat(rawBits);
    }

    /**
     * 1.浮点数的前16位固定写入，因此只统计后48位
     * 2.wireType表示后导0对应的字节数
     * 3.该算法对于整数有很好的收益，对于小数收益较低
     */
    public static int wireTypeOfDouble(double value) {
        long rawBits = Double.doubleToRawLongBits(value);
        if ((rawBits & 0xFFL) != 0) return 0;
        if ((rawBits & 0xFF00L) != 0) return 1;
        if ((rawBits & 0xFF_0000L) != 0) return 2;
        if ((rawBits & 0xFF00_0000L) != 0) return 3;
        if ((rawBits & 0xFF_0000_0000L) != 0) return 4;
        if ((rawBits & 0xFF00_0000_0000L) != 0) return 5;
        return 6;
    }

    public static void writeDouble(DsonOutput output, double value, int wireType) {
        if (wireType == 0) {
            output.writeDouble(value);
            return;
        }

        long rawBits = Double.doubleToRawLongBits(value);
        for (int i = 0; i < wireType; i++) {
            rawBits = rawBits >>> 8;
        }
        for (int i = wireType; i < 8; i++) {
            output.writeRawByte((byte) rawBits);
            rawBits = rawBits >>> 8;
        }
    }

    public static double readDouble(DsonInput input, int wireType) {
        if (wireType == 0) {
            return input.readDouble();
        }

        long rawBits = 0;
        for (int i = wireType; i < 8; i++) {
            rawBits |= (input.readRawByte() & 0XFFL) << (8 * i);
        }
        return Double.longBitsToDouble(rawBits);
    }

    public static boolean readBool(DsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType for bool, bits: " + wireTypeBits);
    }
    // endregion

    // region binary

    public static void writeBinary(DsonOutput output, Binary binary) {
        output.writeUint32(binary.length());
        output.writeRawBytes(binary.unsafeBuffer());
    }

    public static void writeBinary(DsonOutput output, byte[] bytes, int offset, int len) {
        output.writeUint32(len);
        output.writeRawBytes(bytes, offset, len);
    }

    public static Binary readBinary(DsonInput input) {
        int size = input.readUint32();
        int oldLimit = input.pushLimit(size);
        Binary binary;
        {
            binary = Binary.unsafeWrap(input.readRawBytes(size));
        }
        input.popLimit(oldLimit);
        return binary;
    }

    // endregion

    // region 内置结构体
    public static int wireTypeOfPtr(ObjectPtr objectPtr) {
        int v = 0;
        if (objectPtr.hasNamespace()) {
            v |= ObjectPtr.MASK_NAMESPACE;
        }
        if (objectPtr.getType() != 0) {
            v |= ObjectPtr.MASK_TYPE;
        }
        if (objectPtr.getPolicy() != 0) {
            v |= ObjectPtr.MASK_POLICY;
        }
        return v;
    }

    public static void writePtr(DsonOutput output, ObjectPtr objectPtr) {
        output.writeString(objectPtr.getLocalId());
        if (objectPtr.hasNamespace()) {
            output.writeString(objectPtr.getNamespace());
        }
        if (objectPtr.getType() != 0) {
            output.writeRawByte(objectPtr.getType());
        }
        if (objectPtr.getPolicy() != 0) {
            output.writeRawByte(objectPtr.getPolicy());
        }
    }

    public static ObjectPtr readPtr(DsonInput input, int wireTypeBits) {
        String localId = input.readString();
        String namespace = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_NAMESPACE) ? input.readString() : null;
        byte type = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_TYPE) ? input.readRawByte() : (byte) 0;
        byte policy = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_POLICY) ? input.readRawByte() : (byte) 0;
        return new ObjectPtr(localId, namespace, type, policy);
    }

    public static int wireTypeOfLitePtr(ObjectLitePtr objectLitePtr) {
        int v = 0;
        if (objectLitePtr.hasNamespace()) {
            v |= ObjectPtr.MASK_NAMESPACE;
        }
        if (objectLitePtr.getType() != 0) {
            v |= ObjectPtr.MASK_TYPE;
        }
        if (objectLitePtr.getPolicy() != 0) {
            v |= ObjectPtr.MASK_POLICY;
        }
        return v;
    }

    public static void writeLitePtr(DsonOutput output, ObjectLitePtr objectLitePtr) {
        output.writeUint64(objectLitePtr.getLocalId());
        if (objectLitePtr.hasNamespace()) {
            output.writeString(objectLitePtr.getNamespace());
        }
        if (objectLitePtr.getType() != 0) {
            output.writeRawByte(objectLitePtr.getType());
        }
        if (objectLitePtr.getPolicy() != 0) {
            output.writeRawByte(objectLitePtr.getPolicy());
        }
    }

    public static ObjectLitePtr readLitePtr(DsonInput input, int wireTypeBits) {
        long localId = input.readUint64();
        String namespace = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_NAMESPACE) ? input.readString() : null;
        byte type = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_TYPE) ? input.readRawByte() : (byte) 0;
        byte policy = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_POLICY) ? input.readRawByte() : (byte) 0;
        return new ObjectLitePtr(localId, namespace, type, policy);
    }

    public static void writeDateTime(DsonOutput output, ExtDateTime dateTime) {
        output.writeUint64(dateTime.getSeconds());
        output.writeUint32(dateTime.getNanos());
        output.writeSint32(dateTime.getOffset());
//        output.writeRawByte(dateTime.getEnables());
    }

    public static ExtDateTime readDateTime(DsonInput input, int wireTypeBits) {
        return new ExtDateTime(
                input.readUint64(),
                input.readUint32(),
                input.readSint32(),
                (byte) wireTypeBits);
    }

    public static void writeTimestamp(DsonOutput output, Timestamp Timestamp) {
        output.writeUint64(Timestamp.getSeconds());
        output.writeUint32(Timestamp.getNanos());
    }

    public static Timestamp readTimestamp(DsonInput input) {
        return new Timestamp(
                input.readUint64(),
                input.readUint32());
    }

    // endregion

    // region 特殊
    public static void writeValueBytes(DsonOutput output, DsonType dsonType, byte[] data) {
        if (dsonType == DsonType.STRING || dsonType == DsonType.BINARY) {
            output.writeUint32(data.length);
        } else {
            output.writeFixed32(data.length);
        }
        output.writeRawBytes(data);
    }

    public static byte[] readValueAsBytes(DsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.STRING || dsonType == DsonType.BINARY) {
            size = input.readUint32();
        } else {
            size = input.readFixed32();
        }
        return input.readRawBytes(size);
    }

    public static void checkReadValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.contains(dsonType)) {
            throw DsonIOException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void checkWriteValueAsBytes(DsonType dsonType) {
        if (!VALUE_BYTES_TYPES.contains(dsonType)) {
            throw DsonIOException.invalidDsonType(VALUE_BYTES_TYPES, dsonType);
        }
    }

    public static void skipToEndOfObject(DsonInput input) {
        int size = input.getBytesUntilLimit();
        if (size > 0) {
            input.skipRawBytes(size);
        }
    }
    // endregion

    public static void skipValue(DsonInput input, DsonContextType contextType,
                                 DsonType dsonType, WireType wireType, int wireTypeBits) {
        int skip;
        switch (dsonType) {
            case FLOAT -> {
                skip = 4 - wireTypeBits;
            }
            case DOUBLE -> {
                skip = 8 - wireTypeBits;
            }
            case BOOL, NULL -> {
                return;
            }
            case INT32 -> {
                wireType.readInt32(input);
                return;
            }
            case INT64 -> {
                wireType.readInt64(input);
                return;
            }
            case STRING -> {
                skip = input.readUint32();  // string长度
            }
            case BINARY -> {
                skip = input.readUint32(); // length(data)
            }
            case POINTER -> {
                skip = input.readUint32(); // localId长度
                input.skipRawBytes(skip);

                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_NAMESPACE)) {
                    skip = input.readUint32(); // namespace长度
                    input.skipRawBytes(skip);
                }
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_TYPE)) {
                    input.readRawByte();
                }
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_POLICY)) {
                    input.readRawByte();
                }
                return;
            }
            case LITE_POINTER -> {
                input.readUint64(); // localId
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_NAMESPACE)) {
                    skip = input.readUint32(); // namespace长度
                    input.skipRawBytes(skip);
                }
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_TYPE)) {
                    input.readRawByte();
                }
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_POLICY)) {
                    input.readRawByte();
                }
                return;
            }
            case DATETIME -> {
                input.readUint64();
                input.readUint32();
                input.readSint32();
//                input.readRawByte();
                return;
            }
            case TIMESTAMP -> {
                input.readUint64();
                input.readUint32();
                return;
            }
            case HEADER -> {
                skip = input.readFixed16();
            }
            case ARRAY, OBJECT -> {
                skip = input.readFixed32();
            }
            default -> {
                throw DsonIOException.invalidDsonType(contextType, dsonType);
            }
        }
        if (skip > 0) {
            input.skipRawBytes(skip);
        }
    }

    public static DsonReaderGuide whatShouldIDo(DsonContextType contextType, DsonReaderState state) {
        if (contextType == DsonContextType.TOP_LEVEL) {
            if (state == DsonReaderState.END_OF_FILE) {
                return DsonReaderGuide.CLOSE;
            }
            if (state == DsonReaderState.VALUE) {
                return DsonReaderGuide.READ_VALUE;
            }
            return DsonReaderGuide.READ_TYPE;
        } else {
            return switch (state) {
                case TYPE -> DsonReaderGuide.READ_TYPE;
                case VALUE -> DsonReaderGuide.READ_VALUE;
                case NAME -> DsonReaderGuide.READ_NAME;
                case WAIT_START_OBJECT -> {
                    if (contextType == DsonContextType.HEADER) {
                        yield DsonReaderGuide.START_HEADER;
                    }
                    if (contextType == DsonContextType.ARRAY) {
                        yield DsonReaderGuide.START_ARRAY;
                    }
                    yield DsonReaderGuide.START_OBJECT;
                }
                case WAIT_END_OBJECT -> {
                    if (contextType == DsonContextType.HEADER) {
                        yield DsonReaderGuide.END_HEADER;
                    }
                    if (contextType == DsonContextType.ARRAY) {
                        yield DsonReaderGuide.END_ARRAY;
                    }
                    yield DsonReaderGuide.END_OBJECT;
                }
                case INITIAL, END_OF_FILE -> throw new AssertionError("invalid state " + state);
            };
        }
    }

}