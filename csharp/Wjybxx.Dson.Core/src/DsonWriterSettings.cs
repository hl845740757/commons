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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson
{
/// <summary>
/// DsonWriter的设置信息
/// </summary>
public class DsonWriterSettings
{
    public static DsonWriterSettings Default { get; } = NewBuilder().Build();

    public readonly int recursionLimit;
    public readonly bool autoClose;
    public readonly NumberStyle numberStyle;

    public DsonWriterSettings(Builder builder) {
        this.recursionLimit = Math.Max(1, builder.RecursionLimit);
        this.autoClose = builder.AutoClose;
        this.numberStyle = builder.NumberStyle;
    }

    public static Builder NewBuilder() {
        return new Builder();
    }

    public class Builder
    {
        /** 递归深度限制 */
        public int RecursionLimit { get; set; } = 32;
        /** 是否自动关闭底层的输入输出流 */
        public bool AutoClose { get; set; } = true;
        /** 默认的数字编码格式 -- 默认Typed以精确反序列化，打印配置文件时可改用Simple */
        public NumberStyle NumberStyle { get; set; } = NumberStyle.Typed;

        public Builder() {
        }

        public virtual DsonWriterSettings Build() {
            return new DsonWriterSettings(this);
        }
    }
}
}