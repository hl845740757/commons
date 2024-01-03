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

    public void Clear() {
    }

    /// <summary>
    /// 同时使用多个Builder实例的情况很少，因此只缓存少量实例即可
    /// </summary>
    private const int PoolSize = 8;
    /// <summary>
    /// IO操作通常需要较大缓存空间，初始值给大一些
    /// </summary>
    private const int InitCapacity = 4096;

    private static readonly ThreadLocal<StringBuilderPool> ThreadLocalInst = new(
        () => new StringBuilderPool(PoolSize, InitCapacity)
    );
}