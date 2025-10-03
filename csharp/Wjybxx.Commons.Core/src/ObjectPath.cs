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

namespace Wjybxx.Commons
{
/// <summary>
/// 资产对象路径(指针)
///
/// 注：该对象是Dson库中的ObjectPtr的替代物，用于避免引入Dson库。
/// </summary>
[Serializable]
public struct ObjectPath
{
#nullable disable
    /// <summary>
    /// 资产路径
    /// (如果为空，表示引用当前资产内的对象)
    /// </summary>
    public string assetPath;
    /// <summary>
    /// 对象在资产内的名字
    /// (如果name不为空，则使用name查找对象，即localName的优先级高于localId)
    /// </summary>
    public string localName;
    /// <summary>
    /// 对象在资产内的id
    /// (如果目标资产是数组，则可能是下标)
    /// </summary>
    public long localId;
    /// <summary>
    /// 引用的类型
    /// </summary>
    public int type;

    public ObjectPath(string assetPath, string localName, long localId, int type = 0) {
        // 空字符串转null以兼容default构建的实例
        this.assetPath = ObjectUtil.EmptyToDef(assetPath, null);
        this.localName = ObjectUtil.EmptyToDef(localName, null);
        this.localId = localId;
        this.type = type;
    }
#nullable restore

    public bool IsEmpty => localId == 0
                           && string.IsNullOrEmpty(localName)
                           && string.IsNullOrEmpty(assetPath);

    public bool HasLocalId => localId != 0;
    public bool HashLocalName => !string.IsNullOrEmpty(localName);
    public bool HasAssetPath => !string.IsNullOrEmpty(assetPath);

    #region equals

    public bool Equals(ObjectPath other) {
        return localId == other.localId
               && localName == other.localName
               && assetPath == other.assetPath
               && type == other.type;
    }

    public override bool Equals(object? obj) {
        return obj is ObjectPath other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = localId.GetHashCode();
        hashCode = (hashCode * 397) ^ (localName != null ? localName.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (assetPath != null ? assetPath.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ type.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(ObjectPath left, ObjectPath right) {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectPath left, ObjectPath right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(assetPath)}: {assetPath}, {nameof(localName)}: {localName}, {nameof(localId)}: {localId}, {nameof(type)}: {type}";
    }
}
}