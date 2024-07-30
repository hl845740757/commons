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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 表示一个枚举值定义
/// (equals默认不比较文档)
/// </summary>
[Immutable]
public class EnumValueSpec : IEquatable<EnumValueSpec>, ISpecification
{
    /** 枚举名 */
    public readonly string name;
    /** 枚举关联的数字 -- 可能未定义 */
    public readonly int? number;
    /** 枚举的注释 */
    public readonly CodeBlock document;

    public EnumValueSpec(string name, int? number = null, CodeBlock? document = null) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.number = number;
        this.document = document ?? CodeBlock.Empty;
    }

    public string Name => name;
    public SpecType SpecType => SpecType.EnumValue;

    #region builder

    public static Builder NewBuilder(string name, int? number = null) {
        return new Builder(name, number);
    }

    public Builder ToBuilder() {
        return new Builder(name, number)
            .AddDocument(document);
    }

    #endregion

    #region equals

    public bool Equals(EnumValueSpec? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return name == other.name && number == other.number;
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EnumValueSpec)obj);
    }

    public override int GetHashCode() {
        return HashCode.Combine(name, number);
    }

    public override string ToString() {
        return $"{nameof(name)}: {name}, {nameof(number)}: {number}";
    }

    #endregion

    public class Builder
    {
        public readonly string name;
        public readonly int? number;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();

        internal Builder(string name, int? number) {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.number = number;
        }

        public EnumValueSpec Build() {
            return new EnumValueSpec(name, number, document.Build());
        }

        public Builder AddDocument(string format, params object[] args) {
            document.Add(format, args);
            return this;
        }

        public Builder AddDocument(CodeBlock codeBlock) {
            document.Add(codeBlock);
            return this;
        }
    }
}
}