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

using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.IO;

/// <summary>
/// 类数组(ArrayLike)对象池抽象
///
/// 类数组的定义：对象和数组一样固定长度(空间)，不可自动扩容，常见于数组的封装类。
/// </summary>
/// <typeparam name="T">数组元素的类型</typeparam>
public interface IArrayLikePool<T> : IObjectPool<T>
{
    /// <summary>
    /// 从池中租借一个对象
    /// 1.返回的对象的空间可能大于期望的长度
    /// 2.默认情况下不清理
    /// </summary>
    /// <param name="minimumLength">期望的最小数组长度</param>
    /// <returns>池化的字节数组</returns>
    T Acquire(int minimumLength);
}