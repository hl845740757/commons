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

using System.Collections.Generic;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 字典编码代理
/// </summary>
public class DictionaryEncodeProxy<V>
{
    private MapEncodePolicy _policy;
    private IEnumerable<KeyValuePair<string, V>>? _entries;

    public DictionaryEncodeProxy(MapEncodePolicy policy = MapEncodePolicy.Document) {
        this._policy = policy;
    }

    public MapEncodePolicy Policy {
        get => _policy;
        set => _policy = value;
    }

    public IEnumerable<KeyValuePair<string, V>>? Entries {
        get => _entries;
        set => _entries = value;
    }
}
}