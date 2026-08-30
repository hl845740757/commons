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

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 具有类型标签的Int32
/// </summary>
public readonly struct ExtInt32 : IEquatable<ExtInt32>
{
    private readonly int _type;
    private readonly bool _hasVal; // 比较时放前面
    private readonly int _value;

    public ExtInt32(int type, int? value)
        : this(type, value ?? 0, value.HasValue) {
    }

    public ExtInt32(int type, int value, bool hasVal = true) {
        Dsons.CheckSubType(type);
        Dsons.CheckHasValue(value, hasVal);
        _type = type;
        _value = value;
        _hasVal = hasVal;
    }

    public int Type => _type;
    public bool HasValue => _hasVal;
    public int Value => _value;

    #region equals

    public bool Equals(ExtInt32 other) {
        return _type == other._type && _hasVal == other._hasVal && _value == other._value;
    }

    public override bool Equals(object? obj) {
        return obj is ExtInt32 other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_type, _hasVal, _value);
    }

    public static bool operator ==(ExtInt32 left, ExtInt32 right) {
        return left.Equals(right);
    }

    public static bool operator !=(ExtInt32 left, ExtInt32 right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(_type)}: {_type}, {nameof(_hasVal)}: {_hasVal}, {nameof(_value)}: {_value}";
    }
}
}