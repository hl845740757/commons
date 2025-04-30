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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 用于将<see cref="CodeBlock"/>嵌入到任意位置。
/// </summary>
public class CodeBlockSpec : ISpecification
{
    public readonly CodeBlock code;
    public readonly Kind kind;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code">代码</param>
    /// <param name="kind">代码的类型</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CodeBlockSpec(CodeBlock code, Kind kind = Kind.Code) {
        this.code = code ?? throw new ArgumentNullException(nameof(code));
        this.kind = kind;
    }

    public string? Name => null;
    public SpecType SpecType => SpecType.CodeBlock;

    public enum Kind : byte
    {
        /// <summary>
        /// 普通代码
        /// </summary>
        Code = 0,
        /// <summary>
        /// 注释 -- 双斜杠
        /// </summary>
        Comment,
        /// <summary>
        /// 文档 -- 三斜杠
        /// </summary>
        Document,
    }
}
}