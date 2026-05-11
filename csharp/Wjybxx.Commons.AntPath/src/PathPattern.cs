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

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Wjybxx.Commons.AntPath
{
/// <summary>
/// 已编译的 Ant 风格路径模式。
/// 该类型是不可变的，可在多线程间共享、可缓存。
/// </summary>
public sealed class PathPattern
{
    private readonly string _raw;
    private readonly char _separator;
    private readonly bool _caseSensitive;
    private readonly PathSegment[] _segments;
    private readonly PathSpecificity _specificity;
    private readonly bool _isLiteral;

    internal PathPattern(string raw, char separator, bool caseSensitive,
                         PathSegment[] segments, PathSpecificity specificity, bool isLiteral) {
        _raw = raw;
        _separator = separator;
        _caseSensitive = caseSensitive;
        _segments = segments;
        _specificity = specificity;
        _isLiteral = isLiteral;
    }

    /// <summary>编译前的原始模式字符串。</summary>
    public string Raw => _raw;

    /// <summary>路径分隔符（默认 <c>/</c>）。</summary>
    public char Separator => _separator;

    /// <summary>是否区分大小写。</summary>
    public bool CaseSensitive => _caseSensitive;

    /// <summary>已编译的段数组（仅供匹配算法使用）。</summary>
    internal PathSegment[] Segments => _segments;

    /// <summary>段数量。</summary>
    public int SegmentCount => _segments.Length;

    /// <summary>模式特异度，用于优先级比较。</summary>
    public PathSpecificity Specificity => _specificity;

    /// <summary>是否为纯字面量模式（无任何通配符或变量）。</summary>
    public bool IsLiteral => _isLiteral;

    /// <summary>
    /// 编译给定的模式字符串。
    /// </summary>
    /// <param name="pattern">原始模式</param>
    /// <param name="separator">路径分隔符</param>
    /// <param name="caseSensitive">是否区分大小写</param>
    /// <exception cref="ArgumentNullException">pattern 为 null</exception>
    /// <exception cref="AntPathSyntaxException">模式语法错误</exception>
    public static PathPattern Compile(string pattern, char separator = '/', bool caseSensitive = true) {
        ObjectUtil.RequireNonNull(pattern, "pattern");

        // 尾部分隔符快捷规则：以分隔符结尾时自动追加 **
        string normalized = pattern;
        if (normalized.Length > 0 && normalized[normalized.Length - 1] == separator) {
            normalized = normalized + "**";
        }

        string[] tokens = SplitPath(normalized, separator);

        int doubleStarCount = 0;
        int wildcardCount = 0;
        int variableCount = 0;
        bool isLiteral = true;

        PathSegment[] segments = new PathSegment[tokens.Length];
        for (int i = 0; i < tokens.Length; i++) {
            string token = tokens[i];
            PathSegment seg = CompileSegment(token, separator, caseSensitive);
            segments[i] = seg;
            switch (seg.Kind) {
                case PathSegmentKind.DoubleStar:
                    doubleStarCount++;
                    isLiteral = false;
                    break;
                case PathSegmentKind.Wildcard:
                    wildcardCount++;
                    isLiteral = false;
                    break;
                case PathSegmentKind.Variable:
                    variableCount += seg.VariableCount;
                    isLiteral = false;
                    break;
            }
        }

        PathSpecificity specificity = new PathSpecificity(doubleStarCount, wildcardCount, variableCount, pattern.Length);
        return new PathPattern(pattern, separator, caseSensitive, segments, specificity, isLiteral);
    }

    /// <summary>
    /// 检测字符串是否为模式（含通配符或变量）。
    /// </summary>
    public static bool IsPattern(string str) {
        if (string.IsNullOrEmpty(str)) return false;
        for (int i = 0; i < str.Length; i++) {
            char c = str[i];
            if (c == '?' || c == '*' || c == '{') return true;
        }
        return false;
    }

    public override string ToString() => _raw;

    // ---------------- 内部编译实现 ----------------

    /// <summary>
    /// 按分隔符切分模式字符串。
    /// 不丢弃空段（与 Spring 行为一致：前导/尾部分隔符会产生空段，用于绝对路径匹配）。
    /// </summary>
    private static string[] SplitPath(string str, char separator) {
        // 空字符串 -> 空数组
        if (str.Length == 0) return Array.Empty<string>();

        int count = 1;
        for (int i = 0; i < str.Length; i++) {
            if (str[i] == separator) count++;
        }
        string[] result = new string[count];
        int idx = 0;
        int start = 0;
        for (int i = 0; i <= str.Length; i++) {
            if (i == str.Length || str[i] == separator) {
                result[idx++] = i > start ? str.Substring(start, i - start) : string.Empty;
                start = i + 1;
            }
        }
        return result;
    }

    private static PathSegment CompileSegment(string token, char separator, bool caseSensitive) {
        if (token == "**") {
            return new PathSegment(PathSegmentKind.DoubleStar, token, null, null);
        }

        bool hasVariable = token.IndexOf('{') >= 0;
        bool hasWildcard = false;
        for (int i = 0; i < token.Length; i++) {
            char c = token[i];
            if (c == '?' || c == '*') {
                hasWildcard = true;
                break;
            }
        }

        if (!hasVariable && !hasWildcard) {
            return new PathSegment(PathSegmentKind.Literal, token, null, null);
        }

        if (hasVariable) {
            return CompileVariableSegment(token, separator, caseSensitive);
        }
        // 仅含通配符
        return CompileWildcardSegment(token, separator, caseSensitive);
    }

    private static PathSegment CompileWildcardSegment(string token, char separator, bool caseSensitive) {
        StringBuilder regex = new StringBuilder(token.Length + 16);
        regex.Append('^');
        for (int i = 0; i < token.Length; i++) {
            char c = token[i];
            if (c == '?') {
                // 任意单字符（不含分隔符）
                regex.Append('[').Append('^').Append(Regex.Escape(separator.ToString())).Append(']');
            } else if (c == '*') {
                regex.Append('[').Append('^').Append(Regex.Escape(separator.ToString())).Append("]*");
            } else {
                regex.Append(Regex.Escape(c.ToString()));
            }
        }
        regex.Append('$');

        RegexOptions options = RegexOptions.CultureInvariant;
        if (!caseSensitive) options |= RegexOptions.IgnoreCase;
        Regex compiled = new Regex(regex.ToString(), options);
        return new PathSegment(PathSegmentKind.Wildcard, token, compiled, null);
    }

    private static PathSegment CompileVariableSegment(string token, char separator, bool caseSensitive) {
        StringBuilder regex = new StringBuilder(token.Length + 32);
        regex.Append('^');

        List<string> variableNames = new List<string>(2);
        int i = 0;
        while (i < token.Length) {
            char c = token[i];
            if (c == '{') {
                int end = FindClosingBrace(token, i);
                if (end < 0) {
                    throw new AntPathSyntaxException($"Unclosed '{{' in segment: {token}");
                }
                string body = token.Substring(i + 1, end - i - 1);
                int colon = body.IndexOf(':');
                string varName;
                string varRegex;
                if (colon >= 0) {
                    varName = body.Substring(0, colon);
                    varRegex = body.Substring(colon + 1);
                } else {
                    varName = body;
                    // 默认正则：匹配除分隔符外的一个或多个字符
                    varRegex = "[^" + Regex.Escape(separator.ToString()) + "]+";
                }
                if (varName.Length == 0) {
                    throw new AntPathSyntaxException($"Empty variable name in segment: {token}");
                }
                variableNames.Add(varName);
                regex.Append('(').Append(varRegex).Append(')');
                i = end + 1;
            } else if (c == '?') {
                regex.Append('[').Append('^').Append(Regex.Escape(separator.ToString())).Append(']');
                i++;
            } else if (c == '*') {
                regex.Append('[').Append('^').Append(Regex.Escape(separator.ToString())).Append("]*");
                i++;
            } else {
                regex.Append(Regex.Escape(c.ToString()));
                i++;
            }
        }
        regex.Append('$');

        RegexOptions options = RegexOptions.CultureInvariant;
        if (!caseSensitive) options |= RegexOptions.IgnoreCase;
        Regex compiled = new Regex(regex.ToString(), options);
        return new PathSegment(PathSegmentKind.Variable, token, compiled, variableNames.ToArray());
    }

    private static int FindClosingBrace(string token, int start) {
        // 简单的括号配对（不处理转义大括号；正则约束内若需 {n,m} 量词，需用户自行避免冲突）
        int depth = 0;
        for (int i = start; i < token.Length; i++) {
            char c = token[i];
            if (c == '{') depth++;
            else if (c == '}') {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }
}
}
