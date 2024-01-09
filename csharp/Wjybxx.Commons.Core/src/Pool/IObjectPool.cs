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

using System.Collections.ObjectModel;

namespace Wjybxx.Commons.Pool;

/// <summary>
/// 简单对象池抽象
/// 线程安全性取决于具体实现
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IObjectPool<T>
{
    /// <summary>
    /// 从对象池中租借一个对象
    /// 如果池中有可用的对象，则返回缓存的对象，否则返回一个新的对象
    /// </summary>
    /// <returns></returns>
    T Rent();

    /// <summary>
    /// 将对象放入缓存池
    /// （为保持和Java一致，不直接命名Return）
    /// </summary>
    /// <param name="obj"></param>
    void ReturnOne(T obj);

    /// <summary>
    /// 将对象放入缓存池
    /// </summary>
    /// <param name="objects"></param>
    void ReturnAll(Collection<T?> objects) {
        foreach (T obj in objects) {
            if (obj != null) ReturnOne(obj);
        }
    }

    /// <summary>
    /// 删除此池中的所有对象
    /// (可空实现)
    /// </summary>
    void FreeAll();
}