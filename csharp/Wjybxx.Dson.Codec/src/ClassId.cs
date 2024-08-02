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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 类型id。
/// 在二进制编码中，包体大小是比较重要的，因此使用数字来映射类型
/// </summary>
public readonly struct ClassId : IEquatable<ClassId>
{
    /// <summary>
    /// 默认命名空间
    /// </summary>
    public const int DEFAULT_NAMESPACE = 0;

    /// <summary>
    ///  对象的默认的类型id
    /// </summary>
    public static ClassId ObjectClassId { get; } = new ClassId(0, 0);

    private readonly int _ns;
    private readonly int _lclassId;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ns">命名空间</param>
    /// <param name="lclassId">本地id</param>
    public ClassId(int ns, int lclassId) {
        _ns = ns;
        _lclassId = lclassId;
    }

    /// <summary>
    /// 命名空间
    /// </summary>
    public int Namespace => _ns;

    /// <summary>
    /// 本地空间id
    /// </summary>
    public int LclassId => _lclassId;

    /** 是否是默认命名空间的ClassId */
    public bool IsDefaultNameSpace => _ns == 0;

    /** 创建一个默认命名空间的ClassId */
    public static ClassId OfDefaultNameSpace(int lclassId) {
        return new ClassId(0, lclassId);
    }

    /// <summary>
    /// 是否是默认classId
    /// </summary>
    /// <param name="classId"></param>
    /// <returns></returns>
    public static bool IsObjectClassId(ClassId classId) => classId.Equals(ObjectClassId);

    #region equals

    public bool Equals(ClassId other) {
        return _ns == other._ns && _lclassId == other._lclassId;
    }

    public override bool Equals(object? obj) {
        return obj is ClassId other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_ns, _lclassId);
    }

    public static bool operator ==(ClassId left, ClassId right) {
        return left.Equals(right);
    }

    public static bool operator !=(ClassId left, ClassId right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Namespace)}: {Namespace}, {nameof(LclassId)}: {LclassId}";
    }
}
}