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

namespace Wjybxx.Commons.Concurrent;

internal struct AggregateOptions
{
    private readonly bool anyOf;
    public readonly int successRequire;
    public readonly bool failFast;

    AggregateOptions(bool anyOf, int successRequire, bool failFast) {
        this.anyOf = anyOf;
        this.successRequire = successRequire;
        this.failFast = failFast;
    }

    public bool IsAnyOf => anyOf;

    private static readonly AggregateOptions ANY = new AggregateOptions(true, 0, false);

    /// <summary>
    /// 任意一个完成
    /// </summary>
    /// <returns></returns>
    public static AggregateOptions AnyOf() {
        return ANY;
    }

    /// <summary>
    /// 成功完成n个
    /// </summary>
    /// <param name="successRequire">需要成功完成的数量</param>
    /// <param name="failFast">是否快速失败</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static AggregateOptions SelectN(int successRequire, bool failFast = true) {
        if (successRequire < 0) {
            throw new ArgumentException("successRequire < 0");
        }
        return new AggregateOptions(false, successRequire, failFast);
    }
}