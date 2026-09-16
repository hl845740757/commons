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

    public static boolean readBool(DsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType for bool, bits: " + wireTypeBits);
    }

    // region binary

    public static void writeBinary(DsonOutput output, Binary binary) {
        output.writeUInt32(binary.length());
        output.writeRawBytes(binary.unsafeBuffer());
    }

    public static void writeBinary(DsonOutput output, byte[] bytes, int offset, int len) {
        output.writeUInt32(len);
        output.writeRawBytes(bytes, offset, len);
    }

    public static Binary readBinary(DsonInput input) {
        int size = input.readUInt32();
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
        if (objectPtr.hasLocalPath()) {
            v |= ObjectPtr.MASK_LOCAL_PATH;
        }
        if (objectPtr.hasCollection()) {
            v |= ObjectPtr.MASK_COLLECTION;
        }
        if (objectPtr.getType() != 0) {
            v |= ObjectPtr.MASK_TYPE;
        }
        return v;
    }

    public static void writePtr(DsonOutput output, ObjectPtr objectPtr) {
        output.writeUInt64(objectPtr.getLocalId());
        if (objectPtr.hasCollection()) {
            output.writeString(objectPtr.getCollection());
        }
        if (objectPtr.hasLocalPath()) {
            output.writeString(objectPtr.getLocalPath());
        }
        if (objectPtr.getType() != 0) {
            output.writeUInt32(objectPtr.getType());
        }
    }

    public static ObjectPtr readPtr(DsonInput input, int wireTypeBits) {
        long localId = input.readUInt64();
        String colletion = (wireTypeBits & ObjectPtr.MASK_COLLECTION) != 0 ? input.readString() : null;
        String localPath = (wireTypeBits & ObjectPtr.MASK_LOCAL_PATH) != 0 ? input.readString() : null;
        int type = (wireTypeBits & ObjectPtr.MASK_TYPE) != 0 ? input.readUInt32() : 0;
        return new ObjectPtr(colletion, localPath, localId, type);
    }

    private static void skipPtr(DsonInput input, int wireTypeBits) {
        input.readUInt64();
        if ((wireTypeBits & ObjectPtr.MASK_COLLECTION) != 0) {
            int skip = input.readUInt32(); // collection长度
            input.skipRawBytes(skip);
        }
        if ((wireTypeBits & ObjectPtr.MASK_LOCAL_PATH) != 0) {
            int skip = input.readUInt32(); // localPath长度
            input.skipRawBytes(skip);
        }
        if ((wireTypeBits & ObjectPtr.MASK_TYPE) != 0) {
            input.readUInt32();
        }
    }

    public static void writeDateTime(DsonOutput output, ExtDateTime dateTime) {
        output.writeUInt64(dateTime.getSeconds());
        output.writeUInt32(dateTime.getNanos());
        output.writeSInt32(dateTime.getOffset());
//        output.writeRawByte(dateTime.getEnables());
    }

    public static ExtDateTime readDateTime(DsonInput input, int wireTypeBits) {
        return new ExtDateTime(
                input.readUInt64(),
                input.readUInt32(),
                input.readSInt32(),
                (byte) wireTypeBits);
    }

    private static void skipDateTime(DsonInput input) {
        input.readUInt64();
        input.readUInt32();
        input.readSInt32();
    }

    public static void writeTimestamp(DsonOutput output, Timestamp Timestamp) {
        output.writeUInt64(Timestamp.getSeconds());
        output.writeUInt32(Timestamp.getNanos());
    }

    public static Timestamp readTimestamp(DsonInput input) {
        return new Timestamp(
                input.readUInt64(),
                input.readUInt32());
    }

    private static void skipTimestamp(DsonInput input) {
        input.readUInt64();
        input.readUInt32();
    }

    public static void writeDouble4(DsonOutput output, Double4 value) {
        WireType w0 = WireType.bestOfDouble(value.v0);
        WireType w1 = WireType.bestOfDouble(value.v1);
        WireType w2 = WireType.bestOfDouble(value.v2);
        WireType w3 = WireType.bestOfDouble(value.v3);
        int mask = w0.getNumber() | (w1.getNumber() << 2) | (w2.getNumber() << 4) | (w3.getNumber() << 6);
        output.writeRawByte((byte) mask);
        w0.writeDouble(output, value.v0);
        w1.writeDouble(output, value.v1);
        w2.writeDouble(output, value.v2);
        w3.writeDouble(output, value.v3);
    }

    public static Double4 readDouble4(DsonInput input, int wireTypeBits) {
        if (wireTypeBits != 0) throw new DsonIOException("invalid wireType for double4, bits: " + wireTypeBits);
        int mask = input.readRawByte();
        WireType w0 = WireType.forNumber(mask & 3);
        WireType w1 = WireType.forNumber((mask >> 2) & 3);
        WireType w2 = WireType.forNumber((mask >> 4) & 3);
        WireType w3 = WireType.forNumber((mask >> 6) & 3);
        return new Double4(w0.readDouble(input), w1.readDouble(input), w2.readDouble(input), w3.readDouble(input));
    }

    private static void skipDouble4(DsonInput input, int wireTypeBits) {
        readDouble4(input, wireTypeBits);
    }

    public static void writeLong4(DsonOutput output, Long4 value) {
        WireType w0 = WireType.bestOfInt64(value.v0);
        WireType w1 = WireType.bestOfInt64(value.v1);
        WireType w2 = WireType.bestOfInt64(value.v2);
        WireType w3 = WireType.bestOfInt64(value.v3);
        int mask = w0.getNumber() | (w1.getNumber() << 2) | (w2.getNumber() << 4) | (w3.getNumber() << 6);
        output.writeRawByte((byte) mask);
        w0.writeInt64(output, value.v0);
        w1.writeInt64(output, value.v1);
        w2.writeInt64(output, value.v2);
        w3.writeInt64(output, value.v3);
    }

    public static Long4 readLong4(DsonInput input, int wireTypeBits) {
        if (wireTypeBits != 0) throw new DsonIOException("invalid wireType for long4, bits: " + wireTypeBits);
        int mask = input.readRawByte();
        WireType w0 = WireType.forNumber(mask & 3);
        WireType w1 = WireType.forNumber((mask >> 2) & 3);
        WireType w2 = WireType.forNumber((mask >> 4) & 3);
        WireType w3 = WireType.forNumber((mask >> 6) & 3);
        return new Long4(w0.readInt64(input), w1.readInt64(input), w2.readInt64(input), w3.readInt64(input));
    }

    private static void skipLong4(DsonInput input, int wireTypeBits) {
        readLong4(input, wireTypeBits);
    }

    public static void writeFxp4(DsonOutput output, Fxp4 fv4) {
        WireType w0 = WireType.bestOfInt64(fv4.get(0).rawValue);
        WireType w1 = WireType.bestOfInt64(fv4.get(1).rawValue);
        WireType w2 = WireType.bestOfInt64(fv4.get(2).rawValue);
        WireType w3 = WireType.bestOfInt64(fv4.get(3).rawValue);
        int mask = w0.getNumber() | (w1.getNumber() << 2) | (w2.getNumber() << 4) | (w3.getNumber() << 6);
        //
        output.writeRawByte((byte) mask);
        w0.writeInt64(output, fv4.get(0).rawValue);
        w1.writeInt64(output, fv4.get(1).rawValue);
        w2.writeInt64(output, fv4.get(2).rawValue);
        w3.writeInt64(output, fv4.get(3).rawValue);
    }

    public static Fxp4 readFxp4(DsonInput input, int wireTypeBits) {
        if (wireTypeBits != 0) {
            throw new DsonIOException("invalid wireType for fv4, bits: " + wireTypeBits);
        }
        int mask = Byte.toUnsignedInt(input.readRawByte());
        WireType w0 = WireType.forNumber(mask & 3);
        WireType w1 = WireType.forNumber((mask >>> 2) & 3);
        WireType w2 = WireType.forNumber((mask >>> 4) & 3);
        WireType w3 = WireType.forNumber((mask >>> 6) & 3);
        //
        Fxp64 v0 = Fxp64.fromRaw(w0.readInt64(input));
        Fxp64 v1 = Fxp64.fromRaw(w1.readInt64(input));
        Fxp64 v2 = Fxp64.fromRaw(w2.readInt64(input));
        Fxp64 v3 = Fxp64.fromRaw(w3.readInt64(input));
        return new Fxp4(v0, v1, v2, v3);
    }

    private static void skipFv4(DsonInput input, int wireTypeBits) {
        int mask = Byte.toUnsignedInt(input.readRawByte());
        WireType w0 = WireType.forNumber(mask & 3);
        WireType w1 = WireType.forNumber((mask >>> 2) & 3);
        WireType w2 = WireType.forNumber((mask >>> 4) & 3);
        WireType w3 = WireType.forNumber((mask >>> 6) & 3);
        //
        w0.readInt64(input);
        w1.readInt64(input);
        w2.readInt64(input);
        w3.readInt64(input);
    }

    // endregion

    // region 特殊
    public static void writeValueBytes(DsonOutput output, DsonType dsonType, byte[] data) {
        if (dsonType == DsonType.STRING || dsonType == DsonType.BINARY) {
            output.writeUInt32(data.length);
        } else {
            output.writeFixed32(data.length);
        }
        output.writeRawBytes(data);
    }

    public static byte[] readValueAsBytes(DsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.STRING || dsonType == DsonType.BINARY) {
            size = input.readUInt32();
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
            case INT32 -> {
                wireType.readInt32(input);
                return;
            }
            case INT64, FXP64 -> {
                wireType.readInt64(input);
                return;
            }
            case FLOAT -> {
                wireType.readFloat(input);
                return;
            }
            case DOUBLE -> {
                wireType.readDouble(input);
                return;
            }
            case BOOL, NULL -> {
                return;
            }
            case STRING -> {
                skip = input.readUInt32();  // string长度
            }
            case BINARY -> {
                skip = input.readUInt32(); // length(data)
            }
            case POINTER -> {
                skipPtr(input, wireTypeBits);
                return;
            }
            case DATETIME -> {
                skipDateTime(input);
                return;
            }
            case TIMESTAMP -> {
                skipTimestamp(input);
                return;
            }
            case DOUBLE4 -> {
                skipDouble4(input, wireTypeBits);
                return;
            }
            case LONG4 -> {
                skipLong4(input, wireTypeBits);
                return;
            }
            case FXP4 -> {
                skipFv4(input, wireTypeBits);
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