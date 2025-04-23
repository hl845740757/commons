#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;

namespace Wjybxx.Dson
{
/// <summary>
/// DsonReader的设置数据
/// </summary>
public class DsonReaderSettings
{
    public static DsonReaderSettings Default { get; } = NewBuilder().Build();

    public readonly int recursionLimit;
    public readonly bool autoClose;
    public readonly bool? enableNameIntern;

    public DsonReaderSettings(Builder builder) {
        recursionLimit = Math.Max(1, builder.RecursionLimit);
        autoClose = builder.AutoClose;
        enableNameIntern = builder.EnableNameIntern;
    }

    public static Builder NewBuilder() {
        return new Builder();
    }

    public class Builder
    {
        /// <summary>
        /// 递归深度限制
        /// </summary>
        public int RecursionLimit { get; set; } = 32;
        /// <summary>
        /// 是否自动关闭底层的输入输出流
        /// </summary>
        public bool AutoClose { get; set; } = true;
        /// <summary>
        /// 是否池化字段名
        /// 1.字段名几乎都是常量，因此命中率几乎百分之百 -- 字典由Codec处理。
        /// 2.池化字段名可以降低字符串内存占用，有一定的查找开销。
        /// 3.如果未设置，则完全由代码控制；如果为false，则全局禁用；如果为true，则全局启用，用户可临时关闭；
        /// </summary>
        public bool? EnableNameIntern { get; set; } = false;

        public Builder() {
        }

        public virtual DsonReaderSettings Build() {
            return new DsonReaderSettings(this);
        }
    }
}
}