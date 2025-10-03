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
import cn.wjybxx.dson.types.Binary;
import cn.wjybxx.dson.types.ExtDateTime;
import cn.wjybxx.dson.types.ObjectPtr;
import cn.wjybxx.dson.types.Timestamp;

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
        if (objectPtr.hasLocalName()) {
            v |= ObjectPtr.MASK_LOCAL_NAME;
        }
        if (objectPtr.hasNamespace()) {
            v |= ObjectPtr.MASK_NAMESPACE;
        }
        if (objectPtr.getType() != 0) {
            v |= ObjectPtr.MASK_TYPE;
        }
        return v;
    }

    public static void writePtr(DsonOutput output, ObjectPtr objectPtr) {
        output.writeUInt64(objectPtr.getLocalId());
        if (objectPtr.hasLocalName()) {
            output.writeString(objectPtr.getLocalName());
        }
        if (objectPtr.hasNamespace()) {
            output.writeString(objectPtr.getNamespace());
        }
        if (objectPtr.getType() != 0) {
            output.writeUInt32(objectPtr.getType());
        }
    }

    public static ObjectPtr readPtr(DsonInput input, int wireTypeBits) {
        long localId = input.readUInt64();
        String localName = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_LOCAL_NAME) ? input.readString() : null;
        String namespace = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_NAMESPACE) ? input.readString() : null;
        int type = DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_TYPE) ? input.readUInt32() : 0;
        return new ObjectPtr(localId, localName, namespace, type);
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
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_LOCAL_NAME)) {
                    skip = input.readUInt32(); // localName长度
                    input.skipRawBytes(skip);
                }
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_NAMESPACE)) {
                    skip = input.readUInt32(); // namespace长度
                    input.skipRawBytes(skip);
                }
                if (DsonInternals.isSet(wireTypeBits, ObjectPtr.MASK_TYPE)) {
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