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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Attributes
{
/// <summary>
/// 字段实现信息
///
/// 由于属性较多，因此属性都是get/set，但只应该初始化一次
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[Serializable]
public class DsonPropertyAttribute : Attribute
{
    /// <summary>
    /// 用于文档型序列化时字段名
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// 获取字段的属性或方法 -- 特殊情况下使用
    /// </summary>
    public string? Getter { get; set; }
    /// <summary>
    /// 赋值字段的属性或方法 -- 特殊情况下使用
    /// </summary>
    public string? Setter { get; set; }

    #region tag

    /// <summary>
    /// 数字类型属性的编码格式。
    /// 设定合适的类型有助于优化二进制编码，修改该值不产生兼容性问题。
    /// </summary>
    public WireType WireType { get; set; } = WireType.VarInt;

    /// <summary>
    /// 数据关联的{@link DsonType}，配合<see cref="DsonSubType"/>使用
    /// <see cref="DsonBinary"/>
    /// </summary>
    public DsonType DsonType { get; set; } = DsonType.EndOfObject;

    /// <summary>
    /// 用于声明子类型，项目可以定义一个自己的常量类。
    /// <see cref="Wjybxx.Dson.DsonType.Binary"/>
    /// </summary>
    public int DsonSubType { get; set; } = 0;

    /// <summary>
    /// 数字类型字段的文本格式
    /// </summary>
    public NumberStyle NumberStyle { get; set; } = NumberStyle.Simple;

    /// <summary>
    /// 字符串类型字段的文本格式
    /// </summary>
    public StringStyle StringStyle { get; set; } = StringStyle.Auto;

    /// <summary>
    /// 对象类型字段的文本格式。
    /// 注意：该属性只有显式声明才有效，当未声明该属性时，将使用目标类型的默认格式。
    /// </summary>
    public ObjectStyle? ObjectStyle { get; set; } = null;

    #endregion

    #region 多态解析

    /// <summary>
    /// 字段的实现类。
    /// 1. 必须是具体类型，必须有public无参构造函数。
    /// 2. 自定义类型也可以指定实现类。
    /// 3. 实现类的泛型参数个数必须和声明类型一致，typeof时不要指定泛型参数。
    /// 4. 使用<see cref="ReadProxy"/>时忽略该属性。
    /// </summary>
    public Type? Impl { get; set; }

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
    ///      writer.WriteString(name, this.name);
    ///  }
    /// </code>
    /// </summary>
    public string? WriteProxy { get; set; }

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
    ///      this.name = reader.ReadString(name);
    ///  }
    /// </code>
    /// </summary>
    public string? ReadProxy { get; set; }

    #endregion
}
}