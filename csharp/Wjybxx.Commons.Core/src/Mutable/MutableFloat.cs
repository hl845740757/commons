#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using System.Collections.Generic;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Mutable;

/// <summary>
/// 可变Int32
/// </summary>
public class MutableFloat : IMutableNumber<float>, IEquatable<MutableFloat>, IComparable<MutableFloat>
{
    private float _value;

    public MutableFloat(float value = 0f) {
        _value = value;
    }

    public float Value {
        get => _value;
        set => _value = value;
    }

    public int IntValue => (int)_value;
    public long LongValue => (long)_value;
    public float FloatValue => _value;
    public double DoubleValue => _value;

    #region op

    /** 加上操作数 */
    public void Add(float operand) {
        this._value += operand;
    }

    /** 返回加上操作数后的值 */
    public float AddAndGet(float operand) {
        this._value += operand;
        return _value;
    }

    /** 返回当前值 */
    public float GetAndAdd(float operand) {
        float last = _value;
        this._value += operand;
        return last;
    }

    /** 加1 */
    public void Increment() {
        _value++;
    }

    /** 返回加1后的值 */
    public float IncrementAndGet() {
        return ++_value;
    }

    /** 加1并返回当前值 */
    public float GetAndIncrement() {
        return _value++;
    }

    /** 减1 */
    public void Decrement() {
        _value--;
    }

    /** 返回减1后的值 */
    public float DecrementAndGet() {
        return --_value;
    }

    /** 减1并返回当前值 */
    public float GetAndDecrement() {
        return _value--;
    }

    #endregion

    #region compare

    public int CompareTo(MutableFloat? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _value.CompareTo(other._value);
    }

    public static bool operator <(MutableFloat? left, MutableFloat? right) {
        return Comparer<MutableFloat>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(MutableFloat? left, MutableFloat? right) {
        return Comparer<MutableFloat>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(MutableFloat? left, MutableFloat? right) {
        return Comparer<MutableFloat>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(MutableFloat? left, MutableFloat? right) {
        return Comparer<MutableFloat>.Default.Compare(left, right) >= 0;
    }

    #endregion

    #region equals

    public bool Equals(MutableFloat? other) {
        if (other == null) {
            return false;
        }
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MutableFloat)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(MutableFloat? left, MutableFloat? right) {
        return Equals(left, right);
    }

    public static bool operator !=(MutableFloat? left, MutableFloat? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(Value)}: {Value}";
    }

    #endregion
}