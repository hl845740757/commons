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
/// <see cref="UrlMapper{TValue}"/> 的查找结果。
/// </summary>
/// <typeparam name="TValue">注册到路由的值类型</typeparam>
public readonly struct UrlMatch<TValue>
{
    private readonly PathPattern? _pattern;
    private readonly TValue _value;
    private readonly IReadOnlyDictionary<string, string>? _variables;

    internal UrlMatch(PathPattern pattern, TValue value, IReadOnlyDictionary<string, string>? variables) {
        _pattern = pattern;
        _value = value;
        _variables = variables;
    }

    /// <summary>是否命中（命中时 <see cref="Pattern"/> 非空）。</summary>
    public bool IsMatched => _pattern != null;

    /// <summary>命中的模式。</summary>
    public PathPattern? Pattern => _pattern;

    /// <summary>注册时关联的值。</summary>
    public TValue Value => _value;

    /// <summary>提取到的 URI 模板变量；若模式中无变量则为 null。</summary>
    public IReadOnlyDictionary<string, string>? Variables => _variables;
}
}
