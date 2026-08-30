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
using System.Runtime.InteropServices;
using System.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 序列化对象头
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct SerializeHeader
{
    /// <summary>
    /// 集合内id
    /// </summary>
    [FieldOffset(0)] public int localId;
    /// <summary>
    /// 集合名
    ///
    /// 通用序列化库仅支持根据[collection + localId]查找对象。
    /// </summary>
    [FieldOffset(8)] public string? collection;

    /// <summary>
    /// 类型名
    ///
    /// 注：<see cref="TypeName"/>的String格式。
    /// </summary>
    [FieldOffset(16)] public string? clsName;
    /// <summary>
    /// 集合大小
    ///
    /// 注意：count不一定是准确值，不可以根据count判断输入流是否结束！
    /// Count的唯一作用就是更好的初始化<see cref="List{T}"/>和<see cref="Dictionary{TKey,TValue}"/>的空间。
    /// </summary>
    [FieldOffset(24)] public int count;
    /// <summary>
    /// 版本号(自定义序列化用)
    /// </summary>
    [FieldOffset(28)] public int version;

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => localId == 0
                           && string.IsNullOrEmpty(collection)
                           && string.IsNullOrEmpty(clsName)
                           && count == 0
                           && version == 0;

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(collection)) {
            sb.Append(nameof(collection)).Append(": ").Append(collection);
        }
        if (localId != 0) {
            sb.Append(nameof(localId)).Append(": ").Append(localId);
        }
        if (!string.IsNullOrEmpty(clsName)) {
            sb.Append(nameof(clsName)).Append(": ").Append(clsName);
        }
        if (count > 0) {
            sb.Append(nameof(count)).Append(": ").Append(count);
        }
        if (version != 0) {
            sb.Append(nameof(version)).Append(": ").Append(version);
        }
        return sb.ToString();
    }
}
}