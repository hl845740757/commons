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

using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.IO;

/// <summary>
/// 可池化对象处理器
/// </summary>
public interface IPoolableObjectHandler<T>
{
    /// <summary>
    /// 创建一个对应实例
    /// </summary>
    /// <param name="pool">对象池</param>
    /// <param name="capacity">期望的空间，常用于数组类对象</param>
    /// <returns></returns>
    T Create(IObjectPool<T> pool, int capacity);

    /// <summary>
    /// 测试对象是否可以归还到池
    /// </summary>
    /// <returns></returns>
    bool Test(T obj);

    /// <summary>
    /// 重置对象数据
    /// </summary>
    /// <param name="obj"></param>
    void Reset(T obj);

    /// <summary>
    /// 销毁对象
    /// 1.对象未能归还到池中，或对象池清理时调用。
    /// 2.可能需要处理池外对象
    /// </summary>
    /// <param name="obj"></param>
    void Destroy(T obj);
}