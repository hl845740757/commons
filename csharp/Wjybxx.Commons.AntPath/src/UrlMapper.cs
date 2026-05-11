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
/// URL 路由器：注册多组 (模式, 值) 后，按优先级（精确 &gt; 变量 &gt; 通配 &gt; 双星号）查找匹配。
///
/// 该类不是线程安全的：注册（<see cref="Add"/> / <see cref="Remove"/>）必须在初始化阶段完成；
/// 完成注册后，<see cref="TryMatch"/> / <see cref="MatchAll"/> 可在多线程间安全调用。
/// </summary>
/// <typeparam name="TValue">注册值类型</typeparam>
public class UrlMapper<TValue>
{
    private readonly AntPathMatcher _matcher;
    private readonly List<Entry> _entries = new List<Entry>();
    /// <summary>
    /// 是否已按特异度排序；任何修改后置为 false，下次查询前重新排序。
    /// </summary>
    private bool _sorted = true;

    /// <summary>使用默认匹配器构造（分隔符 <c>/</c>，区分大小写）。</summary>
    public UrlMapper() : this(new AntPathMatcher()) {
    }

    /// <summary>使用给定匹配器构造。</summary>
    public UrlMapper(AntPathMatcher matcher) {
        _matcher = ObjectUtil.RequireNonNull(matcher, "matcher");
    }

    /// <summary>当前匹配器。</summary>
    public AntPathMatcher Matcher => _matcher;

    /// <summary>已注册的条目数。</summary>
    public int Count => _entries.Count;

    /// <summary>注册一个 (模式, 值) 条目。</summary>
    public void Add(string pattern, TValue value) {
        Add(_matcher.Compile(pattern), value);
    }

    /// <summary>注册一个已编译的 (模式, 值) 条目。</summary>
    public void Add(PathPattern pattern, TValue value) {
        ObjectUtil.RequireNonNull(pattern, "pattern");
        _entries.Add(new Entry(pattern, value));
        _sorted = false;
    }

    /// <summary>移除首个原始模式与给定字符串相等的条目。</summary>
    public bool Remove(string pattern) {
        ObjectUtil.RequireNonNull(pattern, "pattern");
        for (int i = 0; i < _entries.Count; i++) {
            if (_entries[i].Pattern.Raw == pattern) {
                _entries.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>清空所有条目。</summary>
    public void Clear() {
        _entries.Clear();
        _sorted = true;
    }

    /// <summary>查找最具体的匹配条目。</summary>
    public bool TryMatch(string path, out UrlMatch<TValue> match) {
        ObjectUtil.RequireNonNull(path, "path");
        EnsureSorted();
        for (int i = 0; i < _entries.Count; i++) {
            Entry entry = _entries[i];
            if (_matcher.TryMatch(entry.Pattern, path, out PathMatchResult r)) {
                match = new UrlMatch<TValue>(entry.Pattern, entry.Value, r.Variables);
                return true;
            }
        }
        match = default;
        return false;
    }

    /// <summary>查找所有匹配条目，按优先级从高到低返回。</summary>
    public List<UrlMatch<TValue>> MatchAll(string path) {
        ObjectUtil.RequireNonNull(path, "path");
        EnsureSorted();
        List<UrlMatch<TValue>> result = new List<UrlMatch<TValue>>();
        for (int i = 0; i < _entries.Count; i++) {
            Entry entry = _entries[i];
            if (_matcher.TryMatch(entry.Pattern, path, out PathMatchResult r)) {
                result.Add(new UrlMatch<TValue>(entry.Pattern, entry.Value, r.Variables));
            }
        }
        return result;
    }

    private void EnsureSorted() {
        if (_sorted) return;
        _entries.Sort(EntryComparer.Instance);
        _sorted = true;
    }

    private readonly struct Entry
    {
        public readonly PathPattern Pattern;
        public readonly TValue Value;

        public Entry(PathPattern pattern, TValue value) {
            Pattern = pattern;
            Value = value;
        }
    }

    private sealed class EntryComparer : IComparer<Entry>
    {
        public static readonly EntryComparer Instance = new EntryComparer();

        public int Compare(Entry x, Entry y) {
            return x.Pattern.Specificity.CompareTo(y.Pattern.Specificity);
        }
    }
}
}
