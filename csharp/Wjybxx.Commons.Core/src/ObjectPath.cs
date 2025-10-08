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
public struct ObjectPath : IEquatable<ObjectPath>
{
#nullable disable
    /// <summary>
    /// 目标对象所属的集合(文件路径、资产路径、db路径)
    /// (如果为空，表示引用当前集合内的对象)
    /// </summary>
    public string collection;
    /// <summary>
    /// 对象在集合内的路径(或name)
    /// 
    /// 如果字段不为空，则优先使用localPath查找对象，即localPath的优先级高于localId；
    /// 因为localPath更具有可读性，更适合手工引用对象。
    /// </summary>
    public string localPath;
    /// <summary>
    /// 对象在集合内的id
    /// (如果目标集合是数组，则可能是下标) 
    /// </summary>
    public long localId;
    /// <summary>
    /// 引用类型
    /// (用于引用分析，可以嵌入信息，表示如何解析引用等)
    /// </summary>
    public int type;

    public ObjectPath(long localId) {
        this.localId = localId;
        this.collection = null;
        this.localPath = null;
        this.type = 0;
    }

    public ObjectPath(string collection, string localPath, long localId, int type = 0) {
        // 空字符串转null以兼容default构建的实例
        this.collection = ObjectUtil.EmptyToDef(collection, null);
        this.localPath = ObjectUtil.EmptyToDef(localPath, null);
        this.localId = localId;
        this.type = type;
    }
#nullable restore

    public bool IsEmpty => localId == 0
                           && string.IsNullOrEmpty(localPath)
                           && string.IsNullOrEmpty(collection);
    public bool HasCollection => !string.IsNullOrEmpty(collection);
    public bool HashLocalPath => !string.IsNullOrEmpty(localPath);
    public bool HasLocalId => localId != 0;

    #region equals

    public bool Equals(ObjectPath other) {
        return localId == other.localId
               && localPath == other.localPath
               && collection == other.collection
               && type == other.type;
    }

    public override bool Equals(object? obj) {
        return obj is ObjectPath other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = localId.GetHashCode();
        hashCode = (hashCode * 397) ^ (localPath != null ? localPath.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (collection != null ? collection.GetHashCode() : 0);
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
        return $"{nameof(collection)}: {collection}, {nameof(localPath)}: {localPath}, {nameof(localId)}: {localId}, {nameof(type)}: {type}";
    }
}
}