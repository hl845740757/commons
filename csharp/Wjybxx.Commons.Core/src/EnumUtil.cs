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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons
{
/// <summary>
/// util主要解决dotnet版本问题
/// </summary>
public static class EnumUtil
{
    /// <summary>
    /// 获取所有的枚举值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] GetValues<T>() where T : struct, Enum {
#if NET6_0_OR_GREATER
        return Enum.GetValues<T>();
#else
        Array values = Enum.GetValues(typeof(T));
        T[] result = new T[values.Length];
        for (int i = 0; i < values.Length; i++) {
            T value = (T)values.GetValue(i)!;
            result[i] = value;
        }
        return result;
#endif
    }

    /// <summary>
    /// 获取所有枚举的名字
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] GetNames<T>() where T : struct, Enum {
#if NET6_0_OR_GREATER
        return Enum.GetNames<T>();
#else
        return Enum.GetNames(typeof(T));
#endif
    }

    /// <summary>
    /// 获取枚举值对应的名字
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetName<T>(T value) where T : struct, Enum {
#if NET6_0_OR_GREATER
        return Enum.GetName(value);
#else
        return Enum.GetName(typeof(T), value);
#endif
    }

    /// <summary>
    /// 获取枚举对应的int值
    /// 注意：不适用long/ulong类型枚举
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIntValue<T>(T value) where T : struct, Enum {
        // 奇技淫巧：int32/uint32/short/ushort/byte/sybte的hashcode是自身，可避免装箱。
        return value.GetHashCode();
    }
    
    /// <summary>
    /// 测试Flags是否包含指定枚举值
    /// 注意：不适用long/ulong类型枚举，只适用于可转int32的枚举
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag<T>(this int flags, T value) where T : struct, Enum {
        int hashCode = value.GetHashCode();
        return (flags & hashCode) == hashCode; //与系统库一致
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag<T>(this T flags, int value) where T : struct, Enum {
        return (flags.GetHashCode() & value) == value; // 与系统库一致
    }

    /// <summary>
    /// 是否是Flags枚举
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFlags<T>() {
        return typeof(T).IsDefined(typeof(FlagsAttribute), false);
    }
}
}