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

namespace Wjybxx.Commons.Mutable
{
/// <summary>
/// 通用可变对象
/// </summary>
/// <typeparam name="T"></typeparam>
public class MutableObject<T> : IMutable<T>, IEquatable<MutableObject<T>> where T : class
{
    private T? _value;

    public MutableObject() {
    }

    public MutableObject(T? value) {
        _value = value;
    }

    public T? Value {
        get => _value;
        set => _value = value;
    }

    /// <summary>
    /// 设置为新值并返回旧值
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T? GetAndSet(T value) {
        T r = this._value;
        this._value = value;
        return r;
    }

    #region equals

    public bool Equals(MutableObject<T>? other) {
        if (other == null) {
            return false;
        }
        return Equals(_value, other._value);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MutableObject<T>)obj);
    }

    public override int GetHashCode() {
        return _value == null ? 0 : _value.GetHashCode();
    }

    public static bool operator ==(MutableObject<T>? left, MutableObject<T>? right) {
        return Equals(left, right);
    }

    public static bool operator !=(MutableObject<T>? left, MutableObject<T>? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(Value)}: {Value}";
    }

    #endregion
}
}