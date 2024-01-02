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

using System.Collections.ObjectModel;

namespace Wjybxx.Commons.Pool;

/// <summary>
/// 简单对象池抽象
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IObjectPool<T> where T : class
{
    /// <summary>
    /// 从对象池中租借一个对象
    /// 如果池中有可用的对象，则返回缓存的对象，否则返回一个新的对象
    /// </summary>
    /// <returns></returns>
    T Rent();

    /// <summary>
    /// 将对象放入缓存池
    /// </summary>
    /// <param name="obj"></param>
    void ReturnOne(T obj);

    /// <summary>
    /// 将对象放入缓存池
    /// </summary>
    /// <param name="objects"></param>
    void ReturnAll(Collection<T?> objects) {
        foreach (var obj in objects) {
            if (obj != null) ReturnOne(obj);
        }
    }

    /// <summary>
    /// 缓存池缓存对象数量上限
    /// </summary>
    int MaxCount { get; }

    /// <summary>
    /// 当前池中可用对象数
    /// </summary>
    int IdleCount { get; }

    /// <summary>
    /// 删除此池中的所有可用对象
    /// </summary>
    void Clear();
}