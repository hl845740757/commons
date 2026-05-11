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

using System.Collections.Generic;

namespace Wjybxx.Commons.AntPath
{
/// <summary>
/// 单次路径匹配的结果。
/// 当无 URI 模板变量时，<see cref="Variables"/> 为 null，避免不必要的字典分配。
/// </summary>
public readonly struct PathMatchResult
{
    /// <summary>未匹配的占位结果。</summary>
    public static readonly PathMatchResult NoMatch = default;

    private readonly bool _matched;
    private readonly IReadOnlyDictionary<string, string>? _variables;

    internal PathMatchResult(bool matched, IReadOnlyDictionary<string, string>? variables) {
        _matched = matched;
        _variables = variables;
    }

    /// <summary>是否匹配成功。</summary>
    public bool Matched => _matched;

    /// <summary>
    /// 提取到的 URI 模板变量；若模式中无变量或匹配失败，则为 null。
    /// </summary>
    public IReadOnlyDictionary<string, string>? Variables => _variables;

    public override string ToString() {
        if (!_matched) return "NoMatch";
        return _variables == null ? "Match" : $"Match(vars={_variables.Count})";
    }
}
}
