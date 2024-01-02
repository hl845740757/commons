#region LICENSE

//  Copyright 2023 wjybxx
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

namespace Wjybxx.Commons.Collections;

/// <summary>
/// AddFirst在元素已存在时将移动到Set的首部
/// AddLast在元素已存在时将移动到Set的尾部
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISequencedSet<T> : ISequencedCollection<T>, IGenericSet<T>
{
    /// <summary>
    /// 返回一个当前集合的逆序视图
    /// </summary>
    /// <returns></returns>
    new ISequencedSet<T> Reversed();

    /// <summary>
    /// 添加元素到Set的首部；
    /// 如果是新元素，则返回true，如果元素已存在，则移动到首部。
    /// </summary>
    /// <param name="item"></param>
    /// <returns>如果是新元素则返回true，否则返回false</returns>
    new bool AddFirst(T item);

    /// <summary>
    /// 添加元素到Set的尾部。
    /// 如果是新元素，则返回true，如果元素已存在，则移动到尾部。
    /// </summary>
    /// <param name="item"></param>
    /// <returns>如果是新元素则返回true，否则返回false</returns>
    new bool AddLast(T item);

    /// <summary>
    /// 仅在元素不存在的情况下将元素添加到首部
    /// </summary>
    /// <param name="item"></param>
    /// <returns>如果是新元素则返回true，否则返回false</returns>
    bool AddFirstIfAbsent(T item);

    /// <summary>
    /// 仅在元素不存在的情况下将元素添加到尾部
    /// </summary>
    /// <param name="item"></param>
    /// <returns>如果是新元素则返回true，否则返回false</returns>
    bool AddLastIfAbsent(T item);

    #region 接口适配

    ISequencedCollection<T> ISequencedCollection<T>.Reversed() {
        return Reversed();
    }

    void ISequencedCollection<T>.AddFirst(T item) {
        AddFirst(item);
    }

    void ISequencedCollection<T>.AddLast(T item) {
        AddLast(item);
    }

    #endregion
}