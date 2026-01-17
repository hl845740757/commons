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

    public static void writeTimestamp(DsonOutput output, Timestamp Timestamp) {
        output.writeUInt64(Timestamp.getSeconds());
        output.writeUInt32(Timestamp.getNanos());
    }

    public static Timestamp readTimestamp(DsonInput input) {
        return new Timestamp(
                input.readUInt64(),
                input.readUInt32());
    }

    public static int wireTypeOfDouble4(Double4 double4) {
        int v = 0;
        if (double4.v1 != 0) v |= 0x01;
        if (double4.v2 != 0) v |= 0x02;
        if (double4.v3 != 0) v |= 0x04;
        return v;
    }

    public static void writeDouble4(DsonOutput output, Double4 double4) {
        // V0固定写入，其它三个非0时写入
        output.writeDouble(double4.v0);
        if (double4.v1 != 0) {
            output.writeDouble(double4.v1);
        }
        if (double4.v2 != 0) {
            output.writeDouble(double4.v2);
        }
        if (double4.v3 != 0) {
            output.writeDouble(double4.v3);
        }
    }

    public static Double4 readDouble4(DsonInput input, int wireTypeBits) {
        double v0 = input.readDouble();
        double v1 = (wireTypeBits & 0x01) != 0 ? input.readDouble() : 0;
        double v2 = (wireTypeBits & 0x02) != 0 ? input.readDouble() : 0;
        double v3 = (wireTypeBits & 0x04) != 0 ? input.readDouble() : 0;
        return new Double4(v0, v1, v2, v3);
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
            case INT64 -> {
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
                input.readUInt64(); // localId
                if ((wireTypeBits & ObjectPtr.MASK_COLLECTION) != 0) {
                    skip = input.readUInt32(); // collection长度
                    input.skipRawBytes(skip);
                }
                if ((wireTypeBits & ObjectPtr.MASK_LOCAL_PATH) != 0) {
                    skip = input.readUInt32(); // localPath长度
                    input.skipRawBytes(skip);
                }
                if ((wireTypeBits & ObjectPtr.MASK_TYPE) != 0) {
                    input.readUInt32();
                }
                return;
            }
            case DATETIME -> {
                input.readUInt64();
                input.readUInt32();
                input.readSInt32();
//                input.readRawByte(); // 已转移到wireTypeBits
                return;
            }
            case TIMESTAMP -> {
                input.readUInt64();
                input.readUInt32();
                return;
            }
            case DOUBLE4 -> {
                input.readDouble();
                if ((wireTypeBits & 0x01) != 0) input.readDouble();
                if ((wireTypeBits & 0x02) != 0) input.readDouble();
                if ((wireTypeBits & 0x04) != 0) input.readDouble();
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