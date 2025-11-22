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
    /// 集合名
    ///
    /// 通用序列化库仅支持根据[collection + localId]查找对象。
    /// </summary>
    public string? collection;
    /// <summary>
    /// 集合内id
    /// </summary>
    public long localId;

    /// <summary>
    /// 类型名
    ///
    /// 注：<see cref="TypeName"/>的String格式。
    /// </summary>
    public string? clsName;
    /// <summary>
    /// 集合大小
    ///
    /// 注意：count不一定是准确值，不可以根据count判断输入流是否结束！
    /// 在使用Dson文本配置数据的情况下，Count可能未被正确维护；
    /// Count的唯一作用就是更好的初始化<see cref="List{T}"/>和<see cref="Dictionary{TKey,TValue}"/>的空间。
    /// </summary>
    public int count;
    /// <summary>
    /// 版本号(自定义序列化用)
    /// </summary>
    public int version;

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool isEmpty => string.IsNullOrEmpty(collection)
                           && localId == 0
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