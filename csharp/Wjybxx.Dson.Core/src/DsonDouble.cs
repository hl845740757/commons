#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Collections.Generic;

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
}
}