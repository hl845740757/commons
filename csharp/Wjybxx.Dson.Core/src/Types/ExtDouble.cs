﻿#region LICENSE

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

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 具有类型标签的双精度浮点数
/// </summary>
public readonly struct ExtDouble : IEquatable<ExtDouble>, IComparable<ExtDouble>, IComparable
{
    private readonly int _type;
    private readonly bool _hasVal; // 比较时放前面
    private readonly double _value;

    public ExtDouble(int type, double? value)
        : this(type, value ?? 0, value.HasValue) {
    }

    public ExtDouble(int type, double value, bool hasVal = true) {
        Dsons.CheckSubType(type);
        Dsons.CheckHasValue(value, hasVal);
        _type = type;
        _value = value;
        _hasVal = hasVal;
    }

    public int Type => _type;
    public bool HasValue => _hasVal;
    public double Value => _value;

    #region equals

    public bool Equals(ExtDouble other) {
        return _type == other._type && _hasVal == other._hasVal && _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        return obj is ExtDouble other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _hasVal, _value);
    }

    public static bool operator ==(ExtDouble left, ExtDouble right) {
        return left.Equals(right);
    }

    public static bool operator !=(ExtDouble left, ExtDouble right) {
        return !left.Equals(right);
    }

    public int CompareTo(ExtDouble other) {
        var typeComparison = _type.CompareTo(other._type);
        if (typeComparison != 0) return typeComparison;
        var hasValComparison = _hasVal.CompareTo(other._hasVal);
        if (hasValComparison != 0) return hasValComparison;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is ExtDouble other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ExtDouble)}");
    }

    public static bool operator <(ExtDouble left, ExtDouble right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(ExtDouble left, ExtDouble right) {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(ExtDouble left, ExtDouble right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(ExtDouble left, ExtDouble right) {
        return left.CompareTo(right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_type)}: {_type}, {nameof(_hasVal)}: {_hasVal}, {nameof(_value)}: {_value}";
    }
}
}