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

using System;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 集合转换器
/// 主要用于实现读取为不可变集合。
///
/// 注意：C#没有原生的保持插入序的不可变字典。
/// </summary>
public interface CollectionConverter
{
    /// <summary>
    /// 转换字典
    /// </summary>
    /// <param name="declaredType">类型声明信息</param>
    /// <param name="dictionary">待转换的字典</param>
    /// <returns></returns>
    IDictionary<K, V> ConvertDictionary<K, V>(Type declaredType, IDictionary<K, V> dictionary);

    /// <summary>
    /// 转换集合
    /// </summary>
    /// <param name="declaredType">类型声明信息</param>
    /// <param name="collection">待转换的集合</param>
    /// <returns></returns>
    ICollection<K> ConvertCollection<K>(Type declaredType, ICollection<K> collection);
    
    /// <summary>
    /// 默认的不可变转换器
    /// </summary>
    public static CollectionConverter ImmutableConverter => CImmutableConverter.Inst;

    private class CImmutableConverter : CollectionConverter
    {
        public static CImmutableConverter Inst { get; } = new CImmutableConverter();

        public IDictionary<K, V> ConvertDictionary<K, V>(Type declaredType, IDictionary<K, V> dictionary) {
            return dictionary.ToImmutableLinkedDictionary();
        }

        public ICollection<K> ConvertCollection<K>(Type declaredType, ICollection<K> collection) {
            if (collection is IGenericSet<K>) {
                return collection.ToImmutableLinkedHashSet();
            }
            return collection.ToImmutableList2();
        }
    }
}
}