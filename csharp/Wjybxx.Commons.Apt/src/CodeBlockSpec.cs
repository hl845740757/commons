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
#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 用于将<see cref="CodeBlock"/>嵌入到任意位置。
/// </summary>
public class CodeBlockSpec : ISpecification
{
    public readonly string name;
    public readonly CodeBlock code;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">代码块的名字</param>
    /// <param name="code">代码</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CodeBlockSpec(string name, CodeBlock code) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Name => name;
    public SpecType SpecType => SpecType.CodeBlock;
}