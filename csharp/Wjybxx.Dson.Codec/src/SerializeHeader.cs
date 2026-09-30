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
using System.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 序列化对象头
/// </summary>
public struct SerializeHeader
{
    /// <summary>
    /// 类型名
    ///
    /// 注：<see cref="TypeName"/>的String格式。
    /// </summary>
    public string? clsName;
    /// <summary>
    /// 集合内id
    /// </summary>
    public int localId;
    /// <summary>
    /// 集合大小
    ///
    /// 注意：count不一定是准确值，不可以根据count判断输入流是否结束！
    /// Count的唯一作用就是更好的初始化<see cref="List{T}"/>和<see cref="Dictionary{TKey,TValue}"/>的空间。
    /// </summary>
    public int count;

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(clsName)
                           && localId == 0 && count == 0;

    /// <summary>
    /// 是否只包含类型名
    /// </summary>
    internal bool IsClassNameOnly => !string.IsNullOrEmpty(clsName)
                                     && localId == 0 && count == 0;

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(clsName)) {
            sb.Append(nameof(clsName)).Append(": ").Append(clsName);
        }
        if (localId != 0) {
            sb.Append(nameof(localId)).Append(": ").Append(localId);
        }
        if (count > 0) {
            sb.Append(nameof(count)).Append(": ").Append(count);
        }
        return sb.ToString();
    }
}
}