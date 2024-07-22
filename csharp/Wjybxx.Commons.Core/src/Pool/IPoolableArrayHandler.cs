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

namespace Wjybxx.Commons.Pool;

/// <summary>
/// 类数组对象的处理器
/// </summary>
public interface IPoolableArrayHandler<T>
{
    /// <summary>
    /// 获取实例的空间
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    int GetCapacity(T obj);

    /// <summary>
    /// 创建一个对应实例
    /// </summary>
    /// <param name="pool">对象池</param>
    /// <param name="capacity">期望的空间，常用于数组类对象</param>
    /// <returns></returns>
    T Create(IObjectPool<T> pool, int capacity);

    /// <summary>
    /// 重置对象数据
    /// </summary>
    /// <param name="obj"></param>
    void Reset(T obj);

    /// <summary>
    /// 测试对象的有效性
    /// </summary>
    /// <returns></returns>
    bool Validate(T obj);

    /// <summary>
    /// 销毁对象
    /// 1.对象未能归还到池中，或对象池清理时调用。
    /// 2.可能是一个无效状态的对象
    /// 3.这类对象通常与IO操作相关，可能有必须释放的资源
    /// </summary>
    /// <param name="obj">要销毁的对象</param>
    void Destroy(T obj);
}