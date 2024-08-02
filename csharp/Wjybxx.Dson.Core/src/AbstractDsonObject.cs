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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson KV类结构的抽象实现
/// </summary>
/// <typeparam name="TK">String或<see cref="FieldNumber"/></typeparam>
public abstract class AbstractDsonObject<TK> : DsonValue, IGenericDictionary<TK, DsonValue>, IEquatable<AbstractDsonObject<TK>>
{
    protected readonly IGenericDictionary<TK, DsonValue> _valueMap;

    public AbstractDsonObject(IGenericDictionary<TK, DsonValue> valueMap) {
        _valueMap = valueMap ?? throw new ArgumentNullException(nameof(valueMap));
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public IEnumerator<KeyValuePair<TK, DsonValue>> GetEnumerator() {
        return _valueMap.GetEnumerator();
    }

    #region 元素检查

    protected static void CheckElement(TK? key, DsonValue? value) {
        if (key == null) throw new ArgumentException("key cant be null");
        if (value == null) throw new ArgumentException("value cant be null");
        if (value.DsonType == DsonType.Header) throw new ArgumentException("add Header");
    }

    public DsonValue this[TK key] {
        get => _valueMap[key];
        set {
            CheckElement(key, value);
            _valueMap[key] = value;
        }
    }

    public void Add(KeyValuePair<TK, DsonValue> item) {
        CheckElement(item.Key, item.Value);
        _valueMap.Add(item);
    }

    public void Add(TK key, DsonValue value) {
        CheckElement(key, value);
        _valueMap.Add(key, value);
    }

    public bool TryAdd(TK key, DsonValue value) {
        CheckElement(key, value);
        return _valueMap.TryAdd(key, value);
    }

    public PutResult<DsonValue> Put(TK key, DsonValue value) {
        CheckElement(key, value);
        return _valueMap.Put(key, value);
    }

    public virtual AbstractDsonObject<TK> Append(TK key, DsonValue value) {
        CheckElement(key, value);
        _valueMap[key!] = value;
        return this;
    }

    #endregion

    #region 简单代理

    public bool IsReadOnly => _valueMap.IsReadOnly;
    public int Count => _valueMap.Count;
    public bool IsEmpty => _valueMap.IsEmpty;

    public bool Contains(KeyValuePair<TK, DsonValue> item) => _valueMap.Contains(item);

    public bool ContainsKey(TK key) => _valueMap.ContainsKey(key);

    public bool ContainsValue(DsonValue value) => _valueMap.ContainsValue(value);

    public bool TryGetValue(TK key, out DsonValue value) => _valueMap.TryGetValue(key, out value);

    public bool Remove(KeyValuePair<TK, DsonValue> item) => _valueMap.Remove(item);

    public bool Remove(TK key) => _valueMap.Remove(key);

    public bool Remove(TK key, out DsonValue value) => _valueMap.Remove(key, out value);

    public void Clear() => _valueMap.Clear();

    public void CopyTo(KeyValuePair<TK, DsonValue>[] array, int arrayIndex) => _valueMap.CopyTo(array, arrayIndex);

    public IGenericCollection<TK> Keys => _valueMap.Keys;
    public IGenericCollection<DsonValue> Values => _valueMap.Values;

    public void AdjustCapacity(int expectedCount) => _valueMap.AdjustCapacity(expectedCount);

    #endregion

    #region equals

    // 默认不比较header

    public bool Equals(AbstractDsonObject<TK>? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        // C#的集合默认都是未实现Equals的，因此无法准确的判断内容Equals
        return _valueMap.SequenceEqual(other._valueMap);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AbstractDsonObject<TK>)obj);
    }

    public override int GetHashCode() {
        return _valueMap.GetHashCode();
    }

    public static bool operator ==(AbstractDsonObject<TK>? left, AbstractDsonObject<TK>? right) {
        return Equals(left, right);
    }

    public static bool operator !=(AbstractDsonObject<TK>? left, AbstractDsonObject<TK>? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_valueMap)}: {_valueMap}";
    }
}
}