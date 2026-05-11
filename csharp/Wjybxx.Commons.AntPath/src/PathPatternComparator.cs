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
/// 基于 <see cref="PathSpecificity"/> 的模式优先级比较器。
/// 排序后越靠前的模式优先级越高（精确 &gt; 变量 &gt; 通配 &gt; 双星号）。
/// </summary>
public sealed class PathPatternComparator : IComparer<PathPattern>
{
    /// <summary>共享单例。</summary>
    public static readonly PathPatternComparator Instance = new PathPatternComparator();

    public int Compare(PathPattern? x, PathPattern? y) {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return 1;
        if (y == null) return -1;
        return x.Specificity.CompareTo(y.Specificity);
    }
}
}
