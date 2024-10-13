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

using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Tests.Apt;

/// <summary>
/// 测试泛型类型
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[DsonSerializable]
public class MyDictionary<TKey, TValue> where TKey : notnull
{
    /** 测试泛型指定工厂 */
    public static readonly Func<MyDictionary<TKey, TValue>> FACTORY = () => new MyDictionary<TKey, TValue>();

    internal Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    public MyDictionary() {
    }

    public void Add(TKey key, TValue value) {
        dictionary.Add(key, value);
    }

    public void Clear() {
        dictionary.Clear();
    }

    public bool ContainsKey(TKey key) {
        return dictionary.ContainsKey(key);
    }

    public bool ContainsValue(TValue value) {
        return dictionary.ContainsValue(value);
    }

    public bool Remove(TKey key) {
        return dictionary.Remove(key);
    }

    public bool Remove(TKey key, out TValue value) {
        return dictionary.Remove(key, out value);
    }

    public bool TryAdd(TKey key, TValue value) {
        return dictionary.TryAdd(key, value);
    }

    public bool TryGetValue(TKey key, out TValue value) {
        return dictionary.TryGetValue(key, out value);
    }

    public int Count => dictionary.Count;
    public TValue this[TKey key] {
        get => dictionary[key];
        set => dictionary[key] = value;
    }
    public Dictionary<TKey, TValue>.KeyCollection Keys => dictionary.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => dictionary.Values;
}