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

using System;

namespace Wjybxx.Commons.Concurrent
{
internal readonly struct AggregateOptions
{
    private readonly byte type;
    public readonly int required;
    public readonly bool failFast;

    private AggregateOptions(byte type, int required, bool failFast) {
        this.type = type;
        this.required = required;
        this.failFast = failFast;
    }

    public bool IsWhenAny => type == TYPE_ANY;
    public bool IsWhenAll => type == TYPE_WHEN_ALL;

    private const byte TYPE_ANY = 0;
    private const byte TYPE_WHEN_ALL = 1;
    private const byte TYPE_SELECT_MANY = 2;

    private static readonly AggregateOptions WHEN_ANY = new AggregateOptions(TYPE_ANY, 0, false);
    private static readonly AggregateOptions WHEN_ALL = new AggregateOptions(TYPE_WHEN_ALL, 0, false);

    /// <summary>
    /// 任意一个完成
    /// </summary>
    /// <returns></returns>
    public static AggregateOptions WhenAny() {
        return WHEN_ANY;
    }

    /// <summary>
    /// 所有任务完成（无快速失败逻辑）
    /// </summary>
    /// <returns></returns>
    public static AggregateOptions WhenAll() {
        return WHEN_ALL;
    }

    /// <summary>
    /// 成功完成n个
    /// </summary>
    /// <param name="required">需要成功完成的数量</param>
    /// <param name="failFast">是否快速失败</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static AggregateOptions SelectN(int required, bool failFast) {
        if (required < 0) {
            throw new ArgumentException("required cannot be negative");
        }
        return new AggregateOptions(TYPE_SELECT_MANY, required, failFast);
    }
}
}