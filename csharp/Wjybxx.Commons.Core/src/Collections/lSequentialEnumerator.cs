﻿#region LICENSE

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

using System.Collections.Generic;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 顺序迭代器，可先测试是否还有下一个元素
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ISequentialEnumerator<out T> : IEnumerator<T>
{
    public static ISequentialEnumerator<T> Empty => EmptyEnumerator<T>.Instance;

    /// <summary>
    /// 是否还有下一个元素。
    /// 
    /// 注意：如果返回true，则MoveNext也应当返回true。
    /// </summary>
    /// <returns></returns>
    bool HasNext();
}
}