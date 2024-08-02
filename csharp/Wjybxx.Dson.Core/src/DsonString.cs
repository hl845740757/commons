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
public class DsonString : DsonValue, IEquatable<DsonString>, IComparable<DsonString>, IComparable
{
    private readonly string _value;

    public DsonString(string value) {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override DsonType DsonType => DsonType.String;
    public string Value => _value;

    #region equals

    public bool Equals(DsonString? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value == other._value;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonString)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonString? left, DsonString? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonString? left, DsonString? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonString? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonString other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonString)}");
    }

    public static bool operator <(DsonString? left, DsonString? right) {
        return Comparer<DsonString>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonString? left, DsonString? right) {
        return Comparer<DsonString>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonString? left, DsonString? right) {
        return Comparer<DsonString>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonString? left, DsonString? right) {
        return Comparer<DsonString>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}