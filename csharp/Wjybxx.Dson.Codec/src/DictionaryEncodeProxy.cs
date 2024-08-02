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
public class DictionaryEncodeProxy
{
    /**
     * 将字典写为普通文档
     * {@code
     * { K1: V1, K2: V2, K3: V3... }
     * }
     */
    public const int MODE_DOCUMENT = 0;
    /**
     * 将字典写为普通数组
     * {@code
     * [K, V, K2, V2, K3, V3...]
     * }
     */
    public const int MODE_ARRAY = 1;
    /**
     * 将Pair写为子数组
     * {@code
     * [[K1, V1], [K2, V2], [K3, V3]...]
     * }
     */
    public const int MODE_PAIR_AS_ARRAY = 2;
    /**
     * 将Pair写为子文档
     * {@code
     * [{K1: V1}, {K2: V2}, {K3: V3}...]
     * }
     */
    public const int MODE_PAIR_AS_DOCUMENT = 3;
}

/// <summary>
/// 字典编码代理
/// </summary>
public class DictionaryEncodeProxy<V> : DictionaryEncodeProxy
{
    private int mode = MODE_DOCUMENT;
    private IEnumerable<KeyValuePair<string, V>>? entries;

    public int Mode => mode;

    public IEnumerable<KeyValuePair<string, V>>? Entries {
        get => entries;
        set => entries = value;
    }

    /** 将字典写为普通文档 */
    public DictionaryEncodeProxy<V> SetWriteAsDocument() {
        mode = MODE_DOCUMENT;
        return this;
    }

    /** 将字典写为普通数组 */
    public DictionaryEncodeProxy<V> SetWriteAsArray() {
        mode = MODE_ARRAY;
        return this;
    }

    /** 将Pair写为子数组 -- 外部将写为数组 */
    public DictionaryEncodeProxy<V> SetWritePairAsArray() {
        mode = MODE_PAIR_AS_ARRAY;
        return this;
    }

    /** 将Pair写为子文档 -- 外部将写为数组 */
    public DictionaryEncodeProxy<V> SetWritePairAsDocument() {
        mode = MODE_PAIR_AS_DOCUMENT;
        return this;
    }
}
}