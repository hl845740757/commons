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

namespace Wjybxx.Commons.Mutable
{
/// <summary>
/// 可变Int32
/// </summary>
public class MutableInt : IMutableNumber<int>, IEquatable<MutableInt>, IComparable<MutableInt>
{
    private int _value;

    public MutableInt(int value = 0) {
        _value = value;
    }

    public int Value {
        get => _value;
        set => _value = value;
    }

    public int IntValue => _value;
    public long LongValue => _value;
    public float FloatValue => _value;
    public double DoubleValue => _value;

    #region op

    /** 加上操作数 */
    public void Add(int operand) {
        this._value += operand;
    }

    /** 返回加上操作数后的值 */
    public int AddAndGet(int operand) {
        this._value += operand;
        return _value;
    }

    /** 返回当前值 */
    public int GetAndAdd(int operand) {
        int last = _value;
        this._value += operand;
        return last;
    }

    /** 加1 */
    public void Increment() {
        _value++;
    }

    /** 返回加1后的值 */
    public int IncrementAndGet() {
        return ++_value;
    }

    /** 加1并返回当前值 */
    public int GetAndIncrement() {
        return _value++;
    }

    /** 减1 */
    public void Decrement() {
        _value--;
    }

    /** 返回减1后的值 */
    public int DecrementAndGet() {
        return --_value;
    }

    /** 减1并返回当前值 */
    public int GetAndDecrement() {
        return _value--;
    }

    #endregion

    #region compare

    public int CompareTo(MutableInt? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return _value.CompareTo(other._value);
    }

    public static bool operator <(MutableInt? left, MutableInt? right) {
        return Comparer<MutableInt>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(MutableInt? left, MutableInt? right) {
        return Comparer<MutableInt>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(MutableInt? left, MutableInt? right) {
        return Comparer<MutableInt>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(MutableInt? left, MutableInt? right) {
        return Comparer<MutableInt>.Default.Compare(left, right) >= 0;
    }

    #endregion

    #region equals

    public bool Equals(MutableInt? other) {
        if (other == null) {
            return false;
        }
        return _value == other._value;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MutableInt)obj);
    }

    public override int GetHashCode() {
        return _value;
    }

    public static bool operator ==(MutableInt? left, MutableInt? right) {
        return Equals(left, right);
    }

    public static bool operator !=(MutableInt? left, MutableInt? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(Value)}: {Value}";
    }

    #endregion
}
}