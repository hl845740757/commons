#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 动态数组
/// (支持迭代期间删除元素和扩容)
/// <h3>约定</h3>
/// 0.Set(index, null) 即表示删除元素
/// 1.Add和Insert一定会增加length；但Remove和Set方法不一定会减少length。
/// 2.在迭代期间删除元素，一定不会减少length；在迭代结束后，可能会压缩空间减少length -- 允许一定比例的null是该List的核心；
/// 3.在迭代期间添加元素，元素会被添加到List末尾并增加length；在迭代结束后，可能会压缩空间减少length。
/// 4.在非迭代期间，删除元素可能立即触发空间压缩。
/// 5.需要使用传统数组方式进行迭代，因此未实现<see cref="IEnumerable{T}"/>}接口。
///
/// <h3>使用方式</h3>
/// <![CDATA[
///     list.BeginItr();
///     try {
///         for(int i = 0, length = list.Length; i < length; i++){
///              E e = list[i];
///              if (e == null) {
///                  continue;
///              }
///              DoSomething(e);
///         }
///     } finally {
///         list.EndItr();
///     }
/// ]]>
/// 
/// </summary>
/// <typeparam name="E"></typeparam>
public interface IDynamicArray<E> where E : class
{
    /// <summary>
    /// 当前是否正在迭代
    /// </summary>
    bool IsIterating { get; }

    /// <summary>
    /// 开始迭代
    /// </summary>
    void BeginItr();

    /// <summary>
    /// 迭代结束 -- 特殊情况下可以反复调用该接口修复状态
    /// </summary>
    void EndItr();

    /// <summary>
    /// 获取指定位置的元素
    /// </summary>
    /// <param name="index">数组下标</param>
    E? this[int index] { get; set; }

    /// <summary>
    /// 设置指定位置的元素，同时返回旧值
    ///
    /// Set为Null即表示删除元素。
    /// </summary>
    /// <param name="index">数组下标</param>
    /// <param name="e">新值</param>
    /// <returns>当前位置的旧值</returns>
    E? Set(int index, E? e);

    /// <summary>
    /// 添加元素
    /// 不论是否正在迭代，len一定会增加。
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="NullReferenceException">如果e为null</exception>
    void Add(E e);

    /// <summary>
    /// 插入元素
    /// 
    /// </summary>
    /// <param name="index">要插入的位置，小于等于length</param>
    /// <param name="e">要插入的元素</param>
    /// <exception cref="NullReferenceException">如果e为null</exception>
    /// <exception cref="InvalidOperationException">如果当前正在迭代</exception>
    void Insert(int index, E e);

    /// <summary>
    /// 根据equals相等删除元素
    /// 注意：不论是否正在迭代，length都可能不会变化。
    /// </summary>
    /// <param name="e">null固定返回false</param>
    /// <returns>如果元素在集合中则删除并返回true</returns>
    bool Remove(E? e);

    /// <summary>
    /// 根据引用相等删除元素
    /// 注意：不论是否正在迭代，length都可能不会变化。
    /// </summary>
    /// <param name="e">null固定返回false</param>
    /// <returns>如果元素在集合中则删除并返回true</returns>
    bool RemoveRef(E? e);

    /// <summary>
    /// 清空List
    /// 注意：
    /// 1.在迭代期间调用Clear是高风险行为，会清理自身迭代范围外的数据，可能影响其它迭代逻辑。
    /// 2.在迭代期间清理元素不会更新length
    /// </summary>
    void Clear();

    /// <summary>
    /// 基于equals查询一个元素是否在List中
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    bool Contains(E? e);

    /// <summary>
    /// 基于引用相等查询一个元素是否在List中
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    bool ContainsRef(E? e);

    /// <summary>
    /// 根据equals查询下标
    /// </summary>
    /// <param name="e"></param>
    /// <returns>如果存在则返回对应的下标，否则返回-1</returns>
    int IndexOf(E? e);

    /// <summary>
    /// 根据equals查询下标
    /// </summary>
    /// <param name="e"></param>
    /// <returns>如果存在则返回对应的下标，否则返回-1</returns>
    int LastIndexOf(E? e);

    /// <summary>
    /// 根据引用相等查询下标
    /// </summary>
    /// <param name="e"></param>
    /// <returns>如果存在则返回对应的下标，否则返回-1</returns>
    int IndexOfRef(E? e);

    /// <summary>
    /// 根据引用相等查询下标
    /// </summary>
    /// <param name="e"></param>
    /// <returns>如果存在则返回对应的下标，否则返回-1</returns>
    int LastIndexOfRef(E? e);

    /// <summary>
    /// 数组的当前长度
    /// </summary>
    int Length { get; }

    /// <summary>
    /// 非空元素数量，实时值。
    /// 注意：该方法可能有额外的开销
    /// </summary>
    int ElementCount { get; }

    /// <summary>
    /// 空元素数量，实时值。
    /// 注意：该方法可能有额外的开销
    /// </summary>
    int NullCount { get; }

    /// <summary>
    /// 判断数组是否包含Null元素，用于更快的判断是否为空。
    /// 注意：该方法可能有额外的开销。
    /// </summary>
    bool ContainsNull { get; }

    /// <summary>
    /// 对数组元素进行排序
    /// (该接口会强制压缩空间，再进行排序)
    /// </summary>
    /// <param name="comparator"></param>
    /// <exception cref="InvalidOperationException">如果当前正在迭代</exception>
    void Sort(IComparer<E> comparator);

    /// <summary>
    /// 确保空间足够（减少不必要的扩容）
    /// </summary>
    /// <param name="minCapacity">期望的最小空间</param>
    void EnsureCapacity(int minCapacity);

    /// <summary>
    /// 压缩数组
    /// 
    /// </summary>
    /// <param name="ignoreFactor">是否忽略null比重</param>
    /// <exception cref="InvalidOperationException">如果当前正在迭代</exception>
    void Compress(bool ignoreFactor);

    /// <summary>
    /// 迭代数组内的元素，该快捷方式不会迭代迭代期间新增的元素
    /// </summary>
    /// <param name="action">接收元素和对应的下标</param>
    void ForEach(Action<E, int> action);

    /// <summary>
    /// 将所有的非null元素转存到List
    /// </summary>
    /// <returns></returns>
    List<E> ToList();

    /// <summary>
    /// 转换为Span
    /// </summary>
    /// <returns></returns>
    Span<E?> AsSpan();
}
}