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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Wjybxx.Commons.AntPath
{
/// <summary>
/// Ant 风格路径匹配器。
///
/// 通配符规则：
/// <list type="bullet">
///   <item><c>?</c> 匹配恰好一个字符（不含分隔符）</item>
///   <item><c>*</c> 匹配零个或多个字符（不含分隔符）</item>
///   <item><c>**</c> 匹配零个或多个路径段</item>
///   <item><c>{name}</c> 捕获变量；<c>{name:regex}</c> 带正则约束</item>
/// </list>
///
/// 匹配器是线程安全的，可全局共享。
/// </summary>
public class AntPathMatcher
{
    /// <summary>缓存模式数量上限，超过则停止缓存以防内存溢出。</summary>
    public const int DefaultCacheLimit = 65536;

    private readonly char _separator;
    private readonly bool _caseSensitive;
    private readonly bool _cachePatterns;
    private readonly int _cacheLimit;
    private readonly ConcurrentDictionary<string, PathPattern>? _patternCache;

    /// <summary>
    /// 构造一个默认的匹配器（分隔符 <c>/</c>，区分大小写，启用缓存）。
    /// </summary>
    public AntPathMatcher() : this('/', true, true, DefaultCacheLimit) {
    }

    /// <summary>
    /// 构造匹配器。
    /// </summary>
    /// <param name="separator">路径分隔符</param>
    /// <param name="caseSensitive">是否区分大小写</param>
    /// <param name="cachePatterns">是否缓存已编译的模式</param>
    /// <param name="cacheLimit">缓存上限（超过后停止缓存新模式）</param>
    public AntPathMatcher(char separator, bool caseSensitive = true,
                          bool cachePatterns = true, int cacheLimit = DefaultCacheLimit) {
        _separator = separator;
        _caseSensitive = caseSensitive;
        _cachePatterns = cachePatterns;
        _cacheLimit = cacheLimit;
        if (cachePatterns) {
            _patternCache = new ConcurrentDictionary<string, PathPattern>();
        }
    }

    /// <summary>路径分隔符。</summary>
    public char Separator => _separator;

    /// <summary>是否区分大小写。</summary>
    public bool CaseSensitive => _caseSensitive;

    // ---------------- Compile ----------------

    /// <summary>
    /// 编译给定模式（带缓存）。
    /// </summary>
    public PathPattern Compile(string pattern) {
        ObjectUtil.RequireNonNull(pattern, "pattern");
        if (_patternCache == null) {
            return PathPattern.Compile(pattern, _separator, _caseSensitive);
        }
        if (_patternCache.TryGetValue(pattern, out PathPattern? cached)) {
            return cached;
        }
        if (_patternCache.Count >= _cacheLimit) {
            return PathPattern.Compile(pattern, _separator, _caseSensitive);
        }
        PathPattern compiled = PathPattern.Compile(pattern, _separator, _caseSensitive);
        _patternCache.TryAdd(pattern, compiled);
        return compiled;
    }

    /// <summary>
    /// 检测字符串是否为模式（含通配符或变量）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsPattern(string str) => PathPattern.IsPattern(str);

    // ---------------- Match ----------------

    /// <summary>判断路径是否匹配模式。</summary>
    public bool Match(string pattern, string path) {
        return Match(Compile(pattern), path);
    }

    /// <summary>判断路径是否匹配已编译的模式。</summary>
    public bool Match(PathPattern pattern, string path) {
        ObjectUtil.RequireNonNull(pattern, "pattern");
        ObjectUtil.RequireNonNull(path, "path");
        return DoMatch(pattern, path, true, null);
    }

    /// <summary>
    /// 尝试匹配并提取 URI 模板变量。
    /// </summary>
    /// <param name="pattern">模式字符串</param>
    /// <param name="path">待匹配路径</param>
    /// <param name="result">匹配结果</param>
    public bool TryMatch(string pattern, string path, out PathMatchResult result) {
        return TryMatch(Compile(pattern), path, out result);
    }

    /// <summary>
    /// 尝试匹配已编译模式并提取 URI 模板变量。
    /// </summary>
    public bool TryMatch(PathPattern pattern, string path, out PathMatchResult result) {
        ObjectUtil.RequireNonNull(pattern, "pattern");
        ObjectUtil.RequireNonNull(path, "path");
        Dictionary<string, string>? vars = null;
        bool ok = DoMatch(pattern, path, true, () => vars ??= new Dictionary<string, string>());
        if (!ok) {
            result = PathMatchResult.NoMatch;
            return false;
        }
        result = new PathMatchResult(true, vars);
        return true;
    }

    /// <summary>
    /// 判断路径的开头部分是否能匹配模式。
    /// 当路径仍可能继续输入字符时，可用此方法做前置过滤。
    /// </summary>
    public bool MatchStart(string pattern, string path) {
        return DoMatch(Compile(pattern), path, false, null);
    }

    // ---------------- Combine ----------------

    /// <summary>
    /// 拼接两个模式（简单版本：以分隔符相连接）。
    /// </summary>
    public string Combine(string pattern1, string pattern2) {
        if (string.IsNullOrEmpty(pattern1)) return pattern2 ?? string.Empty;
        if (string.IsNullOrEmpty(pattern2)) return pattern1;

        bool endsWithSep = pattern1[pattern1.Length - 1] == _separator;
        bool startsWithSep = pattern2[0] == _separator;
        if (endsWithSep && startsWithSep) {
            return pattern1 + pattern2.Substring(1);
        }
        if (endsWithSep || startsWithSep) {
            return pattern1 + pattern2;
        }
        return pattern1 + _separator + pattern2;
    }

    // ---------------- 核心匹配算法 ----------------

    private bool DoMatch(PathPattern pattern, string path, bool fullMatch, Func<Dictionary<string, string>>? varsAccessor) {
        // 绝对/相对路径必须一致
        bool patternStartsWithSep = pattern.Raw.Length > 0 && pattern.Raw[0] == _separator;
        bool pathStartsWithSep = path.Length > 0 && path[0] == _separator;
        if (patternStartsWithSep != pathStartsWithSep) {
            return false;
        }

        PathSegment[] pattDirs = pattern.Segments;
        string[] pathDirs = SplitPath(path);

        int pattIdxStart = 0;
        int pattIdxEnd = pattDirs.Length - 1;
        int pathIdxStart = 0;
        int pathIdxEnd = pathDirs.Length - 1;

        // 第 1 阶段：从头匹配，遇到 ** 停止
        while (pattIdxStart <= pattIdxEnd && pathIdxStart <= pathIdxEnd) {
            ref readonly PathSegment seg = ref pattDirs[pattIdxStart];
            if (seg.Kind == PathSegmentKind.DoubleStar) break;
            if (!MatchSegment(seg, pathDirs[pathIdxStart], varsAccessor)) {
                return false;
            }
            pattIdxStart++;
            pathIdxStart++;
        }

        if (pathIdxStart > pathIdxEnd) {
            // 路径耗尽
            if (pattIdxStart > pattIdxEnd) {
                return true;
            }
            if (!fullMatch) return true;
            // 剩余模式段必须全为 **
            for (int i = pattIdxStart; i <= pattIdxEnd; i++) {
                if (pattDirs[i].Kind != PathSegmentKind.DoubleStar) return false;
            }
            return true;
        } else if (pattIdxStart > pattIdxEnd) {
            // 模式耗尽但路径未尽
            return false;
        } else if (!fullMatch && pattDirs[pattIdxStart].Kind == PathSegmentKind.DoubleStar) {
            // 前缀匹配模式下，遇到 ** 即可视为成功
            return true;
        }

        // 第 2 阶段：从尾匹配，遇到 ** 停止
        while (pattIdxStart <= pattIdxEnd && pathIdxStart <= pathIdxEnd) {
            ref readonly PathSegment seg = ref pattDirs[pattIdxEnd];
            if (seg.Kind == PathSegmentKind.DoubleStar) break;
            if (!MatchSegment(seg, pathDirs[pathIdxEnd], varsAccessor)) {
                return false;
            }
            pattIdxEnd--;
            pathIdxEnd--;
        }

        if (pathIdxStart > pathIdxEnd) {
            // 剩余必须全为 **
            for (int i = pattIdxStart; i <= pattIdxEnd; i++) {
                if (pattDirs[i].Kind != PathSegmentKind.DoubleStar) return false;
            }
            return true;
        }

        // 第 3 阶段：中间段，模式形如 [**, ...sub..., **, ...sub..., **]
        while (pattIdxStart != pattIdxEnd && pathIdxStart <= pathIdxEnd) {
            int patIdxTmp = -1;
            for (int i = pattIdxStart + 1; i <= pattIdxEnd; i++) {
                if (pattDirs[i].Kind == PathSegmentKind.DoubleStar) {
                    patIdxTmp = i;
                    break;
                }
            }
            if (patIdxTmp == pattIdxStart + 1) {
                // 相邻 **，跳过
                pattIdxStart++;
                continue;
            }
            // 模式 [pattIdxStart+1, patIdxTmp-1] 需在路径 [pathIdxStart, pathIdxEnd] 中找到子序列匹配
            int patLength = patIdxTmp - pattIdxStart - 1;
            int strLength = pathIdxEnd - pathIdxStart + 1;
            int foundIdx = -1;

            for (int i = 0; i <= strLength - patLength; i++) {
                bool ok = true;
                for (int j = 0; j < patLength; j++) {
                    if (!MatchSegment(pattDirs[pattIdxStart + 1 + j], pathDirs[pathIdxStart + i + j], null)) {
                        ok = false;
                        break;
                    }
                }
                if (ok) {
                    foundIdx = pathIdxStart + i;
                    break;
                }
            }

            if (foundIdx == -1) return false;

            // 找到位置后，再执行一次以提取变量
            if (varsAccessor != null) {
                for (int j = 0; j < patLength; j++) {
                    MatchSegment(pattDirs[pattIdxStart + 1 + j], pathDirs[foundIdx + j], varsAccessor);
                }
            }

            pattIdxStart = patIdxTmp;
            pathIdxStart = foundIdx + patLength;
        }

        // 最终：剩余模式段必须全为 **
        for (int i = pattIdxStart; i <= pattIdxEnd; i++) {
            if (pattDirs[i].Kind != PathSegmentKind.DoubleStar) return false;
        }
        return true;
    }

    private bool MatchSegment(in PathSegment seg, string token, Func<Dictionary<string, string>>? varsAccessor) {
        switch (seg.Kind) {
            case PathSegmentKind.Literal: {
                StringComparison cmp = _caseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                return string.Equals(seg.Text, token, cmp);
            }
            case PathSegmentKind.Wildcard: {
                return seg.Regex!.IsMatch(token);
            }
            case PathSegmentKind.Variable: {
                Match m = seg.Regex!.Match(token);
                if (!m.Success) return false;
                if (varsAccessor != null) {
                    string[] names = seg.VariableNames!;
                    Dictionary<string, string> vars = varsAccessor();
                    for (int i = 0; i < names.Length; i++) {
                        vars[names[i]] = m.Groups[i + 1].Value;
                    }
                }
                return true;
            }
            case PathSegmentKind.DoubleStar:
                // 不应直接匹配单段
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 按分隔符切分路径；保留前导/尾部空段以正确处理绝对路径与尾部斜线。
    /// </summary>
    private string[] SplitPath(string path) {
        if (path.Length == 0) return Array.Empty<string>();
        int count = 1;
        for (int i = 0; i < path.Length; i++) {
            if (path[i] == _separator) count++;
        }
        string[] result = new string[count];
        int idx = 0;
        int start = 0;
        for (int i = 0; i <= path.Length; i++) {
            if (i == path.Length || path[i] == _separator) {
                result[idx++] = i > start ? path.Substring(start, i - start) : string.Empty;
                start = i + 1;
            }
        }
        return result;
    }
}
}
