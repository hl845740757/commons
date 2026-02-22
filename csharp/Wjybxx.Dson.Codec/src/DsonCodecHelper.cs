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
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
internal static class DsonCodecHelper
{
    #region reader

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DsonType ReadOrGetDsonType(IDsonReader<string> reader) {
        return reader.IsAtType ? reader.ReadDsonType() : reader.CurrentDsonType;
    }

    public static int ReadInt(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32(name);
            case DsonType.Int64: return (int)reader.ReadInt64(name);
            case DsonType.Float: return (int)reader.ReadFloat(name);
            case DsonType.Double: return (int)reader.ReadDouble(name);
            case DsonType.Bool: return reader.ReadBool(name) ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull(name);
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(int), dsonType);
        }
    }

    public static long ReadLong(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32(name);
            case DsonType.Int64: return reader.ReadInt64(name);
            case DsonType.Float: return (long)reader.ReadFloat(name);
            case DsonType.Double: return (long)reader.ReadDouble(name);
            case DsonType.Bool: return reader.ReadBool(name) ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull(name);
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(long), dsonType);
        }
    }

    public static float ReadFloat(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32(name);
            case DsonType.Int64: return reader.ReadInt64(name);
            case DsonType.Float: return reader.ReadFloat(name);
            case DsonType.Double: return (float)reader.ReadDouble(name);
            case DsonType.Bool: return reader.ReadBool(name) ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull(name);
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(float), dsonType);
        }
    }

    public static double ReadDouble(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32(name);
            case DsonType.Int64: return reader.ReadInt64(name);
            case DsonType.Float: return reader.ReadFloat(name);
            case DsonType.Double: return reader.ReadDouble(name);
            case DsonType.Bool: return reader.ReadBool(name) ? 1 : 0;
            case DsonType.Null: {
                reader.ReadNull(name);
                return 0;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(double), dsonType);
        }
    }

    public static bool ReadBool(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32(name) != 0;
            case DsonType.Int64: return reader.ReadInt64(name) != 0;
            case DsonType.Float: return reader.ReadFloat(name) != 0;
            case DsonType.Double: return reader.ReadDouble(name) != 0;
            case DsonType.Bool: return reader.ReadBool(name);
            case DsonType.String: {
                string value = reader.ReadString(name);
                return value == "true" || value == "1";
            }
            case DsonType.Null: {
                reader.ReadNull(name);
                return false;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(bool), dsonType);
        }
    }

    public static string? ReadString(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.String: return reader.ReadString(name);
            case DsonType.Binary: return reader.ReadBinary(name).ToHexString();
            case DsonType.Null: {
                reader.ReadNull(name);
                return null;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(string), dsonType);
        }
    }

    public static void ReadNull(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        if (dsonType != DsonType.Null) {
            throw DsonCodecException.Incompatible(DsonType.Null, dsonType);
        }
        reader.ReadNull(name);
    }

    public static Binary? ReadBinary(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Binary: return reader.ReadBinary(name);
            case DsonType.Null: {
                reader.ReadNull(name);
                return null;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Binary), dsonType);
        }
    }

    public static ObjectPtr ReadPtr(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int32: return new ObjectPtr(reader.ReadInt32());
            case DsonType.Int64: return new ObjectPtr(reader.ReadInt64());
            case DsonType.Pointer: return reader.ReadPtr(name);
            case DsonType.Null: {
                reader.ReadNull(name);
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(ObjectPtr), dsonType);
        }
    }

    public static ExtDateTime ReadDateTime(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int64: return new ExtDateTime(reader.ReadInt64(name));
            case DsonType.String: return ExtDateTime.Parse(reader.ReadString(name));
            case DsonType.DateTime: return reader.ReadDateTime(name);
            case DsonType.Null: {
                reader.ReadNull(name);
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(ExtDateTime), dsonType);
        }
    }

    public static Timestamp ReadTimestamp(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Int64: return new Timestamp(reader.ReadInt64(name));
            case DsonType.Timestamp: return reader.ReadTimestamp(name);
            case DsonType.Null: {
                reader.ReadNull(name);
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Timestamp), dsonType);
        }
    }

    public static Double4 ReadDouble4(IDsonReader<string> reader, string? name) {
        DsonType dsonType = ReadOrGetDsonType(reader);
        switch (dsonType) {
            case DsonType.Double4: return reader.ReadDouble4(name);
            case DsonType.Null: {
                reader.ReadNull(name);
                return default;
            }
            default:
                throw DsonCodecException.Incompatible(typeof(Double4), dsonType);
        }
    }

    public static object? ReadDsonValueValue(IDsonReader<string> reader, string? name) {
        DsonType dsonType = reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Int32: return reader.ReadInt32(name);
            case DsonType.Int64: return reader.ReadInt64(name);
            case DsonType.Float: return reader.ReadFloat(name);
            case DsonType.Double: return reader.ReadDouble(name);
            case DsonType.Bool: return reader.ReadBool(name);
            case DsonType.String: return reader.ReadString(name);
            case DsonType.Binary: return reader.ReadBinary(name);
            case DsonType.Pointer: return reader.ReadPtr(name);
            case DsonType.DateTime: return reader.ReadDateTime(name);
            case DsonType.Timestamp: return reader.ReadTimestamp(name);
            case DsonType.Double4: return reader.ReadDouble4(name);
            case DsonType.Null: {
                reader.ReadNull(name);
                return null;
            }
            default: throw new AssertionError(dsonType.ToString()); // null和容器都前面测试了
        }
    }

    public static bool IsReadAsImmutable(DeserializeFeatures features, IDsonObjectReader reader) {
        if ((features & DeserializeFeatures.ReadAsImmutable) != 0) return true;
        TypeMeta? typeMeta = reader.ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.decodeFeatures;
            if ((features & DeserializeFeatures.ReadAsImmutable) != 0) return true;
        }
        features = reader.Options.decodeFeatures;
        return (features & DeserializeFeatures.ReadAsImmutable) != 0;
    }

    #endregion
}
}