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
    /// C#的泛型测试开销较大，我们缓存下来
    /// </summary>
    private static readonly ConcurrentDictionary<TypePair, bool> _classIdPolicyCacheDic = new ConcurrentDictionary<TypePair, bool>();

    /// <summary>
    /// 测试是否需要写入对象类型信息
    /// </summary>
    /// <param name="policy">classId写入策略</param>
    /// <param name="declaredType">实例的声明类型</param>
    /// <param name="encoderType">实例的运行时类型</param>
    /// <returns></returns>
    public static bool Test(this ClassIdPolicy policy, Type declaredType, Type encoderType) {
        if (policy == ClassIdPolicy.Optimized) {
            // Nullable拆箱
            if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                declaredType = DsonConverterUtils.GetGenericArguments(declaredType)[0];
            }
            if (encoderType == declaredType) {
                return false; // 运行时类型和声明类型一致，不写入
            }
            if (declaredType == typeof(object)) {
                return true;
            }
            
            // c# 泛型的测试开销较大，我们需要缓存测试结果
            if (declaredType.IsGenericType && encoderType.IsGenericType) {
                TypePair pair = new TypePair(declaredType, encoderType);
                if (_classIdPolicyCacheDic.TryGetValue(pair, out bool r)) {
                    return r;
                }
                Type encoderGenericDefine = encoderType.GetGenericTypeDefinition();
                if (encoderGenericDefine == typeof(List<>)
                    && DsonConverterUtils.IsCollection(declaredType)) {
                    r = IsSameGenericTypeArguments(declaredType, encoderType);
                    goto next;
                }
                if (encoderGenericDefine == typeof(Dictionary<,>)
                    && DsonConverterUtils.IsDictionary(declaredType)) {
                    r = IsSameGenericTypeArguments(declaredType, encoderType);
                    goto next;
                }
                if (encoderGenericDefine == typeof(HashSet<>)
                    && DsonConverterUtils.IsSet(declaredType)) {
                    r = IsSameGenericTypeArguments(declaredType, encoderType);
                    goto next;
                }
                // 自己的集合实现
                if (encoderGenericDefine == typeof(LinkedDictionary<,>)
                    && DsonConverterUtils.IsDictionary(declaredType)) {
                    r = IsSameGenericTypeArguments(declaredType, encoderType);
                    goto next;
                }
                if (encoderGenericDefine == typeof(LinkedHashSet<>)
                    && DsonConverterUtils.IsGenericSet(declaredType)) {
                    r = IsSameGenericTypeArguments(declaredType, encoderType);
                    goto next;
                }
                r = false;

                next:
                {
                    _classIdPolicyCacheDic.TryAdd(pair, r);
                }
                return r;
            }
            return true;
        }
        return policy == ClassIdPolicy.Always;
    }

    /// <summary>
    /// 测试两个泛型类的泛型参数是否相同
    /// </summary>
    /// <param name="declaredType"></param>
    /// <param name="encoderType"></param>
    /// <returns></returns>
    private static bool IsSameGenericTypeArguments(Type declaredType, Type encoderType) {
        Type[] genericArguments1 = DsonConverterUtils.GetGenericArguments(declaredType);
        Type[] genericArguments2 = DsonConverterUtils.GetGenericArguments(encoderType);
        return genericArguments1.SequenceEqual(genericArguments2);
    }

    /// <summary>
    /// 用做缓存字典的Key
    /// </summary>
    private readonly struct TypePair : IEquatable<TypePair>
    {
        private readonly Type declaredType;
        private readonly Type encoderType;

        public TypePair(Type declaredType, Type encoderType) {
            this.declaredType = declaredType;
            this.encoderType = encoderType;
        }

        public bool Equals(TypePair other) {
            return declaredType == other.declaredType
                   && encoderType == other.encoderType;
        }

        public override bool Equals(object? obj) {
            return obj is TypePair other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(declaredType, encoderType);
        }

        public static bool operator ==(TypePair left, TypePair right) {
            return left.Equals(right);
        }

        public static bool operator !=(TypePair left, TypePair right) {
            return !left.Equals(right);
        }
    }
}
}