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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Wjybxx.Commons;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 用于实现不同Converter不同的数据
/// </summary>
public sealed class TypeWriteHelper
{
    /// <summary>
    /// 这里包含用户配置的非泛型类型之间和泛型原型之间的关系，
    /// 同时包含了运行时类型之间的结果缓存，虽然小数组的equals比较很块，但比较泛型参数会产生临时数组，
    /// 因此我们缓存所有的类型数据。
    /// </summary>
    private readonly ConcurrentDictionary<TypePair, bool> cacheDic = new ConcurrentDictionary<TypePair, bool>();

    public TypeWriteHelper(IDictionary<TypePair, bool> configs) {
        foreach (KeyValuePair<TypePair, bool> pair in configs) {
            cacheDic[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="encoderType">实例的运行时类型，一定不是Nullable</param>
    /// <param name="declaredType">实例的声明类型</param>
    /// <returns>是否可进行优化</returns>
    public bool IsOptimizable(Type encoderType, Type declaredType) {
        // Nullable拆箱，结构体由于不能继承，因此泛型参数必定和EncoderType相等
        if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            return true;
        }
        if (encoderType == declaredType) return true;
        if (declaredType == typeof(object)) return false;

        if (encoderType.IsGenericType) {
            if (!declaredType.IsGenericType) {
                return false;
            }
            // 都是泛型
            // C#端不仅仅缓存了泛型原型之间的关系，还缓存了已构造泛型之间的关系
            TypePair key = new TypePair(encoderType, declaredType);
            if (cacheDic.TryGetValue(key, out bool val)) {
                return val;
            }
            // 如果泛型原型之间配置了可优化，则泛型参数相同时可优化
            key = new TypePair(encoderType.GetGenericTypeDefinition(), declaredType.GetGenericTypeDefinition());
            if (cacheDic.TryGetValue(key, out val)) {
                // 这里的比较会产生小数组，也是我们要缓存最终结果的原因
                val = val && ArrayUtil.Equals(encoderType.GenericTypeArguments, declaredType.GenericTypeArguments);
                cacheDic.TryAdd(new TypePair(encoderType, declaredType), val);
                return val;
            }
            return false;
        } else {
            if (declaredType.IsGenericType) {
                return false;
            }
            // 都不是泛型，如果配置了可优化，则可优化
            TypePair key = new TypePair(encoderType, declaredType);
            return cacheDic.TryGetValue(key, out bool val) && val;
        }
    }
}
}