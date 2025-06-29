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
using static Wjybxx.Dson.DsonInt32;

namespace Wjybxx.Dson
{
/// <summary>
/// DsonInt64
/// </summary>
public sealed class DsonInt64 : DsonNumber, IEquatable<DsonInt64>, IComparable<DsonInt64>, IComparable
{
    private readonly long _value;

    public DsonInt64(long value) {
        this._value = value;
    }

    public override DsonType DsonType => DsonType.Int64;
    public long Value => _value;

    public override int IntValue => (int)_value;
    public override long LongValue => _value;
    public override float FloatValue => _value;
    public override double DoubleValue => _value;

    #region equals

    public bool Equals(DsonInt64? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value == other._value;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonInt64)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonInt64? left, DsonInt64? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonInt64? left, DsonInt64? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonInt64? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonInt64 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonInt64)}");
    }

    public static bool operator <(DsonInt64? left, DsonInt64? right) {
        return Comparer<DsonInt64>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(DsonInt64? left, DsonInt64? right) {
        return Comparer<DsonInt64>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(DsonInt64? left, DsonInt64? right) {
        return Comparer<DsonInt64>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(DsonInt64? left, DsonInt64? right) {
        return Comparer<DsonInt64>.Default.Compare(left, right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }

    #region 池化

    // 注意初始化顺序
    private static readonly DsonInt64[] POOL = new DsonInt64[POOL_END - POOL_START + 1];
    public static readonly DsonInt64 ZERO;
    public static readonly DsonInt64 ONE;
    public static readonly DsonInt64 MINUS_ONE;

    static DsonInt64() {
        for (int i = POOL_START; i <= POOL_END; i++) {
            POOL[i - POOL_START] = new DsonInt64(i);
        }
        ZERO = ValueOf(0);
        ONE = ValueOf(1);
        MINUS_ONE = ValueOf(-1);
    }

    public static DsonInt64 ValueOf(long value) {
        if (value < POOL_START || value > POOL_END) {
            return new DsonInt64(value);
        }
        return POOL[(int)value - POOL_START];
    }

    #endregion
}
}