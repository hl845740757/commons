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
using System.Runtime.CompilerServices;
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
    public const int MaskCollection = 1;
    public const int MaskLocalPath = 1 << 1;
    public const int MaskType = 1 << 2;

#nullable disable
    /// <summary>
    /// 目标对象所属的集合(文件路径、资产路径、db路径)
    /// (如果为空，表示引用当前集合内的对象)
    /// </summary>
    [FieldOffset(0)] private readonly string collection;
    /// <summary>
    /// 对象在集合内的路径(或name)
    /// 
    /// 如果字段不为空，则优先使用localPath查找对象，即localPath的优先级高于localId；
    /// 因为localPath更具有可读性，更适合手工引用对象。
    /// </summary>
    [FieldOffset(8)] private readonly string localPath;
    /// <summary>
    /// 对象在集合内的id
    /// (如果目标集合是数组，则可能是下标) 
    /// </summary>
    [FieldOffset(16)] private readonly long localId;
    /// <summary>
    /// 引用类型
    /// (用于引用分析，也可以表示如何解析引用等)
    /// </summary>
    [FieldOffset(24)] private readonly int type;

    public ObjectPtr(long localId) {
        this.localId = localId;
        this.collection = null;
        this.localPath = null;
        this.type = 0;
    }

    public ObjectPtr(string collection, string localPath, long localId, int type = 0) {
        // 空字符串转null以兼容default构建的实例
        this.collection = ObjectUtil.EmptyToDef(collection, null);
        this.localPath = ObjectUtil.EmptyToDef(localPath, null);
        this.localId = localId;
        this.type = type;
    }
#nullable restore

    public string Collection => collection;
    public string LocalPath => localPath;
    public long LocalId => localId;
    public int Type => type;

    public bool IsEmpty => localId == 0
                           && string.IsNullOrEmpty(localPath)
                           && string.IsNullOrEmpty(collection);
    public bool CanBeAbbreviated => type == 0
                                    && string.IsNullOrEmpty(localPath)
                                    && string.IsNullOrEmpty(collection);
    public bool HasCollection => !string.IsNullOrEmpty(collection);
    public bool HashLocalPath => !string.IsNullOrEmpty(localPath);
    public bool HasLocalId => localId != 0;

    #region equals

    public bool Equals(ObjectPtr other) {
        return localId == other.localId
               && localPath == other.localPath
               && collection == other.collection
               && type == other.type;
    }

    public override bool Equals(object? obj) {
        return obj is ObjectPtr other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = localId.GetHashCode();
        hashCode = (hashCode * 397) ^ (localPath != null ? localPath.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (collection != null ? collection.GetHashCode() : 0);
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
        return $"{nameof(localId)}: {localId}, {nameof(localPath)}: {localPath}, {nameof(collection)}: {collection}, {nameof(type)}: {type}";
    }

    #region 常量

    public const string NamesCollection = "coll";
    public const string NamesLocalPath = "localPath";
    public const string NamesLocalId = "localId";
    public const string NamesType = "type";

    #endregion

    #region 隐式转换

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ObjectPath(ObjectPtr ptr) {
        return new ObjectPath()
        {
            collection = ptr.collection,
            localPath = ptr.localPath,
            localId = ptr.localId,
            type = ptr.type
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ObjectPtr(ObjectPath path) {
        return new ObjectPtr(path.collection, path.localPath, path.localId, path.type);
    }

    #endregion
}
}