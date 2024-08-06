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

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 该接口用于避免集合的中元素直接实现<see cref="IIndexedElement"/>，以避免暴露不必要的接口
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IIndexedElementHelper<in T>
{
    /** 表示不在集合中的索引，也是默认索引 */
    public const int IndexNotFound = -1;

    /// <summary>
    /// 获取元素在指定集合中的索引，如果不在集合中则返回-1
    /// </summary>
    /// <param name="collection">关联的集合 </param>
    /// <param name="element">目标元素</param>
    /// <returns>元素下标</returns>
    public int CollectionIndex(object collection, T element);

    /// <summary>
    /// 设置元素在给定集合中的索引
    /// </summary>
    /// <param name="collection">关联的集合 </param>
    /// <param name="element">目标元素</param>
    /// <param name="index">新的索引；-1 表示从集合中删除</param>
    public void CollectionIndex(object collection, T element, int index);
}
}