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
using System.Collections.Immutable;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Apt;

/// <summary>
/// 命名空间
/// 
/// Q: 为什么要显式支持？
/// A: Unity不支持文件范围命名空间...
/// </summary>
[Immutable]
public class NamespaceSpec : ISpecification
{
    public readonly string name;
    public readonly IList<ISpecification> nestedSpecs;

    private NamespaceSpec(string name, IList<ISpecification> nestedSpecs) {
        this.name = Util.CheckNotBlank(name, "name is blank");
        this.nestedSpecs = Util.ToImmutableList(nestedSpecs);
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Namespace;

    #region builder

    public static NamespaceSpec Of(string name, params ISpecification[] nestedSpecs) {
        return new NamespaceSpec(name, ImmutableList.CreateRange(nestedSpecs));
    }

    public static NamespaceSpec Of(string name, IList<ISpecification> nestedSpecs) {
        return new NamespaceSpec(name, nestedSpecs);
    }

    public static Builder NewBuilder(string name) => new Builder(name);

    public Builder ToBuilder() {
        return new Builder(name)
            .AddSpecs(nestedSpecs);
    }

    #endregion

    public class Builder
    {
        public readonly string name;
        public readonly List<ISpecification> nestedSpecs = new List<ISpecification>();

        internal Builder(string name) {
            this.name = Util.CheckNotBlank(name, "name is blank");
        }

        public NamespaceSpec Build() {
            return new NamespaceSpec(name, nestedSpecs);
        }

        public Builder AddSpecs(IEnumerable<ISpecification> specs) {
            if (specs == null) throw new ArgumentNullException(nameof(specs));
            foreach (ISpecification spec in specs) {
                if (spec == null) throw new ArgumentException("spec == null");
                this.nestedSpecs.Add(spec);
            }
            return this;
        }

        public Builder AddSpec(ISpecification spec) {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            this.nestedSpecs.Add(spec);
            return this;
        }
    }
}