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

namespace Wjybxx.Dson.Codec.Attributes
{
/// <summary>
/// 主要用于为引入的外部库中的Bean自动生成Codec。
/// 1. 默认序列化【目标Bean】的所有可序列化字段，只特殊处理【LinkerBean】中声明的字段。
/// 2. 字段之间通过名字匹配，字段的类型需声明为定义字段的类，以方便未来解决冲突。
/// 3. 【LinkerBean】字段上的<see cref="DsonPropertyAttribute"/>和<see cref="DsonIgnoreAttribute"/>将被映射到【目标Bean】。
/// 4. 字段的读写代理将映射到【LinkerBean】中的静态方法。
/// 5. <see cref="DsonSerializableAttribute"/>中提到的钩子方法也将映射到【LinkerBean】中的静态方法。
/// 6. 如果是泛型类，使用其泛型定义类声明，且当前配置类需要保持相同的泛型参数。
/// 7. C#端存在自动属性，字段映射字段，属性映射属性 —— 都按名字匹配。
/// <pre><code>
///  class MyBeanLinker {
///      MyBean field1; // 表示OuterClass的field1字段
///      MyBean field2;
///
///      // Class
///      public static void BeforeEncode(MyBean instance, ConverterOptions options){}
///      public static void WriteObject(MyBean instance, IDsonObjectWriter writer){}
///      public static void ReadObject(MyBean instance, IDsonObjectReader reader){}
///      public static void AfterDecode(MyBean instance, ConverterOptions options){}
///      // 结构体需要使用ref
///      public static void BeforeEncode(ref MyBean instance, ConverterOptions options){}
///      public static void WriteObject(ref MyBean instance, IDsonObjectWriter writer){}
///      public static void ReadObject(ref MyBean instance, IDsonObjectReader reader){}
///      public static void AfterDecode(ref MyBean instance, ConverterOptions options){}
/// 
///      public static void WriteField1(MyBean instance, IDsonObjectWriter writer, String name){}
///      public static void ReadField1(MyBean instance, IDsonObjectReader reader, String name){}
///  }
/// </code></pre>
///
/// Q：与<see cref="DsonCodecLinkerAttribute"/>的区别？
/// A：该注解用于支持复杂的Codec配置，一个Bean描述一个Bean，而且支持复杂的字段读写代理和序列化钩子。
///
/// Q：为什么要继承？
/// A：我TM也不想啊，定义为组合方式后，无法直接赋值。。。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[Serializable]
public sealed class DsonCodecLinkerBeanAttribute : DsonSerializableAttribute
{
    /// <summary>
    /// 绑定的类型
    /// </summary>
    public Type Target { get; }

    /** 生成类的命名空间 -- 默认为配置类的命名空间 */
    public string? OutputNamespace { get; set; }

    public DsonCodecLinkerBeanAttribute(Type value) {
        Target = value ?? throw new ArgumentNullException(nameof(value));
    }
}
}