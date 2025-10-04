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
using System.Runtime.InteropServices;
using Wjybxx.Commons;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 用于避免对值类型装箱
/// 内存开销：40字节
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct UnionValue : IEquatable<UnionValue>
{
#nullable disable
    // 值的类型 -- 偷懒方案，Object表示任意类型
    // 由于内存对齐的原因，为避免内存浪费，我们在double前面插入三个byte类型值
    [FieldOffset(0)] public DsonType type;
    [FieldOffset(1)] public byte b1;
    [FieldOffset(2)] public byte b2;
    [FieldOffset(3)] public byte b3;
    // 固定8个字节
    [FieldOffset(4)] public int iValue;
    [FieldOffset(4)] public long lValue; // localId, seconds
    [FieldOffset(4)] public float fValue;
    [FieldOffset(4)] public double dValue;
    [FieldOffset(4)] public bool bValue;

    // 2个扩展int值，支持DateTime、TimeStamp
    [FieldOffset(12)] public int v2; // type, nanos
    [FieldOffset(16)] public int v3; // offset

    // 由于内存对齐的原因，引用类型需要偏移24
    [FieldOffset(24)] public object objValue; // collection, string, bytes
    [FieldOffset(32)] public object objValue2; // localPath

    public UnionValue(DsonType type) : this() {
        this.type = type;
    }

    public UnionValue(DsonType type, object objValue) : this() {
        this.type = type;
        this.objValue = objValue;
    }

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfInt32(int value) {
        return new UnionValue(DsonType.Int32) { iValue = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfInt64(long value) {
        return new UnionValue(DsonType.Int64) { lValue = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfFloat(float value) {
        return new UnionValue(DsonType.Float) { fValue = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfDouble(double value) {
        return new UnionValue(DsonType.Double) { dValue = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfBool(bool value) {
        return new UnionValue(DsonType.Bool) { bValue = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfString(string value) {
        return new UnionValue(DsonType.String, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfBinary(Binary value) {
        return new UnionValue(DsonType.Binary, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfObjectPtr(in ObjectPtr value) {
        return new UnionValue(DsonType.Pointer) { ObjectPtr = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfDateTime(in ExtDateTime value) {
        return new UnionValue(DsonType.DateTime) { DateTime = value };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfTimestamp(in Timestamp value) {
        return new UnionValue(DsonType.Timestamp) { Timestamp = value };
    }

    #endregion

    #region converter

    public ObjectPtr ObjectPtr {
        get => new ObjectPtr((string)objValue, (string)objValue2, lValue, v2);
        set {
            objValue = value.Collection;
            objValue2 = value.LocalPath;
            lValue = value.LocalId;
            v2 = value.Type;
        }
    }

    public ExtDateTime DateTime {
        get => new ExtDateTime(lValue, v2, v3, b1);
        set {
            lValue = value.Seconds;
            v2 = value.Nanos;
            v3 = value.Offset;
            b1 = value.Enables;
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

#nullable restore

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
            case DsonType.DateTime: return $"Type: {type}, Value: {DateTime}";
            case DsonType.Timestamp: return $"Type: {type}, Value: {Timestamp}";
            default:
                return $"Type: {type}, Value: {objValue}";
        }
    }
}
}