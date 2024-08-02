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

using System.Collections.Generic;
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
    /// 是否弱顺序。
    /// 对于集合和字典，要保证解码的正确性，就必须保持插入序。而C#未提供原生的高性能的保持插入序的Set和Dictionary，
    /// 我实现的<see cref="LinkedHashSet{TKey}"/>和<see cref="LinkedDictionary{TKey,TValue}"/>虽然能保持插入序，
    /// 但优化做的较少，可能会产生较多的小对象，增加gc开销。
    ///
    /// ps：根据观察，系统的<see cref="Dictionary{TKey,TValue}"/>在只插入的情况下是保持有序的，但有删除操作的情况下就会无序。
    /// </summary>
    public readonly bool weakOrder;

    /// <summary>
    /// classId的写入策略
    /// </summary>
    public readonly ClassIdPolicy classIdPolicy;
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
    /// 是否把Map(字典)编码为普通对象（文档）
    /// 1.只在文档编解码中生效
    /// 2.如果要将一个Map结构编码为普通对象，<b>Key的运行时必须和声明类型相同</b>，且只支持String、Integer、Long、Enum。
    /// 3.在不启用该选项的情况下，用户可通过字段写代理将字段转换为<see cref="DictionaryEncodeProxy{V}"/>，实现更精确的控制。
    ///
    /// <h3>Map不是Object</h3>
    /// 本质上讲，Map是数组，而不是普通的Object，因为标准的Map是允许复杂key的，因此Map默认应该序列化为数组。但存在两个特殊的场景：
    /// 1.与脚本语言通信
    /// 脚本语言通常没有静态语言中的字典结构，由object充当，但object不支持复杂的key作为键，通常仅支持数字和字符串作为key。
    /// 因此在与脚本语言通信时，要求将Map序列化为简单的object。
    /// 2.配置文件读写
    /// 配置文件通常是无类型的，因此读取到内存中通常是一个字典结构；程序在输出配置文件时，同样需要将字典结构输出为object结构。
    /// </summary>
    public readonly bool writeMapAsDocument;
    /// <summary>
    /// 是否将枚举写为字符串
    /// 1.只在文档编码中生效(DsonCodec)
    /// 2.对字典的Key也生效
    /// </summary>
    public readonly bool writeEnumAsString;
    /// <summary>
    /// 是否启用随机读
    /// 启用随机读会增加较多的开销，需要先读取为中间结构，再解码为对象；但启用随机读的数据兼容性更好。
    /// 如果不写入默认值和null值的，通常都需要启用该特性。
    /// 如果需要反复反序列化一个对象(通常是配置文件)，可以先解码为中间对象，将中间对象保存下来。
    /// 另一种方式是先反序列化，然后完整序列化为字节数组，再通过字节数组反序列化 -- 可关闭随机读。
    /// </summary>
    public readonly bool randomRead;

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
    ///  默认启用！因为我们假设afterDecode仅依赖自身数据。
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
    public readonly int bufferSize;
    /** 字节数组缓存池 -- 多线程下需要注意线程安全问题 */
    public readonly IArrayPool<byte> bufferPool;
    /** 字典key队列缓存池 */
    public readonly IObjectPool<MultiChunkDeque<string>> keySetPool;
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
        this.weakOrder = builder.WeakOrder;
        this.classIdPolicy = builder.ClassIdPolicy;
        this.appendDef = builder.AppendDef;
        this.appendNull = builder.AppendNull;
        this.writeMapAsDocument = builder.WriteMapAsDocument;
        this.writeEnumAsString = builder.WriteEnumAsString;
        this.randomRead = builder.RandomRead;
        this.enableBeforeEncode = builder.EnableBeforeEncode;
        this.enableAfterDecode = builder.EnableAfterDecode;

        this.pbBinaryType = builder.PbBinaryType;
        this.usage = builder.Usage;

        this.bufferSize = builder.BufferSize;
        this.bufferPool = builder.BufferPool;
        this.keySetPool = builder.KeySetPool;
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
        builder.WeakOrder = weakOrder;
        builder.ClassIdPolicy = classIdPolicy;
        builder.AppendDef = appendDef;
        builder.AppendNull = appendNull;
        builder.WriteMapAsDocument = writeMapAsDocument;
        builder.WriteEnumAsString = writeEnumAsString;
        builder.RandomRead = randomRead;
        builder.EnableBeforeEncode = enableBeforeEncode;
        builder.EnableAfterDecode = enableAfterDecode;

        builder.PbBinaryType = pbBinaryType;
        builder.Usage = usage;

        builder.BufferSize = bufferSize;
        builder.BufferPool = bufferPool;
        builder.KeySetPool = keySetPool;
        builder.StringBuilderPool = stringBuilderPool;

        builder.BinReaderSettings = binReaderSettings;
        builder.BinWriterSettings = binWriterSettings;
        builder.TextReaderSettings = textReaderSettings;
        builder.TextWriterSettings = textWriterSettings;
    }


    /** 全局共享的key队列 */
    public static readonly IObjectPool<MultiChunkDeque<string>> SHARED_KEY_SET_POOL
        = new ConcurrentObjectPool<MultiChunkDeque<string>>(
            () => new MultiChunkDeque<string>(32, 4), queue => queue.Clear(),
            64);
    /** 默认的Options */
    public static readonly ConverterOptions DEFAULT = NewBuilder().Build(); // 有初始化顺序依赖

    public static Builder NewBuilder() {
        return new Builder();
    }

    public class Builder
    {
        public bool WeakOrder { get; set; } = true;
        public ClassIdPolicy ClassIdPolicy { get; set; } = ClassIdPolicy.Optimized;
        public bool AppendDef { get; set; } = true;
        public bool AppendNull { get; set; } = true;
        public bool WriteMapAsDocument { get; set; } = false;
        public bool WriteEnumAsString { get; set; } = false;
        public bool RandomRead { get; set; } = true;
        public bool EnableBeforeEncode { get; set; } = false;
        public bool EnableAfterDecode { get; set; } = true;

        public int PbBinaryType { get; set; } = 127;
        public int Usage { get; set; } = 0;

        public int BufferSize { get; set; } = 8192;
        public IArrayPool<byte> BufferPool { get; set; } = IArrayPool<byte>.Shared;
        public IObjectPool<MultiChunkDeque<string>> KeySetPool = SHARED_KEY_SET_POOL;
        public IObjectPool<StringBuilder> StringBuilderPool { get; set; } = ConcurrentObjectPool.SharedStringBuilderPool;

        public DsonReaderSettings BinReaderSettings { get; set; } = DsonReaderSettings.Default;
        public DsonWriterSettings BinWriterSettings { get; set; } = DsonWriterSettings.Default;
        public DsonTextReaderSettings TextReaderSettings { get; set; } = DsonTextReaderSettings.Default;
        public DsonTextWriterSettings TextWriterSettings { get; set; } = DsonTextWriterSettings.Default;

        public virtual ConverterOptions Build() => new ConverterOptions(this);
    }
}
}