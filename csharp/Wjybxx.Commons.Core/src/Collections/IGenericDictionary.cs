#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

using System.Collections.Generic;

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 泛型字典
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public interface IGenericDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>,
    IGenericCollection<KeyValuePair<TKey, TValue>>
{
    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    new TValue this[TKey key] { get; set; }

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    new IGenericCollection<TKey> Keys { get; }

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    new IGenericCollection<TValue> Values { get; }

    /// <inheritdoc cref="IDictionary{TKey,TValue}"/>
    new bool ContainsKey(TKey key);

    /// <summary>
    /// 获取允许删除元素的Key集合，删除会作用于原始字典。
    /// 在C#的集合中，Keys和Values默认都是只读的，这在多数情况下都是好的选择；但有时我们也确实需要能通过Keys集合删除元素。
    /// 直接通过Values删除字典元素的情况很不常见，因此不提供额外支持。
    /// </summary>
    IGenericCollection<TKey> UnsafeKeys();

    /// <summary>
    /// 获取key关联的值，如果关联的值不存在，则返回预设的默认值。
    /// 如果字典支持自定义默认值，则返回自定义默认值；否则返回default分配的对象。
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value">接受返回值</param>
    /// <returns>如果key存在则返回true；否则返回false</returns>
    new bool TryGetValue(TKey key, out TValue value);

    /// <summary>
    /// 是否包含给定的Value
    /// (看似IDictionary没定义此接口，实际上却必须要实现，因为Values集合要实现Contains)
    /// </summary>
    /// <returns></returns>
    bool ContainsValue(TValue value);

    /// <summary>
    /// 如果key不存在，则插入键值对并返回true，否则返回false
    /// </summary>
    /// <returns>插入成功则返回true</returns>
    bool TryAdd(TKey key, TValue value);

    /// <summary>
    /// 与Add不同，Put操作在Key存在值，总是覆盖当前关联值，而不是抛出异常
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    PutResult<TValue> Put(TKey key, TValue value);

    /// <summary>
    /// 字典的原生接口Remove只返回bool值，而更多的情况下我们需要返回值；但C#存在值结构，当value是值类型的时候总是返回值会导致不必要的内存分配。
    /// <see cref="Dictionary{TKey,TValue}"/>中提供了该补偿方法，但未在接口中添加。
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value">接收返回值</param>
    /// <returns>是否删除成功</returns>
    bool Remove(TKey key, out TValue value);

    #region 接口适配

    // 泛型接口建议实现类再显式实现，因为转换为接口的情况较多，可减少转发
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(key);

    bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(key);

    TValue IDictionary<TKey, TValue>.this[TKey key] {
        get => this[key];
        set => this[key] = value;
    }

    TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) {
        Add(item.Key, item.Value);
    }

    #endregion
}