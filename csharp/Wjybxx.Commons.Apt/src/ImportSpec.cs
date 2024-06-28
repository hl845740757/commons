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
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Apt;

/// <summary>
/// 表示类型导入(using xxx)
/// </summary>
[Immutable]
public class ImportSpec : IEquatable<ImportSpec>, ISpecification
{
    public static readonly ImportSpec System = new ImportSpec("System", null);

    /** 原始的命名空间 -- namespace是关键字，取名总是犯难 */
    public readonly string name;
    /** 命名空间别名 */
    public readonly string? alias;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">命名空间</param>
    /// <param name="alias">别名</param>
    public ImportSpec(string name, string? alias) {
        this.name = Util.CheckNotBlank(name, "name is blank");
        this.alias = string.IsNullOrWhiteSpace(alias) ? null : alias;
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Import;

    /// <summary>
    /// 构建子命名空间
    /// </summary>
    /// <param name="childName"></param>
    /// <param name="alias"></param>
    /// <returns></returns>
    public ImportSpec Nested(string childName, string? alias = null) {
        if (childName == null) throw new ArgumentNullException(nameof(childName));
        return new ImportSpec(name + "." + childName, alias);
    }

    #region equals

    public bool Equals(ImportSpec? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return name == other.name && alias == other.alias;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ImportSpec)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(name, alias);
    }

    public override string ToString() {
        return $"{nameof(name)}: {name}, {nameof(alias)}: {alias}";
    }

    #endregion
}