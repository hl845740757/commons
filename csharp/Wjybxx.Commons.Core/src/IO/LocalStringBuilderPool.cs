#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
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

using System.Text;
using System.Threading;
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Commons.IO;

/// <summary>
/// 基于ThreadLocal的Builder池
/// </summary>
public class LocalStringBuilderPool : IObjectPool<StringBuilder>
{
    public static readonly LocalStringBuilderPool Instance = new();

    public StringBuilder Rent() {
        return ThreadLocalInst.Value!.Rent();
    }

    public void ReturnOne(StringBuilder sb) {
        ThreadLocalInst.Value!.ReturnOne(sb);
    }

    public void FreeAll() {
    }

    /** 获取线程本地实例 - 慎用；定义为实例方法，以免和{@link #INSTANCE}的提示冲突 */
    public StringBuilderPool LocalInst() {
        return ThreadLocalInst.Value!;
    }

    /// <summary>
    /// 每个线程缓存的StringBuilder数量
    /// 同时使用多个Builder实例的情况很少，因此只缓存少量实例即可
    /// </summary>
    private static readonly int PoolSize;
    /// <summary>
    /// StringBuilder的初始空间
    /// IO操作通常需要较大缓存空间，初始值给大一些
    /// </summary>
    private static readonly int InitCapacity;
    /// <summary>
    /// StringBuilder的最大空间
    /// 超过限定值的Builder不会被复用
    /// </summary>
    private static readonly int MaxCapacity;
    /** 封装以便我们可以在某些时候去除包装 */
    private static readonly ThreadLocal<StringBuilderPool> ThreadLocalInst;

    static LocalStringBuilderPool() {
        PoolSize = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.LocalStringBuilderPool.PoolSize", 8);
        InitCapacity = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.LocalStringBuilderPool.InitCapacity", 1024);
        MaxCapacity = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.LocalStringBuilderPool.MaxCapacity", 64 * 1024);
        ThreadLocalInst = new ThreadLocal<StringBuilderPool>(() => new StringBuilderPool(PoolSize, InitCapacity, MaxCapacity));
    }
}