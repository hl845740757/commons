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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 用于避免对值类型装箱
/// 本想做得更彻底一点，但引用类型不能和非引用类型重叠，于是只能修改为阉割版...
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct UnionValue : IEquatable<UnionValue>
{
    // 值的类型
    [FieldOffset(0)] public DsonType type;
    [FieldOffset(1)] public int iValue;
    [FieldOffset(1)] public long lValue;
    [FieldOffset(1)] public float fValue;
    [FieldOffset(1)] public double dValue;
    [FieldOffset(1)] public bool bValue;
    // 只包含基础值的结构体
    [FieldOffset(1)] public ExtInt32 extInt32;
    [FieldOffset(1)] public ExtInt64 extInt64;
    [FieldOffset(1)] public ExtDouble extDouble;
    [FieldOffset(1)] public ExtDateTime dateTime;
    [FieldOffset(1)] public Timestamp timestamp;

    public UnionValue(DsonType type) : this() {
        this.type = type;
    }

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
            case DsonType.DateTime: return dateTime.Equals(other.dateTime);
            case DsonType.Timestamp: return timestamp.Equals(other.timestamp);
            default:
                throw new AssertionError();
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
            DsonType.DateTime => dateTime.GetHashCode(),
            DsonType.Timestamp => timestamp.GetHashCode(),
            _ => throw new AssertionError()
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
            case DsonType.DateTime: return $"Type: {type}, Value: {dateTime}";
            case DsonType.Timestamp: return $"Type: {type}, Value: {timestamp}";
            default:
                throw new AssertionError();
        }
    }
}
}