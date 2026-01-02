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

using System;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 反序列化特征值(TODO)
///
/// 1.反序列化特征值主要用于处理数据异常的情况，因此大多仅为字段级别特征值。
/// 2.这里的特征值大多数未生效，只为了保持接口稳定预先添加了该枚举。
/// </summary>
[Flags]
public enum DeserializeFeatures
{
    /// <summary>
    /// 读取为不可变对象，主要用于集合。
    ///
    /// 1.声明类型应当为接口类型，否则不会生效。
    /// 2.字段也可直接声明为不可变集合，但局限于Commons库的不可变集合。
    /// </summary>
    ReadAsImmutable = 0x01,
    /// <summary>
    /// Enum解码时忽略大小写
    /// </summary>
    EnumIgnoreCase = 0x02,
    /// <summary>
    /// 数字类型尝试从字符串中解析（字段级别）
    /// </summary>
    TryParseString = 0x04,
    /// <summary>
    /// 拷贝引用指向的对象(未实现)
    /// </summary>
    CopyReferenceTarget = 0x08,

    /// <summary>
    /// 不跳过Null字段赋值
    /// </summary>
    ReadNullValue = 0x01 << 8,
    /// <summary>
    /// 跳过Null字段赋值
    /// </summary>
    SkipNullValue = 0x02 << 8,
    /// <summary>
    /// 不跳过零值字段赋值
    /// </summary>
    ReadZeroValue = 0x03 << 8,
    /// <summary>
    /// 跳过零值字段赋值
    /// </summary>
    SkipZeroValue = 0x04 << 8,

    EmptyStringAsEmpty = 0x10 << 8,
    /// <summary>
    /// 空字符串转换为Null值（对应序列化特征值）
    /// </summary>
    EmptyStringAsNull = 0x20 << 8,
    /// <summary>
    /// 字符串放入常量池
    /// </summary>
    InternedString = 0x40 << 8,

    /// <summary>
    /// 集合元素的特征值
    /// </summary>
    MaskElementFeatures = EnumIgnoreCase | TryParseString
                                         | EmptyStringAsEmpty | EmptyStringAsNull | InternedString
}
}