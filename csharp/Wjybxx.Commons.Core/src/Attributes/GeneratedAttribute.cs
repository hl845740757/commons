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

namespace Wjybxx.Commons.Attributes
{
/// <summary>
/// 用于标注类文件是生成的
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class GeneratedAttribute : Attribute
{
    /// <summary>
    /// 生成类文件的处理器的限定名
    /// </summary>
    public readonly string processor;
    /// <summary>
    /// 生成器的版本
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 文件应当归属的程序集
    /// （dotnet是多进程构建的，有时候文件会被生成到其它程序集目录下....）
    /// </summary>
    public string? Assembly { get; set; }
    /// <summary>
    /// 生成文件的时间，该文件主要用于Debug
    /// (有时候发现生成的文件没有变化，但又不确定是否是最新的文件)
    /// </summary>
    public string? DateTime { get; set; }

    public GeneratedAttribute(string processor) {
        this.processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }
}
}