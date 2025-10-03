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
using Wjybxx.Commons;

namespace Wjybxx.BTree
{
/// <summary>
/// 行为树加载器
/// </summary>
public interface ITreeLoader
{
    /// <summary>
    /// 从资产文件中加载对象
    /// </summary>
    /// <param name="path">对象的路径</param>
    /// <returns>编辑器导出的对象</returns>
    object? TryLoadObject(ObjectPath path);

    /// <summary>
    /// 从资产文件中加载对象，如果目标对象不存在则抛出异常
    /// </summary>
    /// <param name="path">对象的路径</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">目标对象不存在时</exception>
    object LoadObject(ObjectPath path) {
        object result = TryLoadObject(path);
        if (result == null) {
            throw new ArgumentException("target object is absent, path: " + path);
        }
        return result;
    }

    /// <summary>
    /// 尝试加载行为树的根节点
    /// </summary>
    /// <param name="path">行为树的路径</param>
    /// <typeparam name="T">用于类型解析</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">目标对象不是Task类型时</exception>
    Task<T>? TryLoadRootTask<T>(ObjectPath path) where T : class {
        object result = TryLoadObject(path);
        if (result == null) {
            return null;
        }
        if (!(result is Task<T> task)) {
            throw new ArgumentException("target object is not a task, path: " + path);
        }
        return task;
    }

    /// <summary>
    /// 加载根节点为<see cref="Task{T}"/>的实例
    /// </summary>
    /// <param name="path">行为树的路径</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    Task<T> LoadRootTask<T>(ObjectPath path) where T : class {
        object result = TryLoadObject(path);
        if (result == null) {
            throw new ArgumentException("target tree is absent, path: " + path);
        }
        if (!(result is Task<T> task)) {
            throw new ArgumentException("target object is not a task, path: " + path);
        }
        return task;
    }

    /// <summary>
    /// 注：path可能不包含name信息，因此推荐重写该方法，正确赋值行为树的name
    /// </summary>
    /// <param name="path">行为树的路径</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    TaskEntry<T> LoadTree<T>(ObjectPath path) where T : class {
        Task<T> rootTask = LoadRootTask<T>(path);
        return new TaskEntry<T>(path.localName, rootTask, null, this);
    }

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

        public object? TryLoadObject(ObjectPath path) {
            return null;
        }
    }

    #endregion
}
}