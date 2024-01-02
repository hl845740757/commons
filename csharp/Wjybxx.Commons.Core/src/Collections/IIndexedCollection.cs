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
/// 在元素身上存储了索引信息的集合。
/// 1.这类集合禁止重复添加元素，且使用引用相等判断重复
/// 2.更多用于非连续存储的集合。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IIndexedCollection<T> : IGenericCollection<T> where T : class, IIndexedElement
{
    /** 清空集合中的元素，并且不清理元素上的索引 */
    void ClearIgnoringIndexes();
}