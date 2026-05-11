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

namespace Wjybxx.Commons.AntPath
{
/// <summary>
/// 模式特异度统计，用于优先级比较。
/// 各分量数值越小，模式越具体、优先级越高。
/// </summary>
public readonly struct PathSpecificity
{
    private readonly int _doubleStarCount;
    private readonly int _wildcardCount;
    private readonly int _variableCount;
    private readonly int _length;

    internal PathSpecificity(int doubleStarCount, int wildcardCount, int variableCount, int length) {
        _doubleStarCount = doubleStarCount;
        _wildcardCount = wildcardCount;
        _variableCount = variableCount;
        _length = length;
    }

    /// <summary>双星号 <c>**</c> 段的数量。</summary>
    public int DoubleStarCount => _doubleStarCount;

    /// <summary>含 <c>?</c> 或 <c>*</c>（不含变量）的段的数量。</summary>
    public int WildcardCount => _wildcardCount;

    /// <summary>含 URI 模板变量的段的数量。</summary>
    public int VariableCount => _variableCount;

    /// <summary>模式原始字符串长度。</summary>
    public int Length => _length;

    /// <summary>
    /// 与另一个特异度比较，返回 &lt; 0 表示当前更具体（优先级更高）。
    /// 比较顺序：DoubleStarCount → WildcardCount → VariableCount → -Length。
    /// </summary>
    public int CompareTo(PathSpecificity other) {
        int r = _doubleStarCount - other._doubleStarCount;
        if (r != 0) return r;
        r = _wildcardCount - other._wildcardCount;
        if (r != 0) return r;
        r = _variableCount - other._variableCount;
        if (r != 0) return r;
        // 长者更具体，反向
        return other._length - _length;
    }
}
}
