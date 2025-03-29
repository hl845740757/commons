#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

namespace BTree.Tests;

/// <summary>
/// 测试代码使用的黑板
/// </summary>
internal class Blackboard
{
    private readonly IDictionary<string, object> map = new Dictionary<string, object>(16);

    public int Count => map.Count;

    public void Clear() {
        map.Clear();
    }

    public void Add(string key, object value) {
        map.Add(key, value);
    }

    public bool ContainsKey(string key) {
        return map.ContainsKey(key);
    }

    public bool Remove(string key) {
        return map.Remove(key);
    }

    public bool TryGetValue(string key, out object value) {
        return map.TryGetValue(key, out value);
    }

    public object this[string key] {
        get => map[key];
        set => map[key] = value;
    }
}