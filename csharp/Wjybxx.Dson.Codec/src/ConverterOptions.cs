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

using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 序列化选项
/// </summary>
[Immutable]
public class ConverterOptions
{
    /// <summary>
    /// classId的写入策略
    /// </summary>
    public readonly TypeWritePolicy typeWritePolicy;
    /// <summary>
    /// 是否写入对象基础类型字段的默认值
    /// 1.数值类型默认值为0，bool类型默认值为false
    /// 2.只在Object上下文生效
    ///
    /// 基础值类型需要单独控制，因为有时候我们仅想不输出null，但要输出基础类型字段的默认值 -- 通常是在文本模式下。
    /// </summary>
    public readonly bool appendDef;
    /// <summary>
    /// 是否写入对象内的null值
    /// 1.只在Object上下文生效
    /// 2.对于一般的对象可不写入，因为ObjectReader是支持随机读的
    /// </summary>
    public readonly bool appendNull;
    /// <summary>
    /// 字典的编码策略
    /// </summary>
    public readonly MapEncodePolicy mapEncodePolicy;
    /// <summary>
    /// 是否将枚举写为字符串
    /// 1.不适用字典的Key，当字典需要被编码为Document时，枚举将固定输出为数字 -- 可通过字段编解码代码自定义格式化。
    /// 2.通常不建议开启，兼容性不好；如果个别字段的字典想定制编码，可通过字段编解码代理实现。
    /// </summary>
    public readonly bool writeEnumAsString;
    /// <summary>
    /// 是否将普通object编码为数组
    /// 1.如果开启该选项，将不写入object的字段名，只是顺序写入object的所有字段值。
    /// 2.这可以避免大量的字符串编解码，从而提升性能 - 适用于非持久化场景。
    /// 3.该选项仅对<see cref="IDsonCodec.AutoStartEnd"/>为true的编码器有效。
    /// 4.不可以有基于name进行Switch编解码的codec。
    /// 5.对象字段不可以有特殊的初始值 -- 否则会被反序列化覆盖。
    /// </summary>
    public readonly bool writeObjectAsArray;

    /// <summary>
    /// 是否启用随机读
    /// 启用随机读会增加较多的开销，需要先读取为中间结构，再解码为对象；但启用随机读的数据兼容性更好。
    /// 如果不写入默认值和null值的，通常都需要启用该特性。
    /// 如果需要反复反序列化一个对象(通常是配置文件)，可以先解码为中间对象，将中间对象保存下来。
    /// 另一种方式是先反序列化，然后完整序列化为字节数组，再通过字节数组反序列化 -- 可关闭随机读。
    /// </summary>
    public readonly bool randomRead;
    /// <summary>
    /// 集合类型是否读取为不可变
    /// 其它类型的对象也可以使用该设置
    /// </summary>
    public readonly bool readAsImmutable;
    /// <summary>
    /// 是否启用BeforeEncode钩子方法。
    /// 默认不启用！因为启用该特性要求同一个Bean不能被多线程同时序列化 -- 只适用单线程序列化场景，
    /// <code>
    /// public void BeforeEncode(ConverterOptions) {}
    /// </code>
    /// </summary>
    public readonly bool enableBeforeEncode;
    /// <summary>
    /// 是否启用AfterDecode钩子方法。
    /// 默认启用！因为我们假设afterDecode仅依赖自身数据。
    /// <code>
    /// public void AfterDecode(ConverterOptions) {}
    /// </code>
    /// </summary>
    public readonly bool enableAfterDecode;

    /** protoBuf对应的二进制子类型 -- 其它模块依赖 */
    public readonly int pbBinaryType;
    /** converter的用途 -- 用于判断是临时序列化，还是持久化入库等 */
    public readonly int usage;

    /** 序列化申请的字节数组大小 */
    public readonly int bufferLength;
    /** 序列化申请的最大字节数组大小 */
    public readonly int maxBufferLength;
    /** 字节数组缓存池 -- 多线程下需要注意线程安全问题 */
    public readonly IArrayPool<byte> bufferPool;
    /** 字符串缓存池 -- 多线程下需要注意线程安全问题 */
    public readonly IObjectPool<StringBuilder> stringBuilderPool;

    /** 二进制解码设置 */
    public readonly DsonReaderSettings binReaderSettings;
    /** 二进制编码设置 */
    public readonly DsonWriterSettings binWriterSettings;
    /** 文本解码设置 */
    public readonly DsonTextReaderSettings textReaderSettings;
    /** 文本编码设置 */
    public readonly DsonTextWriterSettings textWriterSettings;

    public ConverterOptions(Builder builder) {
        this.typeWritePolicy = builder.TypeWritePolicy;
        this.appendDef = builder.AppendDef;
        this.appendNull = builder.AppendNull;
        this.mapEncodePolicy = builder.MapEncodePolicy;
        this.writeEnumAsString = builder.WriteEnumAsString;
        this.writeObjectAsArray = builder.WriteObjectAsArray;

        this.randomRead = builder.RandomRead;
        this.readAsImmutable = builder.ReadAsImmutable;
        this.enableBeforeEncode = builder.EnableBeforeEncode;
        this.enableAfterDecode = builder.EnableAfterDecode;

        this.pbBinaryType = builder.PbBinaryType;
        this.usage = builder.Usage;

        this.bufferLength = builder.BufferLength;
        this.maxBufferLength = builder.MaxBufferLength;
        this.bufferPool = builder.BufferPool;
        this.stringBuilderPool = builder.StringBuilderPool;

        this.binReaderSettings = builder.BinReaderSettings;
        this.binWriterSettings = builder.BinWriterSettings;
        this.textReaderSettings = builder.TextReaderSettings;
        this.textWriterSettings = builder.TextWriterSettings;
    }

    public Builder ToBuilder() {
        Builder builder = new Builder();
        AssignToBuilder(builder);
        return builder;
    }

    /** 允许子类重写 */
    public virtual void AssignToBuilder(Builder builder) {
        builder.TypeWritePolicy = typeWritePolicy;
        builder.AppendDef = appendDef;
        builder.AppendNull = appendNull;
        builder.MapEncodePolicy = mapEncodePolicy;
        builder.WriteEnumAsString = writeEnumAsString;
        builder.WriteObjectAsArray = writeObjectAsArray;

        builder.RandomRead = randomRead;
        builder.ReadAsImmutable = readAsImmutable;
        builder.EnableBeforeEncode = enableBeforeEncode;
        builder.EnableAfterDecode = enableAfterDecode;

        builder.PbBinaryType = pbBinaryType;
        builder.Usage = usage;

        builder.BufferLength = bufferLength;
        builder.MaxBufferLength = maxBufferLength;
        builder.BufferPool = bufferPool;
        builder.StringBuilderPool = stringBuilderPool;

        builder.BinReaderSettings = binReaderSettings;
        builder.BinWriterSettings = binWriterSettings;
        builder.TextReaderSettings = textReaderSettings;
        builder.TextWriterSettings = textWriterSettings;
    }

    /** 默认的Options */
    public static readonly ConverterOptions DEFAULT = NewBuilder().Build(); // 有初始化顺序依赖

    public static Builder NewBuilder() {
        return new Builder();
    }

    public class Builder
    {
        public TypeWritePolicy TypeWritePolicy { get; set; } = TypeWritePolicy.Optimized;
        public bool AppendDef { get; set; } = true;
        public bool AppendNull { get; set; } = true;
        public MapEncodePolicy MapEncodePolicy { get; set; } = MapEncodePolicy.Array;
        public bool WriteEnumAsString { get; set; } = false;
        public bool WriteObjectAsArray { get; set; } = false;
        public bool RandomRead { get; set; } = true;
        public bool ReadAsImmutable { get; set; } = false;
        public bool EnableBeforeEncode { get; set; } = false;
        public bool EnableAfterDecode { get; set; } = true;

        public int PbBinaryType { get; set; } = 127;
        public int Usage { get; set; } = 0;

        public int BufferLength { get; set; } = 8192;
        public int MaxBufferLength { get; set; } = 1024 * 1024;
        public IArrayPool<byte> BufferPool { get; set; } = IArrayPool<byte>.Shared;
        public IObjectPool<StringBuilder> StringBuilderPool { get; set; } = ConcurrentObjectPool.SharedStringBuilderPool;

        public DsonReaderSettings BinReaderSettings { get; set; } = DsonReaderSettings.Default;
        public DsonWriterSettings BinWriterSettings { get; set; } = DsonWriterSettings.Default;
        public DsonTextReaderSettings TextReaderSettings { get; set; } = DsonTextReaderSettings.Default;
        public DsonTextWriterSettings TextWriterSettings { get; set; } = DsonTextWriterSettings.Default;

        public virtual ConverterOptions Build() => new ConverterOptions(this);
    }
}
}