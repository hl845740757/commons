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

using System.Collections.Generic;

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 允许在迭代期间删除元素的迭代器
/// (在C#中，迭代器默认是不可删除元素的，因此该接口是不安全的)
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IUnsafeIterator<out T> : IEnumerator<T>
{
    /// <summary>
    /// 删除当前元素
    /// </summary>
    void Remove();
}