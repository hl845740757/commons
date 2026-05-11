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
/// 路径段的类型
/// </summary>
public enum PathSegmentKind : byte
{
    /// <summary>
    /// 字面量（无任何通配符或变量），匹配时使用字符串相等比较。
    /// </summary>
    Literal = 0,

    /// <summary>
    /// 含 <c>?</c> 或 <c>*</c> 通配符（不含变量），匹配时使用预编译的 Regex。
    /// </summary>
    Wildcard = 1,

    /// <summary>
    /// 含 URI 模板变量（<c>{name}</c> 或 <c>{name:regex}</c>），匹配时使用预编译的 Regex 并提取变量。
    /// </summary>
    Variable = 2,

    /// <summary>
    /// 双星号段 <c>**</c>，可匹配零个或多个路径段。
    /// </summary>
    DoubleStar = 3,
}
}
