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

using System;
using System.Collections.Generic;

namespace Wjybxx.BTree
{
/// <summary>
/// 行为树加载器
/// </summary>
public interface ITreeLoader
{
    /// <summary>
    /// 从资产文件中加载对象
    /// 1.加载时，通常应按照名字加载，再尝试按照guid加载 -- 名字是有规律的。
    /// 2.如果对象是一棵树，行为树的结构必须是稳定的。
    /// </summary>
    /// <param name="nameOrGuid">行为树的名字或guid</param>
    /// <returns>编辑器导出的对象</returns>
    object? TryLoadObject(string nameOrGuid);

    /// <summary>
    /// 从资产文件中加载对象，如果目标对象不存在则抛出异常
    /// </summary>
    /// <param name="nameOrGuid">行为树的名字或guid</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">目标对象不存在时</exception>
    object LoadObject(string nameOrGuid) {
        object result = TryLoadObject(nameOrGuid);
        if (result == null) {
            throw new ArgumentException("target object is absent, name: " + nameOrGuid);
        }
        return result;
    }

    /// <summary>
    /// 批量加载指定文件中的对象
    /// </summary>
    /// <param name="fileName">文件名，通常不建议带扩展后缀</param>
    /// <param name="filter">过滤器，为null则加载给定文件全部的入口对象；不要修改Entry对象的数据</param>
    /// <param name="sharable">是否共享；如果为true，则返回前不进行拷贝</param>
    /// <returns></returns>
    List<object> LoadManyFromFile(string fileName, Predicate<IEntry>? filter, bool sharable = false);

    /// <summary>
    /// 尝试加载行为树的根节点
    /// </summary>
    /// <param name="treeName">行为树的名字或guid</param>
    /// <typeparam name="T">用于类型解析</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">目标对象不是Task类型时</exception>
    Task<T>? TryLoadRootTask<T>(string treeName) where T : class {
        object result = TryLoadObject(treeName);
        if (result == null) {
            return null;
        }
        if (!(result is Task<T> task)) {
            throw new ArgumentException("target object is not a task, name: " + treeName);
        }
        return task;
    }

    /// <summary>
    /// 加载根节点为<see cref="Task{T}"/>的实例
    /// </summary>
    /// <param name="treeName">行为树的名字或guid</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    Task<T> LoadRootTask<T>(string treeName) where T : class {
        object result = TryLoadObject(treeName);
        if (result == null) {
            throw new ArgumentException("target tree is absent, name: " + treeName);
        }
        if (!(result is Task<T> task)) {
            throw new ArgumentException("target object is not a task, name: " + treeName);
        }
        return task;
    }

    /// <summary>
    /// 加载根节点为<see cref="Task{T}"/>的行为树实例
    /// </summary>
    /// <param name="treeName">行为树的名字或guid</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    TaskEntry<T> LoadTree<T>(string treeName) where T : class {
        Task<T> rootTask = LoadRootTask<T>(treeName);
        return new TaskEntry<T>(treeName, rootTask, null, this);
    }

    # region entry

    /// <summary>
    /// 编辑器中的Entry节点抽象。
    /// 接口层不处理数据和行为分离架构下的配置需求，用户在具体的Entry上处理即可。
    /// </summary>
    interface IEntry
    {
        /** 入口对象的名字(别名) -- 可能为null */
        string? Name { get; }

        /** 入口对象的guid */
        string Guid { get; }

        /** 入口对象的标记信息 */
        int Flags { get; }

        /** 入口对象的类型，通常用于表示其作用 */
        int Type { get; }

        /** 入口对象绑定的Root对象 */
        object Root { get; }
    }

    #endregion

    #region NullLoader

    /// <summary>
    /// 获取不加载对象的空加载器
    /// </summary>
    /// <returns></returns>
    static ITreeLoader NullLoader() {
        return CNullLoader.Instance;
    }

    private class CNullLoader : ITreeLoader
    {
        internal static readonly CNullLoader Instance = new CNullLoader();

        public object? TryLoadObject(string nameOrGuid) {
            return null;
        }

        public List<object> LoadManyFromFile(string fileName, Predicate<IEntry>? filter, bool sharable = false) {
            return new List<object>();
        }
    }

    #endregion
}
}