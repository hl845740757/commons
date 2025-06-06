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
using Wjybxx.Commons;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// Dson文本token
/// (值类型小心使用)
/// </summary>
public readonly struct DsonToken : IEquatable<DsonToken>
{
#nullable disable
    /** token的类型 */
    public readonly DsonTokenType type;
    /** 用于避免装箱的联合结构体 */
    public readonly UnionValue value;
    /** token所在的位置，-1表示动态生成的token */
    public readonly int pos;
#nullable enable
    public DsonToken(DsonTokenType type, in UnionValue value, int pos) {
        this.type = type;
        this.value = value;
        this.pos = pos;
    }

    // String的情况比较多，提供快捷方式
    public DsonToken(DsonTokenType type, string? value, int pos) {
        this.type = type;
        this.value = new UnionValue(DsonType.String, value);
        this.pos = pos;
    }

    /** 将value转换为字符串值 */
    public string StringValue() {
        return (string)value.objValue!;
    }

    /** 将value转换为字符串值；如果字符串是无引号字符串null，则返回null */
    public string? NullableStringValue() {
        string str = (string)this.value.objValue;
        if (type == DsonTokenType.UnquoteString && "null" == str) {
            return null;
        }
        return str;
    }

    #region equals

    // Equals默认不比较位置

    public bool Equals(DsonToken other) {
        return type == other.type && value.Equals(other.value);
    }

    public override bool Equals(object? obj) {
        return obj is DsonToken other && Equals(other);
    }

    public override int GetHashCode() {
        // 不处理字节数组hash，是因为我们并不会将Token放入Set
        return HashCode.Combine((int)type, value);
    }

    public static bool operator ==(DsonToken left, DsonToken right) {
        return left.Equals(right);
    }

    public static bool operator !=(DsonToken left, DsonToken right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(type)}: {type}, value: {value}, pos: {pos}";
    }
}
}