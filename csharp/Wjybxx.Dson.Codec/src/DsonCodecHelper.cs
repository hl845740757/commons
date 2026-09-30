#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
internal static class DsonCodecHelper
{
    public static int ReadInt(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32();
            case DsonType.Int64: return (int)reader.ReadInt64();
            case DsonType.Float: return (int)reader.ReadFloat();
            case DsonType.Double: return (int)reader.ReadDouble();
            case DsonType.Bool: return reader.ReadBool() ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull();
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(int), dsonType);
        }
    }

    public static long ReadLong(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32();
            case DsonType.Int64: return reader.ReadInt64();
            case DsonType.Float: return (long)reader.ReadFloat();
            case DsonType.Double: return (long)reader.ReadDouble();
            case DsonType.Fxp64: return reader.ReadFxp64().rawValue;
            case DsonType.Bool: return reader.ReadBool() ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull();
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(long), dsonType);
        }
    }

    public static float ReadFloat(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32();
            case DsonType.Int64: return reader.ReadInt64();
            case DsonType.Float: return reader.ReadFloat();
            case DsonType.Double: return (float)reader.ReadDouble();
            case DsonType.Bool: return reader.ReadBool() ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull();
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(float), dsonType);
        }
    }

    public static double ReadDouble(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32();
            case DsonType.Int64: return reader.ReadInt64();
            case DsonType.Float: return reader.ReadFloat();
            case DsonType.Double: return reader.ReadDouble();
            case DsonType.Fxp64: return reader.ReadFxp64().ToDouble();
            case DsonType.Bool: return reader.ReadBool() ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull();
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(double), dsonType);
        }
    }

    public static Fxp64 ReadFxp64(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Fxp64: return reader.ReadFxp64();
            case DsonType.Int32: return Fxp64.FromRaw(reader.ReadInt32());
            case DsonType.Int64: return Fxp64.FromRaw(reader.ReadInt64());
            case DsonType.Float: return Fxp64.FromDouble(reader.ReadFloat());
            case DsonType.Double: return Fxp64.FromDouble(reader.ReadDouble());
            case DsonType.String: return Fxp64.Parse(reader.ReadString());
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Fxp64), dsonType);
        }
    }

    public static bool ReadBool(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32() != 0;
            case DsonType.Int64: return reader.ReadInt64() != 0;
            case DsonType.Float: return reader.ReadFloat() != 0;
            case DsonType.Double: return reader.ReadDouble() != 0;
            case DsonType.Bool: return reader.ReadBool();
            case DsonType.String: {
                string value = reader.ReadString();
                return value == "1" || value == "true";
            }
            case DsonType.Null: {
                reader.ReadNull();
                return false;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(bool), dsonType);
        }
    }

    public static string? ReadString(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.String: return reader.ReadString();
            case DsonType.Binary: return reader.ReadBinary().ToHexString(); // 可以接收二进制
            case DsonType.Null: {
                reader.ReadNull();
                return null;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(string), dsonType);
        }
    }

    public static void ReadNull(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        if (dsonType != DsonType.Null) {
            throw DsonCodecException.Incompatible(DsonType.Null, dsonType);
        }
        reader.ReadNull();
    }

    public static Binary? ReadBinary(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Binary: return reader.ReadBinary();
            case DsonType.String: {
                string str = reader.ReadString();
                byte[] bytes = ObjectUtil.GetUtf8Bytes(str);
                return Binary.Wrap(bytes);
            }
            case DsonType.Null: {
                reader.ReadNull();
                return null;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Binary), dsonType);
        }
    }

    public static ObjectPtr ReadPtr(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return new ObjectPtr(reader.ReadInt32());
            case DsonType.Int64: return new ObjectPtr(reader.ReadInt64());
            case DsonType.Pointer: return reader.ReadPtr();
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(ObjectPtr), dsonType);
        }
    }

    public static ExtDateTime ReadDateTime(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return new ExtDateTime(reader.ReadInt32());
            case DsonType.Int64: return new ExtDateTime(reader.ReadInt64());
            case DsonType.DateTime: return reader.ReadDateTime();
            case DsonType.Timestamp: {
                Timestamp ts = reader.ReadTimestamp();
                return new ExtDateTime(ts.Seconds, ts.Nanos);
            }
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(ExtDateTime), dsonType);
        }
    }

    public static Timestamp ReadTimestamp(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return new Timestamp(reader.ReadInt32());
            case DsonType.Int64: return new Timestamp(reader.ReadInt64());
            case DsonType.Timestamp: return reader.ReadTimestamp();
            case DsonType.DateTime: {
                ExtDateTime dt = reader.ReadDateTime();
                return new Timestamp(dt.Seconds, dt.Nanos);
            }
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Timestamp), dsonType);
        }
    }

    public static Double4 ReadDouble4(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Double4: return reader.ReadDouble4();
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Double4), dsonType);
        }
    }

    public static Long4 ReadLong4(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Long4: return reader.ReadLong4();
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Long4), dsonType);
        }
    }

    public static Fxp4 ReadFxp4(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Fxp4: return reader.ReadFxp4();
            case DsonType.Null: {
                reader.ReadNull();
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Fxp4), dsonType);
        }
    }

    public static object? ReadDsonValueValue(IDsonReader<string> reader) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32();
            case DsonType.Int64: return reader.ReadInt64();
            case DsonType.Float: return reader.ReadFloat();
            case DsonType.Double: return reader.ReadDouble();
            case DsonType.Fxp64: return reader.ReadFxp64();
            case DsonType.Bool: return reader.ReadBool();
            case DsonType.String: return reader.ReadString();
            case DsonType.Binary: return reader.ReadBinary();
            case DsonType.Pointer: return reader.ReadPtr();
            case DsonType.DateTime: return reader.ReadDateTime();
            case DsonType.Timestamp: return reader.ReadTimestamp();
            case DsonType.Double4: return reader.ReadDouble4();
            case DsonType.Long4: return reader.ReadLong4();
            case DsonType.Fxp4: return reader.ReadFxp4();
            case DsonType.Null: {
                reader.ReadNull();
                return null;
            }
            default: throw new AssertionError(dsonType.ToString()); // null和容器都前面测试了
        }
    }
}
}