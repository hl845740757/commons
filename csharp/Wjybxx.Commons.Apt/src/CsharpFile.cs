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
#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 表示一个C#文件
/// （暂时不实现<see cref="ISpecification"/>接口）
/// </summary>
public class CsharpFile
{
    public readonly string name;
    public readonly CodeBlock document; // 文件注释
    public readonly IList<ISpecification> nestedSpecs;

    private CsharpFile(Builder builder) {
        this.name = builder.name;
        this.document = builder.document.Build();
        this.nestedSpecs = Util.ToImmutableList(builder.nestedSpecs);
    }

    public static Builder NewBuilder(string name) => new Builder(name);

    public class Builder
    {
        public readonly string name;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly List<ISpecification> nestedSpecs = new List<ISpecification>();

        internal Builder(string name) {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public CsharpFile Build() {
            return new CsharpFile(this);
        }

        public Builder AddDocument(string format, params object[] args) {
            document.Add(format, args);
            return this;
        }

        public Builder AddDocument(CodeBlock codeBlock) {
            document.Add(codeBlock);
            return this;
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