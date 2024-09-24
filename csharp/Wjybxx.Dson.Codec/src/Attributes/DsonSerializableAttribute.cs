#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
/// 用于标注一个类的对象可序列化为Dson文档结构
///
/// <h3>注解处理器</h3>
/// 对于带有该注解的类：
/// 1. 对于普通类，必须提供<b>非私有无参构造方法</b>，或提供非私有的{@link DsonObjectReader}的单参构造方法。
/// 2. 对于普通类，所有托管给生成代码读的字段，必须提供setter属性或直接写权限。
/// 3. 对于普通类，所有托管给生成代码写的字段，必须提供getter属性或直接读权限。
/// 4. 如果字段通过<see cref="DsonPropertyAttribute"/>指定了读代理，则不要求setter权限
/// 5. 如果字段通过<see cref="DsonPropertyAttribute"/>指定了写代理，则不要求getter权限
/// 6. 自动属性规则同普通字段，非自动属性不会被序列化。
/// 
/// 普通类钩子方法：
/// 1. 如果类提供了非私有的<code>ClassName(IDsonObjectReader)</code>的单参构造方法，将自动调用 -- 该方法可用于final和忽略字段。
/// 2. 如果类提供了静态的<code>newInstance(IDsonObjectReader)</code>方法，将自动调用 -- 优先级高于构造方法。
/// 3. 如果类提供了非私有的<code>AfterDecode(ConverterOptions)</code>方法，且在options中启用，则自动调用 -- 通常用于数据转换，或构建缓存字段。
/// 4. 如果类提供了非私有的<code>BeforeEncode(ConverterOptions)</code>方法，且在options中启用，则自动调用 -- 通常用于数据转换。
/// 5. 如果类提供了非私有的<code>ReadObject(IDsonObjectReader)</code>方法，将自动调用 -- 该方法可用于忽略字段。
/// 6. 如果类提供了非私有的<code>WriteObject(IDsonObjectWriter)</code>方法，将自动调用 -- 该方法可用于final和忽略字段。
/// 7. 如果是通过<see cref="DsonCodecLinkerBeanAttribute"/>配置的类，这些方法都需要转换为静态方法。
///
/// <pre><code>
///   public void BeforeEncode(ConverterOptions options){}
///   public void WriteObject(DsonObjectWriter writer){}
///
///   public static Bean newInstance(DsonObjectReader reader){}
///   public void ReadObject(DsonObjectReader reader){}
///   public void AfterDecode(ConverterOptions options){}
///
///   public void WriteField1(DsonObjectWriter writer, String dsonName){}
///   public void ReadField1(DsonObjectReader reader, String dsonName){}
/// </code></pre>
///
/// <h3>序列化的字段</h3>
/// 1. 默认序列化public和或包含public属性的字段；默认忽略有<see cref="NonSerializedAttribute"/>或<see cref="DsonIgnoreAttribute"/>注解的字段。
/// 2. <see cref="DsonIgnoreAttribute"/>的优先级更高，可以覆盖<see cref="NonSerializedAttribute"/>。 // 未特殊处理Unity的SerializedField
/// 3. 如果你提供了WriteObjet和ReadObject方法，你可以在其中写入忽略字段。
/// 4. 自动属性规则同普通字段，非自动属性不会被序列化。
///
/// <h3>多态字段</h3>
/// 1. 如果对象的运行时类型存在于<see cref="IDsonCodecRegistry"/>中，则总是可以精确解析，因此不需要特殊处理。
/// 2. 否则用户需要指定实现类或读代理实现精确解析，请查看<see cref="DsonPropertyAttribute"/>。
///
/// <h3>final字段</h3>
/// 详见：
///
/// <h3>读写忽略字段</h3>
/// 用户可以通过构造解码器和写对象方法实现。
///
/// <h3>扩展</h3>
/// Q: 是否可以不使用注解，也能序列化？
/// A: 如果不使用注解，需要手动实现<see cref="IDsonCodec{T}"/>，并将其添加到注册表中。
/// （也可以加入到Scanner的扫描路径）
///
/// <h3>一些建议</h3>
/// 1. 一般而言，建议使用该注解并遵循相关规范，由注解处理器生成的类负责解析，而不是手动实现<see cref="IDsonCodec{T}"/>。
/// 2. 并不建议都实现为贫血模型。
/// 3. 由于属性较多，因此属性都是get/set，但只应该初始化一次。
/// ///
/// <h3>辅助类类名</h3>
/// 生成的辅助类为{@code XXXCodec}
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
[Serializable]
public class DsonSerializableAttribute : Attribute
{
    /// <summary>
    /// 字符串类型名 -- 相关类<see cref="ClassName"/>。
    ///
    /// 1.第一个元素为默认名。
    /// 2.支持多个以支持别名。
    /// </summary>
    public string[] ClassNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 编码的布局样式
    /// </summary>
    public ObjectStyle Style { get; set; } = ObjectStyle.Indent;

    /// <summary>
    /// 获取单例的方法名（兼容属性）
    /// </summary>
    public string? Singleton { get; set; }

    /// <summary>
    /// 不自动编解码的字段和属性，通常用于跳过不能直接访问的超类字段和属性，然后手动编解码。
    /// (注意：skip仅仅表示不自动读，被跳过的字段仍然会占用字段编号和name！)
    /// </summary>
    public string[] SkipFields { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 为生成代码附加的注解(只支持无参注解)
    /// </summary>
    public Type[] Attributes { get; set; } = Array.Empty<Type>();
}
}