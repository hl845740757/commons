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
/// Dson单精度浮点数
/// </summary>
public sealed class DsonFloat : DsonNumber, IEquatable<DsonFloat>, IComparable<DsonFloat>, IComparable
{
    private readonly float _value;

    public DsonFloat(float value) {
        this._value = value;
    }

    public override DsonType DsonType => DsonType.Float;
    public float Value => _value;

    public override int IntValue => (int)_value;
    public override long LongValue => (long)_value;
    public override float FloatValue => _value;
    public override double DoubleValue => _value;

    #region equals

    public bool Equals(DsonFloat? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonFloat)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonFloat? left, DsonFloat? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonFloat? left, DsonFloat? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonFloat? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonFloat other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonFloat)}");
    }

    public static bool operator <(DsonFloat? left, DsonFloat? right) {
        return Comparer<DsonFloat>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonFloat? left, DsonFloat? right) {
        return Comparer<DsonFloat>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonFloat? left, DsonFloat? right) {
        return Comparer<DsonFloat>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonFloat? left, DsonFloat? right) {
        return Comparer<DsonFloat>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}