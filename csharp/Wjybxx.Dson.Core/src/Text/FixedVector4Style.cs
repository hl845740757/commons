#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// <see cref="FixedVector4"/>的文本输出格式。
/// 解码时固定顺序读取，忽略字段名；限定长度可能导致数据丢失。
/// </summary>
[Flags]
public enum FixedVector4Style
{
    /// <summary>
    /// 打印为数组格式：<c>[@fv4 v0, v1, v2, v3]</c>
    /// </summary>
    Array = 0x00,
    /// <summary>
    /// 打印为向量格式：<c>{@fv4 x: 1, y: 1, z: 1, w: 1}</c>
    /// </summary>
    Vector = 0x01,

    /// <summary>
    /// 只打印前两个数；未指定长度时打印四个数。
    /// </summary>
    Len2 = 0x04,
    /// <summary>
    /// 只打印前三个数；与Len2同时指定时优先。
    /// </summary>
    Len3 = 0x08,
}
}