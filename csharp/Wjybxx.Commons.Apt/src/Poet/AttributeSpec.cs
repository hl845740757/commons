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
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 属性（注解）
///
/// Q：为什么要实现<see cref="ISpecification"/>？
/// A：因为注解可以在宏区间。
/// </summary>
public class AttributeSpec : ISpecification
{
    /// <summary>
    /// 属性类型
    /// </summary>
    public readonly ClassName type;
    /// <summary>
    /// 构造块赋值
    /// </summary>
    public readonly CodeBlock? constructor;
    /// <summary>
    /// 属性赋值
    /// </summary>
    public readonly IList<KeyValuePair<string, CodeBlock>> props;

    private AttributeSpec(Builder builder) {
        this.type = builder.type;
        this.constructor = builder.constructor;
        this.props = builder.props.ToImmutableList();
    }

    public string? Name => null;
    public SpecType SpecType => SpecType.Attribute;

    #region builder

    public static Builder NewBuilder(ClassName typeName) {
        return new Builder(typeName);
    }

    public static Builder NewBuilder(Type type) {
        return new Builder(ClassName.Get(type));
    }

    public Builder ToBuilder() {
        Builder builder = NewBuilder(type);
        builder.constructor = constructor;
        foreach (KeyValuePair<string, CodeBlock> pair in props) {
            builder.AddMember(pair.Key, pair.Value);
        }
        return builder;
    }

    #endregion

    public class Builder
    {
        public readonly ClassName type;
        public CodeBlock? constructor;
        public readonly Dictionary<string, CodeBlock> props = new Dictionary<string, CodeBlock>();

        internal Builder(ClassName type) {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public AttributeSpec Build() {
            return new AttributeSpec(this);
        }

        public Builder Constructor(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.constructor != null) throw new IllegalStateException("constructor was already set");
            this.constructor = codeBlock;
            return this;
        }

        public Builder AddMember(string name, string format, params object[] args) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            props[name] = CodeBlock.Of(format, args);
            return this;
        }

        public Builder AddMember(string name, CodeBlock codeBlock) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            props[name] = codeBlock;
            return this;
        }
    }
}
}