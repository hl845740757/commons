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

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 具有类型标签的字符串
/// </summary>
public readonly struct ExtString : IEquatable<ExtString>, IComparable<ExtString>, IComparable
{
    public const int MaskType = 1;
    public const int MaskValue = 1 << 1;

    private readonly int _type;
    private readonly string? _value;

    public ExtString(int type, string? value) {
        _type = type;
        _value = value;
    }

    public int Type => _type;
    public bool HasValue => _value != null;

    /// <summary>
    /// value可能为null
    /// </summary>
    public string? Value => _value;

    #region equals

    public bool Equals(ExtString other) {
        return _type == other._type && _value == other._value;
    }

    public override bool Equals(object? obj) {
        return obj is ExtString other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _value);
    }

    public static bool operator ==(ExtString left, ExtString right) {
        return left.Equals(right);
    }

    public static bool operator !=(ExtString left, ExtString right) {
        return !left.Equals(right);
    }

    public int CompareTo(ExtString other) {
        var typeComparison = _type.CompareTo(other._type);
        if (typeComparison != 0) return typeComparison;
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is ExtString other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ExtString)}");
    }

    public static bool operator <(ExtString left, ExtString right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(ExtString left, ExtString right) {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(ExtString left, ExtString right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(ExtString left, ExtString right) {
        return left.CompareTo(right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_type)}: {_type}, {nameof(_value)}: {_value}";
    }
}
}