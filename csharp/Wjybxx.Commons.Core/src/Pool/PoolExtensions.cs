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

using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Pool
{
public static class PoolExtensions
{
    /// <summary>
    /// 该扩展方法用于更方便地使用using
    /// </summary>
    /// <param name="pool"></param>
    /// <param name="r"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReleaseHelper<T> Acquire<T>(this IObjectPool<T> pool, out T r) where T : class {
        r = pool.Acquire();
        return new ReleaseHelper<T>(pool, r);
    }

    /// <summary>
    /// 该扩展方法用于更方便地使用using
    /// </summary>
    /// <param name="pool">对象池</param>
    /// <param name="minLen">数组的最小长度</param>
    /// <param name="r"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReleaseHelper<T[]> Acquire<T>(this IArrayPool<T> pool, int minLen, out T[] r) {
        r = pool.Acquire(minLen);
        return new ReleaseHelper<T[]>(pool, r);
    }
}
}