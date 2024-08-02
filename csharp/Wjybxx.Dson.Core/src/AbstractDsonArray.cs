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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson 数组类结构的抽象实现
/// </summary>
public abstract class AbstractDsonArray : DsonValue, IList<DsonValue>, IEquatable<AbstractDsonArray>
{
    protected readonly IList<DsonValue> _values;

    protected AbstractDsonArray(IList<DsonValue> values) {
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IEnumerator<DsonValue> GetEnumerator() {
        return _values.GetEnumerator();
    }

    /** 勿修改Values内容 */
    public IList<DsonValue> Values => _values;

    #region 元素检查

    protected static void CheckElement(DsonValue? value) {
        if (value == null) throw new ArgumentException("value cant be null");
        if (value.DsonType == DsonType.Header) throw new ArgumentException("add Header");
    }

    public DsonValue this[int index] {
        get => _values[index];
        set {
            CheckElement(value);
            _values[index] = value;
        }
    }

    public void Add(DsonValue item) {
        CheckElement(item);
        _values.Add(item);
    }

    public void Insert(int index, DsonValue item) {
        CheckElement(item);
        _values.Insert(index, item);
    }

    public virtual AbstractDsonArray Append(DsonValue item) {
        CheckElement(item);
        _values.Add(item);
        return this;
    }

    #endregion

    #region 简单代理

    public bool IsReadOnly => _values.IsReadOnly;
    public int Count => _values.Count;
    public bool IsEmpty => _values.Count == 0;

    public void Clear() {
        _values.Clear();
    }

    public bool Contains(DsonValue item) {
        return _values.Contains(item);
    }

    public void CopyTo(DsonValue[] array, int arrayIndex) {
        _values.CopyTo(array, arrayIndex);
    }

    public bool Remove(DsonValue item) {
        return _values.Remove(item);
    }

    public int IndexOf(DsonValue item) {
        return _values.IndexOf(item);
    }

    public void RemoveAt(int index) {
        _values.RemoveAt(index);
    }

    #endregion

    #region equals

    // 默认不比较header

    public bool Equals(AbstractDsonArray? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _values.SequenceEqual(other._values);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AbstractDsonArray)obj);
    }

    public override int GetHashCode() {
        return _values.GetHashCode();
    }

    public static bool operator ==(AbstractDsonArray? left, AbstractDsonArray? right) {
        return Equals(left, right);
    }

    public static bool operator !=(AbstractDsonArray? left, AbstractDsonArray? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_values)}: {_values}";
    }
}
}