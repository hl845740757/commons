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

namespace Wjybxx.Commons
{
/// <summary>
/// 类型工具类
/// <see cref="Type.GetGenericArguments"/>拿到的是编译时类型，可能是真实类型，也可能是T这样的泛型参数。
/// <see cref="Type.GenericTypeArguments"/>拿到的是运行时类型 -- 可能为空数组。
/// </summary>
public static class TypeUtil
{
    /// <summary>
    /// 获取的Type的简单名，不包含
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetSimpleName(this Type type) {
        string typeName = type.Name;
        int idx = typeName.IndexOf('`');
        if (idx < 0) {
            return typeName;
        }
        return typeName.Substring(0, idx);
    }
}
}