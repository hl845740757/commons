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

using System.Buffers;

namespace Wjybxx.Commons.Pool
{
/// <summary>
/// 数组池抽象
///
/// C#的原生接口<see cref="ArrayPool{T}"/>有点问题，看似在归还到池中的时候选择清理是合理的，实际上是有问题的；
/// 对于共享池来说，如果一部分操作进行清理，而另一部分不进行清理，那么清理操作就是没有意义的 —— 因为无法保证租借到的对象是安全的。
/// 对于不受信任的池来说，只有在租借的时候进行清理才能保证安全性。
/// </summary>
/// <typeparam name="T">数组元素的类型</typeparam>
public interface IArrayPool<T> : IObjectPool<T[]>
{
    /// <summary>
    /// 共享数组池
    /// 1. 默认最大只保存1M大小的数组
    /// 2. 默认不执行清理(稳定API)
    /// </summary>
    public static IArrayPool<T> Shared { get; } = new ArrayPoolAdapter<T>(ArrayPool<T>.Shared, 4096);

    /// <summary>
    /// 从池中租借一个数组
    /// 1.返回的字节数组可能大于期望的数组长度
    /// 2.默认情况下不清理
    /// </summary>
    /// <param name="minimumLength">期望的最小数组长度</param>
    /// <param name="clear">返回前是否先清理；只有当前池默认不清理的情况下，该参数才有效用。</param>
    /// <returns>池化的字节数组</returns>
    T[] Acquire(int minimumLength, bool clear = false);

    /// <summary>
    /// 归还一个数组到池中 - 可选择清理
    /// </summary>
    /// <param name="array">租借的数组</param>
    /// <param name="clear">是否需要清理；只有当前池默认不清理的情况下，该参数才有效用。</param>
    /// <returns></returns>
    void Release(T[] array, bool clear);
}
}