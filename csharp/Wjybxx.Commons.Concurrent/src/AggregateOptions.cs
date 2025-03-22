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
    public readonly int successRequire;
    public readonly bool failFast;

    private AggregateOptions(byte type, int successRequire, bool failFast) {
        this.type = type;
        this.successRequire = successRequire;
        this.failFast = failFast;
    }

    public bool IsAnyOf => type == TYPE_ANY;
    public bool IsSelectAll => type == TYPE_SELECT_ALL;
    public bool IsSelectMany => type == TYPE_SELECT_MANY;

    private const byte TYPE_ANY = 0;
    private const byte TYPE_SELECT_ALL = 1;
    private const byte TYPE_SELECT_MANY = 2;

    private static readonly AggregateOptions ANY = new AggregateOptions(TYPE_ANY, 0, false);
    private static readonly AggregateOptions SELECT_ALL = new AggregateOptions(TYPE_SELECT_ALL, 0, false);
    private static readonly AggregateOptions SELECT_ALL2 = new AggregateOptions(TYPE_SELECT_ALL, 0, true);

    /// <summary>
    /// 任意一个完成
    /// </summary>
    /// <returns></returns>
    public static AggregateOptions AnyOf() {
        return ANY;
    }

    /// <summary>
    /// 全部完成
    /// </summary>
    /// <param name="failFast"></param>
    /// <returns></returns>
    public static AggregateOptions SelectAll(bool failFast) {
        return failFast ? SELECT_ALL2 : SELECT_ALL;
    }

    /// <summary>
    /// 成功完成n个
    /// </summary>
    /// <param name="futureCount">future数量</param>
    /// <param name="successRequire">需要成功完成的数量</param>
    /// <param name="failFast">是否快速失败</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static AggregateOptions SelectN(int futureCount, int successRequire, bool failFast) {
        if (futureCount < 0 || successRequire < 0) {
            throw new ArgumentException();
        }
        return new AggregateOptions(TYPE_SELECT_MANY, successRequire, failFast);
    }
}
}