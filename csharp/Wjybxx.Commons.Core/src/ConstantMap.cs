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
using System.Collections.Immutable;
using System.Linq;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons;

/// <summary>
/// 常量池快照字典。
/// 由于<see cref="ConstantPool{T}"/>是可变的，这使得有些查询是高开销的，比如：{@link ConstantPool#values()}
/// </summary>
[Immutable]
public sealed class ConstantMap<T> where T : class, IConstant
{
    private readonly IList<T> immutableValues;
    private readonly IList<string> immutableNames;
    private readonly IDictionary<string, T> constants;

    internal ConstantMap(ConstantPool<T> pool) {
        immutableValues = pool.Values.ToImmutableList();
        immutableNames = immutableValues.Select(e => e.Name)
            .ToImmutableList();
        constants = immutableValues.ToImmutableDictionary(e => e.Name, e => e);
    }

    /// <summary>
    /// 判断对应的常量是否存在
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Exists(string name) {
        IConstant.CheckName(name);
        return constants.ContainsKey(name);
    }

    /// <summary>
    /// 获取对应的常量，若不存在关联的常量则返回null
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public T? Get(string name) {
        IConstant.CheckName(name);
        return constants.TryGetValue(name, out var constant) ? constant : null;
    }

    /// <summary>
    /// 获取对应的常量，若不存在关联的常量则抛出异常
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public T GetOrThrow(string name) {
        IConstant.CheckName(name);
        if (constants.TryGetValue(name, out var constant)) {
            return constant;
        }
        throw new ArgumentException(name + " does not exist");
    }

    /// <summary>
    /// 常量对象数
    /// </summary>
    public int Count => constants.Count;

    /// <summary>
    /// 已排序的不可变常量集合
    /// </summary>
    public IList<T> Values => immutableValues;

    /// <summary>
    /// 常量的名字集合，和<see cref="Values"/>的顺序一致。
    /// </summary>
    public IList<string> Names => immutableNames;
}