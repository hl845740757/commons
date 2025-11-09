#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Dson.Codec.Attributes
{
/// <summary>
/// 字段实现信息
///
/// 1.由于属性较多，因此属性都是get/set，但只应该初始化一次
/// 2.由于要支持属性，因此不能关闭继承属性
/// 3.如果是非自动属性，注解必须添加到字段上
///
/// TODO 合并Style枚举，增加ElementStyle
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[Serializable]
public class DsonPropertyAttribute : Attribute
{
    /// <summary>
    /// 用于文档型序列化时字段名
    /// 可用于枚举。
    /// </summary>
    [StableName] public string? Name { get; set; }

    /// <summary>
    /// 获取字段的属性或方法 -- 特殊情况下使用
    /// </summary>
    [StableName] public string? Getter { get; set; }

    /// <summary>
    /// 赋值字段的属性或方法 -- 特殊情况下使用
    /// </summary>
    [StableName] public string? Setter { get; set; }
    /// <summary>
    /// 序列化特征值
    /// </summary>
    [StableName] public SerializeFeatures EncodeFeatures { get; set; }
    /// <summary>
    /// 反序列化特征值
    /// </summary>
    [StableName] public SerializeFeatures DecodeFeatures { get; set; }

    #region 多态解析

    /// <summary>
    /// 字段的实现类。
    /// 1. 必须是具体类型，必须有public无参构造函数。
    /// 2. 自定义类型也可以指定实现类。
    /// 3. 实现类的泛型参数个数必须和声明类型一致，typeof时不要指定泛型参数。
    /// 4. 使用<see cref="ReadProxy"/>时忽略该属性。
    /// 5. 不要轻易使用该属性，这会导致总是按照固定类型解析，从而导致多态失效。
    /// </summary>
    [StableName] public Type? Impl { get; set; }

    /// <summary>
    /// 写代理：自定义写方法。
    /// 1. 如果由<see cref="DsonCodecLinkerBeanAttribute"/>配置，则表示静态方法代理，否则为普通实例方法代理。
    /// 2. writer的类型限定为<see cref="IDsonObjectWriter"/>
    /// 3. 对于需要特殊编解码的字段是很有用的。
    /// <code>
    ///  // 实例方法代理
    ///  public void WriteName(IDsonObjectWriter writer, String name) {
    ///      writer.WriteString(name, this.name);
    ///  }
    ///  // 静态方法代理
    ///  public static void WriteName(T inst, IDsonObjectWriter writer, String name) {
    ///      writer.WriteString(name, inst.name);
    ///  }
    /// </code>
    /// </summary>
    [StableName] public string? WriteProxy { get; set; }

    /// <summary>
    /// 读代理：自定义读方法。
    /// 1. 如果由<see cref="DsonCodecLinkerBeanAttribute"/>配置，则表示静态方法代理，否则为普通实例方法代理。
    /// 2. reader的类型限定为<see cref="IDsonObjectReader"/>
    /// 3. 对于有特殊构造过程的字段是很有帮助的，也可以进行类型转换。
    /// <code>
    ///  // 实例方法代理
    ///  public void ReadName(IDsonObjectReader reader, String name) {
    ///      this.name = reader.ReadString(name);
    ///  }
    ///  // 静态方法代理
    ///  public static void ReadName(T inst, IDsonObjectReader reader, String name) {
    ///      inst.name = reader.ReadString(name);
    ///  }
    /// </code>
    /// </summary>
    [StableName] public string? ReadProxy { get; set; }

    #endregion
}
}