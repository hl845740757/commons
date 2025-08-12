#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
/// 可选值结构
/// 
/// 1.类似<see cref="Nullable{T}"/>，但也适用于引用类型。
/// 2.避免Equals和HashCode操作。
/// 3.使用Optional以避免通过异常表示没有结果。
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Optional<T>
{
    public static Optional<T> Empty => default;

    private readonly bool _hasValue;
    private readonly T _value;

    public Optional(T value) {
        _hasValue = true;
        _value = value;
    }

    /// <summary>
    /// 是否包含值
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// 获取关联值
    /// 注：当值不存在时会抛出异常。
    /// </summary>
    public T Value {
        get {
            if (_hasValue) {
                return _value;
            }
            throw new InvalidOperationException("no value");
        }
    }

    /// <summary>
    /// 当值不存在时返回系统默认值
    /// </summary>
    /// <returns></returns>
    public T GetValueOrDefault() {
        return _hasValue ? _value : default;
    }

    /// <summary>
    /// 当值不存在时返回指定默认值
    /// </summary>
    /// <returns></returns>
    public T GetValueOrDefault(T defaultValue) {
        return _hasValue ? _value : defaultValue;
    }

    /// <summary>
    /// 隐式创建对象
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>\
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Optional<T>(T value) {
        return new Optional<T>(value);
    }

    public override string ToString() {
        if (!_hasValue) {
            return "unspecified";
        }
        return _value?.ToString() ?? "null";
    }
}
}