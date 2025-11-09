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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 反序列化特征值(TODO)
/// (反序列化特征值主要用于处理数据异常的情况)
/// 
/// 注：这里的特征值并未完全生效，只为了保持接口稳定预先添加了该枚举。
/// </summary>
public enum DeserializeFeatures
{
    /// <summary>
    /// 读取为不可变对象
    ///
    /// PS：其实直接声明为不可变集合就好，非要用接口类型咱也拦不住。
    /// </summary>
    ReadAsImmutable = 0x01,

    /// <summary>
    /// 不跳过Null字段赋值
    /// </summary>
    ReadNullValue = 0x01 << 8,
    /// <summary>
    /// 跳过Null字段赋值
    /// </summary>
    SkipNullValue = 0x02 << 8,

    /// <summary>
    /// 保持Null值为Null
    /// </summary>
    NullValueAsNull = 0x10 << 8,
    /// <summary>
    /// Null字符串转换为空值（字段声明类型必须是string类型）
    /// </summary>
    NullAsEmptyString = 0x20 << 8,
    /// <summary>
    /// 空字符串转换为Null值
    /// </summary>
    EmptyStringAsNull = 0x40 << 8,
}
}