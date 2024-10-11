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
using System.Linq;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// <see cref="ClassIdPolicy"/>的扩展方法
/// </summary>
public static class ClassIdPolicyExts
{
    /// <summary>
    /// C#的泛型测试开销较大，我们需要缓存测试结果；另外，也为了支持用户配置。
    /// </summary>
    private static readonly ConcurrentDictionary<CacheKey, bool> cacheDic = new ConcurrentDictionary<CacheKey, bool>();

    /// <summary>
    /// 注意：
    /// 1.如果声明类型是泛型类，仅支持编码类型也是泛型类
    /// 2.通常当真实类型是声明类型的默认实例类型时，可指定不编码类型信息
    /// </summary>
    /// <param name="declaredType">声明类型</param>
    /// <param name="encoderType">实现类型</param>
    /// <param name="value">是否写入类型信息</param>
    /// <returns></returns>
    public static void AddCache(Type declaredType, Type encoderType, bool value) {
        CacheKey key = new CacheKey(declaredType, encoderType);
        cacheDic[key] = value;
    }

    /// <summary>
    /// 测试是否需要写入对象类型信息
    /// </summary>
    /// <param name="policy">classId写入策略</param>
    /// <param name="declaredType">实例的声明类型</param>
    /// <param name="encoderType">实例的运行时类型，一定不是Nullable</param>
    /// <returns>是否需要写入类型信息</returns>
    public static bool Test(this ClassIdPolicy policy, Type declaredType, Type encoderType) {
        if (policy == ClassIdPolicy.Optimized) {
            // Nullable拆箱，结构体由于不能继承，因此泛型参数必定和EncoderType相等
            if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                return false;
            }
            if (encoderType == declaredType) return false;
            if (declaredType == typeof(object)) return true;

            CacheKey key = new CacheKey(declaredType, encoderType);
            if (cacheDic.TryGetValue(key, out bool r)) {
                return r;
            }
            // 泛型之间可能需要动态测试
            if (declaredType.IsGenericType && encoderType.IsGenericType) {
                r = TestGenericType(declaredType, encoderType);
                cacheDic.TryAdd(key, r);
                return r;
            }
            return true;
        }
        return policy == ClassIdPolicy.Always;
    }

    private static bool TestGenericType(Type declaredType, Type encoderType) {
        Type declaredGenericDefine = declaredType.GetGenericTypeDefinition();
        Type encoderGenericDefine = encoderType.GetGenericTypeDefinition();
        // 如果泛型原型之间设置为必须写入，则必须写入；如果泛型原型之间设置为无需写入，则测试泛型参数是否相同
        {
            CacheKey key = new CacheKey(declaredGenericDefine, encoderGenericDefine);
            if (cacheDic.TryGetValue(key, out bool r)) {
                return r || IsGenericTypeArgumentsDifferent(declaredType, encoderType);
            }
        }
        // 默认类型测试，系统集合库
        if (encoderGenericDefine == typeof(List<>)
            && DsonConverterUtils.IsCollection(declaredGenericDefine)) {
            return IsGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        if (encoderGenericDefine == typeof(Dictionary<,>)
            && DsonConverterUtils.IsDictionary(declaredGenericDefine)) {
            return IsGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        if (encoderGenericDefine == typeof(HashSet<>)
            && DsonConverterUtils.IsSet(declaredGenericDefine)) {
            return IsGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        // 自己的集合实现
        if (encoderGenericDefine == typeof(LinkedDictionary<,>)
            && DsonConverterUtils.IsDictionary(declaredGenericDefine)) {
            return IsGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        if (encoderGenericDefine == typeof(LinkedHashSet<>)
            && DsonConverterUtils.IsGenericSet(declaredGenericDefine)) {
            return IsGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        return true;
    }

    /** 泛型参数是否不同 */
    private static bool IsGenericTypeArgumentsDifferent(Type declaredType, Type encoderType) {
        Type[] genericArguments1 = declaredType.GenericTypeArguments;
        Type[] genericArguments2 = encoderType.GenericTypeArguments;
        return !genericArguments1.SequenceEqual(genericArguments2);
    }

    /// <summary>
    /// 用做缓存字典的Key
    /// </summary>
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        private readonly Type declaredType;
        private readonly Type encoderType;

        public CacheKey(Type declaredType, Type encoderType) {
            this.declaredType = declaredType;
            this.encoderType = encoderType;
        }

        public bool Equals(CacheKey other) {
            return declaredType == other.declaredType
                   && encoderType == other.encoderType;
        }

        public override bool Equals(object? obj) {
            return obj is CacheKey other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(declaredType, encoderType);
        }

        public static bool operator ==(CacheKey left, CacheKey right) {
            return left.Equals(right);
        }

        public static bool operator !=(CacheKey left, CacheKey right) {
            return !left.Equals(right);
        }
    }
}
}