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
    /** object值 */
    public readonly object objValue;
    /** 用于避免装箱的联合结构体 */
    public readonly UnionValue unionValue;
    /** token所在的位置，-1表示动态生成的token */
    public readonly int pos;
#nullable enable
    public DsonToken(DsonTokenType type, object? value, int pos) {
        this.type = type;
        this.objValue = value; // 这个value好像只有string类型
        this.unionValue = default;
        this.pos = pos;
    }

    public DsonToken(DsonTokenType type, in UnionValue value, int pos) {
        this.type = type;
        this.objValue = null;
        this.unionValue = value;
        this.pos = pos;
    }

    /** 将value转换为字符串值 */
    public string StringValue() {
        return (string)objValue!;
    }

    #region equals

    // Equals默认不比较位置

    public bool Equals(DsonToken other) {
        if (type != other.type) {
            return false;
        }
        // value可能是字节数组...需要处理以保证测试用例通过
        if (type == DsonTokenType.Binary) {
            byte[] src = (byte[])objValue;
            byte[] dest = (byte[])other.objValue;
            return ArrayUtil.Equals(src, dest);
        }
        return Equals(objValue, other.objValue)
               && unionValue.Equals(other.unionValue);
    }

    public override bool Equals(object? obj) {
        return obj is DsonToken other && Equals(other);
    }

    public override int GetHashCode() {
        // 不处理字节数组hash，是因为我们并不会将Token放入Set
        return HashCode.Combine((int)type, unionValue, objValue);
    }

    public static bool operator ==(DsonToken left, DsonToken right) {
        return left.Equals(right);
    }

    public static bool operator !=(DsonToken left, DsonToken right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(type)}: {type}, objValue: {objValue}, unionValue: {unionValue}, pos: {pos}";
    }
}
}