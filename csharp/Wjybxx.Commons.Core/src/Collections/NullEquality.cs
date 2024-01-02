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

using System;
using System.Collections.Generic;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 用于匹配Null元素
/// </summary>
/// <typeparam name="T"></typeparam>
public class NullEquality<T> : IEqualityComparer<T>
{
    public static readonly NullEquality<T> Default = new();

    public bool Equals(T? x, T? y) {
        // 通常是因为左参为null，期望右边为null
        return (y == null) && (x == null);
    }

    public int GetHashCode(T obj) {
        throw new InvalidOperationException("Unsupported_NullEquality");
    }
}