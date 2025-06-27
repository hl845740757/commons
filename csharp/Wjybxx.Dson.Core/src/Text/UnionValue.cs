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
using System.Runtime.InteropServices;
using Wjybxx.Commons;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 用于避免对值类型装箱
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct UnionValue : IEquatable<UnionValue>
{
#nullable disable
    // 值的类型 -- 偷懒方案，Object表示任意类型
    [FieldOffset(0)] public DsonType type;
    // 固定8个字节
    [FieldOffset(1)] public int iValue;
    [FieldOffset(1)] public long lValue;
    [FieldOffset(1)] public float fValue;
    [FieldOffset(1)] public double dValue;
    [FieldOffset(1)] public bool bValue;

    // 3个扩展int值，支持DateTime、TimeStamp、ObjectPtr、ObjectLitePtr
    [FieldOffset(9)] public int v2; // nanos, type
    [FieldOffset(13)] public int v3; // offset, policy
    [FieldOffset(17)] public int v4; // enables

    // 由于内存对齐的原因，引用类型需要偏移24 -- 所以上面的v4可以声明为int
    [FieldOffset(24)] public object objValue; // localId, string, bytes
    [FieldOffset(32)] public object objValue2; // namespace

    public UnionValue(DsonType type) : this() {
        this.type = type;
    }

    public UnionValue(DsonType type, object objValue) : this() {
        this.type = type;
        this.objValue = objValue;
    }

    #region converter

    public ObjectPtr ObjectPtr {
        get => new ObjectPtr((string)objValue, (string)objValue2, (byte)v2, (byte)v3);
        set {
            objValue = value.LocalId;
            objValue2 = value.Namespace; // 固定value2
            v2 = value.Type;
            v3 = value.Policy;
        }
    }

    public ObjectLitePtr ObjectLitePtr {
        get => new ObjectLitePtr(lValue, (string)objValue2, (byte)v2, (byte)v3);
        set {
            lValue = value.LocalId;
            objValue2 = value.Namespace; // 固定value2
            v2 = value.Type;
            v3 = value.Policy;
        }
    }

    public ExtDateTime DateTime {
        get => new ExtDateTime(lValue, v2, v3, (byte)v4);
        set {
            lValue = value.Seconds;
            v2 = value.Nanos;
            v3 = value.Offset;
            v4 = value.Enables;
        }
    }

    public Timestamp Timestamp {
        get => new Timestamp(lValue, v2);
        set {
            lValue = value.Seconds;
            v2 = value.Nanos;
        }
    }

    #endregion

#nullable enable

    public bool Equals(UnionValue other) {
        if (type != other.type) {
            return false;
        }
        switch (type) {
            case DsonType.EndOfObject: return true;
            case DsonType.Int32: return iValue == other.iValue;
            case DsonType.Int64: return lValue == other.lValue;
            case DsonType.Float: return fValue.Equals(other.fValue);
            case DsonType.Double: return dValue.Equals(other.dValue);
            case DsonType.Bool: return bValue == other.bValue;
            case DsonType.Null: return true;
            case DsonType.Pointer: return ObjectPtr.Equals(other.ObjectPtr);
            case DsonType.LitePointer: return ObjectLitePtr.Equals(other.ObjectLitePtr);
            case DsonType.DateTime: return DateTime.Equals(other.DateTime);
            case DsonType.Timestamp: return Timestamp.Equals(other.Timestamp);
            default:
                return Equals(objValue, other.objValue);
        }
    }

    public override bool Equals(object? obj) {
        return obj is UnionValue other && Equals(other);
    }

    public override int GetHashCode() {
        int r = type.GetHashCode(); // 可能为0
        int vhash = type switch
        {
            DsonType.EndOfObject => 0,
            DsonType.Int32 => iValue,
            DsonType.Int64 => lValue.GetHashCode(),
            DsonType.Float => fValue.GetHashCode(),
            DsonType.Double => dValue.GetHashCode(),
            DsonType.Bool => bValue.GetHashCode(),
            DsonType.Null => DsonNull.NULL.GetHashCode(), // null的Hash也特殊处理
            DsonType.Pointer => ObjectPtr.GetHashCode(),
            DsonType.LitePointer => ObjectLitePtr.GetHashCode(),
            DsonType.DateTime => DateTime.GetHashCode(),
            DsonType.Timestamp => Timestamp.GetHashCode(),
            _ => objValue == null ? 0 : objValue.GetHashCode()
        };
        return r * 31 + vhash;
    }

    public static bool operator ==(UnionValue left, UnionValue right) {
        return left.Equals(right);
    }

    public static bool operator !=(UnionValue left, UnionValue right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        switch (type) {
            case DsonType.EndOfObject: return $"Type: {type}, Value: null";
            case DsonType.Int32: return $"Type: {type}, Value: {iValue}";
            case DsonType.Int64: return $"Type: {type}, Value: {lValue}";
            case DsonType.Float: return $"Type: {type}, Value: {fValue}";
            case DsonType.Double: return $"Type: {type}, Value: {dValue}";
            case DsonType.Bool: return $"Type: {type}, Value: {bValue}";
            case DsonType.Pointer: return $"Type: {type}, Value: {ObjectPtr}";
            case DsonType.LitePointer: return $"Type: {type}, Value: {ObjectLitePtr}";
            case DsonType.DateTime: return $"Type: {type}, Value: {DateTime}";
            case DsonType.Timestamp: return $"Type: {type}, Value: {Timestamp}";
            default:
                return $"Type: {type}, Value: {objValue}";
        }
    }
}
}