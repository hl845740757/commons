#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using System.Collections.Generic;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 宏
/// (宏本身只是语句，通过成对的宏的来构成块)
/// </summary>
public class MacroSpec : ISpecification, IEquatable<MacroSpec>
{
    /// <summary>
    /// 宏命令
    /// </summary>
    public readonly string name;
    /// <summary>
    /// 宏参数(可能为空字符串-合法)
    /// </summary>
    public readonly IList<string> arguments;

    public MacroSpec(string name, params string[] arguments) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.arguments = Util.ToImmutableList(arguments);
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Macro;

    public static MacroSpec Get(string name, params string[] arguments) {
        return new MacroSpec(name, arguments);
    }

    #region equals

    public bool Equals(MacroSpec? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return name == other.name && Util.SequenceEqual(arguments, other.arguments);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MacroSpec)obj);
    }

    public override int GetHashCode() {
        return name.GetHashCode() * 31 + Util.HashCode(arguments);
    }

    public override string ToString() {
        return $"{nameof(name)}: {name}, {nameof(arguments)}: {Util.ToString(arguments)}";
    }

    #endregion
}
}