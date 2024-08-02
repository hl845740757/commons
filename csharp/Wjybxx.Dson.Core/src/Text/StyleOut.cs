#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// Dson数值转文本的结果类
/// </summary>
public readonly struct StyleOut : IEquatable<StyleOut>
{
    /** 字符串值 */
    public string Value { get; }
    /** 是否需要打印类型标签 */
    public bool IsTyped { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value">文本</param>
    /// <param name="isTyped">输出是否需要加类型</param>
    public StyleOut(string value, bool isTyped) {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        IsTyped = isTyped;
    }

    public bool Equals(StyleOut other) {
        return Value == other.Value && IsTyped == other.IsTyped;
    }

    public override bool Equals(object? obj) {
        return obj is StyleOut other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Value, IsTyped);
    }

    public static bool operator ==(StyleOut left, StyleOut right) {
        return left.Equals(right);
    }

    public static bool operator !=(StyleOut left, StyleOut right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(Value)}: {Value}, {nameof(IsTyped)}: {IsTyped}";
    }
}
}