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

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 对象指针
/// </summary>
public readonly struct ObjectPtr : IEquatable<ObjectPtr>
{
    public const int MaskNamespace = 1;
    public const int MaskType = 1 << 1;
    public const int MaskPolicy = 1 << 2;

#nullable disable
    /** 引用对象的本地id - 如果目标对象是容器中的一员，该值是其容器内编号 */
    public string LocalId { get; }
    /** 引用对象所属的命名空间 */
    public string Namespace { get; }
    /** 引用的对象的大类型 -- 给业务使用的，用于快速引用分析 */
    public byte Type { get; }
    /** 引用的解析策略 -- 自定义解析规则 */
    public byte Policy { get; }
#nullable restore

    public ObjectPtr(string? localId, string? ns = null, byte type = 0, byte policy = 0) {
        // 空字符串转null而不是null转空字符串，以兼容default构建的实例
        this.LocalId = ObjectUtil.EmptyToDef(localId, null);
        this.Namespace = ObjectUtil.EmptyToDef(ns, null);
        this.Type = type;
        this.Policy = policy;

        if (IsEmpty && (type != 0 || policy != 0)) {
            throw new IllegalStateException();
        }
    }

    public bool CanBeAbbreviated => string.IsNullOrWhiteSpace(Namespace) && Type == 0 && Policy == 0;

    public bool IsEmpty => string.IsNullOrWhiteSpace(LocalId) && string.IsNullOrWhiteSpace(Namespace);

    public bool HasLocalId => !string.IsNullOrWhiteSpace(LocalId);

    public bool HasNamespace => !string.IsNullOrWhiteSpace(Namespace);

    #region equals

    public bool Equals(ObjectPtr other) {
        return LocalId == other.LocalId && Namespace == other.Namespace && Type == other.Type && Policy == other.Policy;
    }

    public override bool Equals(object? obj) {
        return obj is ObjectPtr other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = (LocalId != null ? LocalId.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ Type.GetHashCode();
        hashCode = (hashCode * 397) ^ Policy.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(ObjectPtr left, ObjectPtr right) {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectPtr left, ObjectPtr right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(LocalId)}: {LocalId}, {nameof(Namespace)}: {Namespace}, {nameof(Type)}: {Type}, {nameof(Policy)}: {Policy}";
    }

    #region 常量

    public const string NamesNamespace = "ns";
    public const string NamesLocalId = "localId";
    public const string NamesType = "type";
    public const string NamesPolicy = "policy";

    #endregion
}
}