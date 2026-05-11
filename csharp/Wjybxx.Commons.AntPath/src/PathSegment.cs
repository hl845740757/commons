#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using System.Text.RegularExpressions;

namespace Wjybxx.Commons.AntPath
{
/// <summary>
/// 已编译的单个路径段。
/// 该类型为只读结构体，仅在 <see cref="PathPattern"/> 内部数组中持有，避免每段一次堆分配。
/// </summary>
public readonly struct PathSegment
{
    private readonly PathSegmentKind _kind;
    private readonly string _text;
    private readonly Regex? _regex;
    private readonly string[]? _variableNames;

    internal PathSegment(PathSegmentKind kind, string text, Regex? regex, string[]? variableNames) {
        _kind = kind;
        _text = text;
        _regex = regex;
        _variableNames = variableNames;
    }

    /// <summary>
    /// 段类型
    /// </summary>
    public PathSegmentKind Kind => _kind;

    /// <summary>
    /// 段的原始文本（编译前的字符串）。
    /// </summary>
    public string Text => _text;

    /// <summary>
    /// 预编译的正则；仅当 <see cref="Kind"/> 为 <see cref="PathSegmentKind.Wildcard"/> 或
    /// <see cref="PathSegmentKind.Variable"/> 时非空。
    /// </summary>
    public Regex? Regex => _regex;

    /// <summary>
    /// 变量名数组；仅当 <see cref="Kind"/> 为 <see cref="PathSegmentKind.Variable"/> 时非空。
    /// 数组顺序与正则中的捕获组顺序一致。
    /// </summary>
    public string[]? VariableNames => _variableNames;

    /// <summary>
    /// 变量个数
    /// </summary>
    public int VariableCount => _variableNames?.Length ?? 0;

    public override string ToString() {
        return $"{_kind}({_text})";
    }
}
}
