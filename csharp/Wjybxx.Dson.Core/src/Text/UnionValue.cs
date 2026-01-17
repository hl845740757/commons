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
    [FieldOffset(0)] public DsonType type;
    // 固定8个字节
    [FieldOffset(4)] public int iValue;
    [FieldOffset(4)] public long lValue; // localId, seconds
    [FieldOffset(4)] public float fValue;
    [FieldOffset(4)] public double dValue;

    // 3个扩展int值，支持DateTime、TimeStamp
    [FieldOffset(12)] public int v2; // type, nanos
    [FieldOffset(16)] public int v3; // offset
    [FieldOffset(20)] public int v4; // enables

    // 由于内存对齐的原因，引用类型需要偏移24
    [FieldOffset(24)] public object objValue1; // collection, string, bytes
    [FieldOffset(32)] public object objValue2; // localPath

    public UnionValue(DsonType type) : this() {
        this.type = type;
    }

    public UnionValue(DsonType type, object objValue1) : this() {
        this.type = type;
        this.objValue1 = objValue1;
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
        return new UnionValue(DsonType.Bool) { iValue = value ? 1 : 0 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfString(string value) {
        return new UnionValue(DsonType.String, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfBinary(Binary value) {
        return new UnionValue(DsonType.Binary) { objValue1 = value };
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnionValue OfDouble4(in Double4 value) {
        return new UnionValue(DsonType.Double4) { Double4 = value };
    }

    #endregion

    #region converter

    public ObjectPtr ObjectPtr {
        get => new ObjectPtr((string)objValue1, (string)objValue2, lValue, v2);
        set {
            objValue1 = value.Collection;
            objValue2 = value.LocalPath;
            lValue = value.LocalId;
            v2 = value.Type;
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

    public Double4 Double4 {
        get => (Double4)objValue1;
        set => objValue1 = value;
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
            case DsonType.Bool: return iValue == other.iValue;
            case DsonType.Null: return true;
            case DsonType.Pointer: return ObjectPtr.Equals(other.ObjectPtr);
            case DsonType.DateTime: return DateTime.Equals(other.DateTime);
            case DsonType.Timestamp: return Timestamp.Equals(other.Timestamp);
            case DsonType.Double4: return Double4.Equals(other.Double4);
            default:
                return Equals(objValue1, other.objValue1);
        }
    }

    public override bool Equals(object? obj) {
        return obj is UnionValue other && Equals(other);
    }

    public override int GetHashCode() {
        int vhash = type switch
        {
            DsonType.EndOfObject => 0,
            DsonType.Int32 => iValue,
            DsonType.Int64 => lValue.GetHashCode(),
            DsonType.Float => fValue.GetHashCode(),
            DsonType.Double => dValue.GetHashCode(),
            DsonType.Bool => iValue.GetHashCode(),
            DsonType.Null => 0,
            DsonType.Pointer => ObjectPtr.GetHashCode(),
            DsonType.DateTime => DateTime.GetHashCode(),
            DsonType.Timestamp => Timestamp.GetHashCode(),
            DsonType.Double4 => Double4.GetHashCode(),
            _ => objValue1 == null ? 0 : objValue1.GetHashCode()
        };
        return (int)type * 31 + vhash;
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
            case DsonType.Bool: return $"Type: {type}, Value: {iValue != 0}";
            case DsonType.Pointer: return $"Type: {type}, Value: {ObjectPtr}";
            case DsonType.DateTime: return $"Type: {type}, Value: {DateTime}";
            case DsonType.Timestamp: return $"Type: {type}, Value: {Timestamp}";
            case DsonType.Double4: return $"Type: {type}, Value: {Double4}";
            default:
                return $"Type: {type}, Value: {objValue1}";
        }
    }
}
}