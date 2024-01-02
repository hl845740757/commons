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

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 被索引的元素
/// 1.索引信息存储在元素上，可大幅提高查找效率；
/// 2.如果对象可能存在多个集合中，慎重实现该接口，更建议为每个集合设置一个粘合对象；
/// </summary>
public interface IIndexedElement
{
    /** 表示不在集合中的索引，也是默认索引 */
    public const int IndexNotFound = -1;

    /// <summary>
    /// 获取元素在指定集合中的索引，如果不在集合中则返回-1
    /// </summary>
    public int CollectionIndex(object collection);

    /// <summary>
    /// 设置元素在给定集合中的索引
    /// </summary>
    /// <param name="collection">关联的集合 </param>
    /// <param name="index">新的索引；-1 表示从集合中删除</param>
    public void CollectionIndex(object collection, int index);
}