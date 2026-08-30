#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using static Wjybxx.Dson.DsonInt32;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson双精度浮点数
/// </summary>
public sealed class DsonDouble : DsonNumber, IComparable<DsonDouble>, IEquatable<DsonDouble>, IComparable
{
    private readonly double _value;

    public DsonDouble(double value) {
        this._value = value;
    }

    public override DsonType DsonType => DsonType.Double;
    public double Value => _value;

    public override int IntValue => (int)_value;
    public override long LongValue => (long)_value;
    public override float FloatValue => (float)_value;
    public override double DoubleValue => _value;

    #region equals

    public bool Equals(DsonDouble? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonDouble)obj);
    }

    public static bool operator ==(DsonDouble? left, DsonDouble? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonDouble? left, DsonDouble? right) {
        return !Equals(left, right);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public int CompareTo(DsonDouble? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonDouble other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonDouble)}");
    }

    public static bool operator <(DsonDouble? left, DsonDouble? right) {
        return Comparer<DsonDouble>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonDouble? left, DsonDouble? right) {
        return Comparer<DsonDouble>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonDouble? left, DsonDouble? right) {
        return Comparer<DsonDouble>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonDouble? left, DsonDouble? right) {
        return Comparer<DsonDouble>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }

    #region 池化

    /// <summary>
    /// Q：为什么double要池化？
    /// A：因为数字的默认解析类型是double。
    /// </summary>
    private static readonly DsonDouble[] POOL = new DsonDouble[POOL_END - POOL_START + 1];
    public static readonly DsonDouble ZERO;
    public static readonly DsonDouble ONE;
    public static readonly DsonDouble MINUS_ONE;

    static DsonDouble() {
        for (int i = POOL_START; i <= POOL_END; i++) {
            POOL[i - POOL_START] = new DsonDouble(i);
        }
        ZERO = ValueOf(0);
        ONE = ValueOf(1);
        MINUS_ONE = ValueOf(-1);
    }

    public static DsonDouble ValueOf(double dValue) {
        int value = (int)dValue;
        if (value != dValue) { // 非整数
            return new DsonDouble(dValue);
        }
        if (value < POOL_START || value > POOL_END) {
            return new DsonDouble(value);
        }
        return POOL[value - POOL_START];
    }

    #endregion
}
}