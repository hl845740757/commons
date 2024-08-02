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

import cn.wjybxx.dson.DsonReader;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.types.*;

/**
 * 1.int扩展之间可以相互转换，当int的扩展不可以直接转换为其它数值类型
 * 2.long扩展之间可以相互转换，但long的扩展不可直接转换为其它数值类型
 * 3.String扩展之间也可以相互转换
 *
 * @author wjybxx
 * date - 2023/4/17
 */
final class DsonCodecHelper {

    static DsonType readOrGetDsonType(DsonReader reader) {
        if (reader.isAtType()) {
            return reader.readDsonType();
        } else {
            return reader.getCurrentDsonType();
        }
    }

    static int readInt(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> (int) reader.readInt64(name);
            case FLOAT -> (int) reader.readFloat(name);
            case DOUBLE -> (int) reader.readDouble(name);
            case BOOL -> reader.readBool(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            default -> throw DsonCodecException.incompatible(Integer.class, dsonType);
        };
    }

    static long readLong(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> (long) reader.readFloat(name);
            case DOUBLE -> (long) reader.readDouble(name);
            case BOOL -> reader.readBool(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            default -> throw DsonCodecException.incompatible(Long.class, dsonType);
        };
    }

    static float readFloat(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> reader.readFloat(name);
            case DOUBLE -> (float) reader.readDouble(name);
            case BOOL -> reader.readBool(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            default -> throw DsonCodecException.incompatible(Float.class, dsonType);
        };
    }

    static double readDouble(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> reader.readFloat(name);
            case DOUBLE -> reader.readDouble(name);
            case BOOL -> reader.readBool(name) ? 1 : 0;
            case NULL -> {
                reader.readNull(name);
                yield 0;
            }
            default -> throw DsonCodecException.incompatible(Double.class, dsonType);
        };
    }

    static boolean readBool(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name) != 0;
            case INT64 -> reader.readInt64(name) != 0;
            case FLOAT -> reader.readFloat(name) != 0;
            case DOUBLE -> reader.readDouble(name) != 0;
            case BOOL -> reader.readBool(name);
            case NULL -> {
                reader.readNull(name);
                yield false;
            }
            default -> throw DsonCodecException.incompatible(Boolean.class, dsonType);
        };
    }

    static String readString(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case STRING -> reader.readString(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(String.class, dsonType);
        };
    }

    static void readNull(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        if (dsonType != DsonType.NULL) {
            throw DsonCodecException.incompatible(DsonType.NULL, dsonType);
        }
        reader.readNull(name);
    }

    static Binary readBinary(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case BINARY -> reader.readBinary(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(Binary.class, dsonType);
        };
    }

    static ObjectPtr readPtr(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case POINTER -> reader.readPtr(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(ObjectPtr.class, dsonType);
        };
    }

    static ObjectLitePtr readLitePtr(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case LITE_POINTER -> reader.readLitePtr(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(ObjectLitePtr.class, dsonType);
        };
    }

    static ExtDateTime readDateTime(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case DATETIME -> reader.readDateTime(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(ExtDateTime.class, dsonType);
        };
    }

    static Timestamp readTimestamp(DsonReader reader, String name) {
        DsonType dsonType = readOrGetDsonType(reader);
        return switch (dsonType) {
            case TIMESTAMP -> reader.readTimestamp(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw DsonCodecException.incompatible(Timestamp.class, dsonType);
        };
    }

    //
    static Object readPrimitive(DsonReader reader, String name, Class<?> declared) {
        if (declared == int.class) {
            return readInt(reader, name);
        }
        if (declared == long.class) {
            return readLong(reader, name);
        }
        if (declared == float.class) {
            return readFloat(reader, name);
        }
        if (declared == double.class) {
            return readDouble(reader, name);
        }
        if (declared == boolean.class) {
            return readBool(reader, name);
        }
        if (declared == short.class) {
            return (short) readInt(reader, name);
        }
        if (declared == byte.class) {
            return (byte) readInt(reader, name);
        }
        if (declared == char.class) {
            return (char) readInt(reader, name);
        }
        throw DsonCodecException.unsupportedType(declared);
    }

    public static Object readDsonValue(DsonReader reader, DsonType dsonType, String name) {
        return switch (dsonType) {
            case INT32 -> reader.readInt32(name);
            case INT64 -> reader.readInt64(name);
            case FLOAT -> reader.readFloat(name);
            case DOUBLE -> reader.readDouble(name);
            case BOOL -> reader.readBool(name);
            case STRING -> reader.readString(name);
            case BINARY -> reader.readBinary(name);
            case POINTER -> reader.readPtr(name);
            case LITE_POINTER -> reader.readLitePtr(name);
            case DATETIME -> reader.readDateTime(name);
            case TIMESTAMP -> reader.readTimestamp(name);
            case NULL -> {
                reader.readNull(name);
                yield null;
            }
            default -> throw new AssertionError(dsonType); // null和容器都前面测试了
        };
    }
}