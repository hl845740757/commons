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
using System.Runtime.InteropServices;
using Wjybxx.Commons;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 对象指针
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct ObjectPtr : IEquatable<ObjectPtr>
{
    public const int MaskLocalName = 1;
    public const int MaskNamespace = 1 << 1;
    public const int MaskType = 1 << 2;

#nullable disable
    /** 引用对象的本地id */
    [FieldOffset(0)] private readonly long localId;
    /** 引用对象的本地name - 优先级高于LocalId */
    [FieldOffset(8)] private readonly string localName;
    /** 引用对象所属的命名空间 - 集合库/对象桶 */
    [FieldOffset(16)] private readonly string ns;
    /** 引用的对象的大类型 -- 给业务使用的，用于快速引用分析 */
    [FieldOffset(24)] private readonly int type;

    public ObjectPtr(long localId) {
        this.localId = localId;
        this.localName = null;
        this.ns = null;
        this.type = 0;
    }

    public ObjectPtr(long localId, string localName, string ns, int type = 0) {
        // 空字符串转null以兼容default构建的实例
        this.localId = localId;
        this.localName = ObjectUtil.EmptyToDef(localName, null);
        this.ns = ObjectUtil.EmptyToDef(ns, null);
        this.type = type;
    }
#nullable restore

    public long LocalId => localId;
    public string LocalName => localName;
    public string Namespace => ns;
    public int Type => type;

    public bool IsEmpty => LocalId == 0
                           && string.IsNullOrEmpty(localName)
                           && string.IsNullOrEmpty(ns);
    public bool CanBeAbbreviated => type == 0
                                    && string.IsNullOrEmpty(localName)
                                    && string.IsNullOrEmpty(ns);
    public bool HasLocalId => LocalId != 0;
    public bool HashLocalName => !string.IsNullOrEmpty(localName);
    public bool HasNamespace => !string.IsNullOrEmpty(ns);

    #region equals

    public bool Equals(ObjectPtr other) {
        return localId == other.localId
               && localName == other.localName
               && ns == other.ns
               && type == other.type;
    }

    public override bool Equals(object? obj) {
        return obj is ObjectPtr other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = localId.GetHashCode();
        hashCode = (hashCode * 397) ^ (localName != null ? localName.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (ns != null ? ns.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ type.GetHashCode();
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
        return $"{nameof(localId)}: {localId}, {nameof(localName)}: {localName}, {nameof(ns)}: {ns}, {nameof(type)}: {type}";
    }

    #region 常量

    public const string NamesNamespace = "ns";
    public const string NamesLocalId = "localId";
    public const string NamesLocalName = "localName";
    public const string NamesType = "type";

    #endregion
}
}