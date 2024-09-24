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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 默认实现
/// </summary>
[ThreadSafe]
public class DefaultDsonConverter : IDsonConverter
{
    private readonly DynamicTypeMetaRegistry typeMetaRegistry;
    private readonly DynamicDsonCodecRegistry codecRegistry;
    private readonly ConverterOptions options;

    private DefaultDsonConverter(DynamicTypeMetaRegistry typeMetaRegistry,
                                 DynamicDsonCodecRegistry codecRegistry,
                                 ConverterOptions options) {
        this.typeMetaRegistry = typeMetaRegistry;
        this.codecRegistry = codecRegistry;
        this.options = options;
    }

    #region other

    public ConverterOptions Options => options;
    ITypeMetaRegistry IDsonConverter.TypeMetaRegistry => typeMetaRegistry;
    IDsonCodecRegistry IDsonConverter.CodecRegistry => codecRegistry;

    public IDsonConverter WithOptions(ConverterOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new DefaultDsonConverter(typeMetaRegistry, codecRegistry, options);
    }

    /// <summary>
    /// 暴露注册表以允许用户提前缓存
    /// </summary>
    public DynamicTypeMetaRegistry TypeMetaRegistry => typeMetaRegistry;

    /// <summary>
    /// 暴露注册表以允许用户提前缓存
    /// </summary>
    public DynamicDsonCodecRegistry CodecRegistry => codecRegistry;

    #endregion

    #region binary

    public byte[] Write<T>(in T value, Type declaredType) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        byte[] localBuffer = options.bufferPool.Acquire(options.bufferSize);
        try {
            DsonChunk chunk = new DsonChunk(localBuffer);
            Write(in value, declaredType, chunk);
            return chunk.UsedPayload();
        }
        finally {
            options.bufferPool.Release(localBuffer);
        }
    }

    public void Write<T>(in T value, Type declaredType, DsonChunk chunk) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        IDsonOutput outputStream = DsonOutputs.NewInstance(chunk.Buffer, chunk.Offset, chunk.Length);
        EncodeObject(outputStream, in value, declaredType);
        chunk.Used = outputStream.Position;
    }

    public T Read<T>(DsonChunk chunk, Func<T>? factory = null) {
        IDsonInput inputStream = DsonInputs.NewInstance(chunk.Buffer, chunk.Offset, chunk.Length);
        return DecodeObject<T>(inputStream, factory);
    }

    public U CloneObject<T, U>(in T value, Func<U>? factory = null) {
        if (value == null) return default;
        byte[] localBuffer = options.bufferPool.Acquire(options.bufferSize);
        try {
            IDsonOutput outputStream = DsonOutputs.NewInstance(localBuffer);
            EncodeObject<T>(outputStream, in value, typeof(T));

            IDsonInput inputStream = DsonInputs.NewInstance(localBuffer, 0, outputStream.Position);
            return DecodeObject<U>(inputStream, factory);
        }
        finally {
            options.bufferPool.Release(localBuffer);
        }
    }

    private void EncodeObject<T>(IDsonOutput outputStream, in T value, Type declaredType) {
        DsonBinaryWriter<string> binaryWriter = new DsonBinaryWriter<string>(options.binWriterSettings, outputStream);
        using IDsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, binaryWriter);
        wrapper.WriteObject(null, in value, declaredType);
        wrapper.Flush();
    }

    private T DecodeObject<T>(IDsonInput inputStream, Func<T>? factory) {
        IDsonReader<string> binaryReader = new DsonBinaryReader<string>(options.binReaderSettings, inputStream);
        using IDsonObjectReader wrapper = WrapReader(binaryReader);
        return wrapper.ReadObject(null, typeof(T), factory);
    }

    private IDsonObjectReader WrapReader(IDsonReader<string> reader) {
        if (options.randomRead) {
            return new BufferedDsonObjectReader(this, ToDsonCollectionReader(reader));
        } else {
            return new DefaultDsonObjectReader(this, reader);
        }
    }

    private DsonCollectionReader<string> ToDsonCollectionReader(IDsonReader<string> dsonReader) {
        Debug.Assert(dsonReader is not DsonCollectionReader<string>);
        // 如果要优化gc的话，需要传入DsonObject和DsonArray的对象池... 这和外部缓存DsonValue是两个优化
        DsonValue dsonValue = Dsons.ReadTopDsonValue(dsonReader) ?? throw new DsonCodecException("input is empty");
        return new DsonCollectionReader<string>(options.binReaderSettings, new DsonArray<string>().Append(dsonValue));
    }

    #endregion

    #region text

    public string WriteAsDson<T>(in T value, Type declaredType, ObjectStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        StringBuilder stringBuilder = options.stringBuilderPool.Acquire();
        try {
            WriteAsDson(in value, declaredType, new StringWriter(stringBuilder), style);
            return stringBuilder.ToString();
        }
        finally {
            options.stringBuilderPool.Release(stringBuilder);
        }
    }

    public T ReadFromDson<T>(string source, Func<T>? factory = null) {
        IDsonReader<string> textReader = new DsonTextReader(options.textReaderSettings, source);
        using IDsonObjectReader wrapper = WrapReader(textReader);
        return wrapper.ReadObject(null, typeof(T), factory);
    }

    public void WriteAsDson<T>(in T value, Type declaredType, TextWriter writer, ObjectStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        DsonTextWriter textWriter = new DsonTextWriter(options.textWriterSettings, writer, false);
        using IDsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, textWriter);
        wrapper.WriteObject(null, in value, declaredType, style);
        wrapper.Flush();
    }

    public T ReadFromDson<T>(TextReader source, Func<T>? factory = null) {
        DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.NewStreamScanner(source, false));
        using IDsonObjectReader wrapper = WrapReader(textReader);
        return wrapper.ReadObject(null, typeof(T), factory);
    }

    public DsonValue WriteAsDsonValue<T>(in T value, Type declaredType) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        DsonArray<string> outList = new DsonArray<string>(1);
        IDsonWriter<string> objectWriter = new DsonCollectionWriter<string>(options.binWriterSettings, outList);
        using IDsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, objectWriter);

        wrapper.WriteObject(null, in value, declaredType, ObjectStyle.Flow);
        DsonValue dsonValue = outList[0];
        if (dsonValue.DsonType.IsContainer()) {
            return dsonValue;
        }
        throw new AggregateException("value must be container");
    }

    public T ReadFromDsonValue<T>(DsonValue source, Func<T>? factory = null) {
        if (!source.DsonType.IsContainer()) {
            throw new ArgumentException("value must be container");
        }
        DsonCollectionReader<string> objectReader =
            new DsonCollectionReader<string>(options.binReaderSettings, new DsonArray<string>().Append(source));
        using IDsonObjectReader wrapper = new BufferedDsonObjectReader(this, objectReader);
        return wrapper.ReadObject(null, typeof(T), factory);
    }

    public DsonValue ReadAsDsonValue(TextReader source) {
        using DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.NewStreamScanner(source, false));
        return Dsons.ReadTopDsonValue(textReader)!;
    }

    public DsonArray<string> ReadAsDsonCollection(TextReader source) {
        using DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.NewStreamScanner(source, false));
        return Dsons.ReadCollection(textReader)!;
    }

    #endregion

    #region factory

    /**
     * @param pojoCodecImplList 所有的普通对象编解码器，外部传入，因此用户可以处理冲突后传入
     * @param typeMetaRegistry  所有的类型id信息，包括protobuf的类
     * @param options           一些可选项
     */
    public static DefaultDsonConverter NewInstance(ITypeMetaRegistry typeMetaRegistry,
                                                   IList<IDsonCodec> pojoCodecList,
                                                   IGenericCodecConfig genericCodecConfig,
                                                   ConverterOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        // 检查classId是否存在，以及命名是否非法
        foreach (IDsonCodec rawCodec in pojoCodecList) {
            typeMetaRegistry.CheckedOfType(rawCodec.GetEncoderClass());
        }
        // 转换codecImpl
        List<DsonCodecImpl> allPojoCodecList = new List<DsonCodecImpl>(pojoCodecList.Count);
        foreach (IDsonCodec rawCodec in pojoCodecList) {
            Type genericType = typeof(DsonCodecImpl<>).MakeGenericType(rawCodec.GetEncoderClass());
            DsonCodecImpl codecImpl = (DsonCodecImpl)Activator.CreateInstance(genericType, rawCodec);
            allPojoCodecList.Add(codecImpl);
        }
        // 泛型支持
        DynamicTypeMetaRegistry dynamicTypeMetaRegistry = new DynamicTypeMetaRegistry(
            TypeMetaRegistries.FromRegistries(typeMetaRegistry,
                DsonConverterUtils.GetDefaultTypeMetaRegistry()));
        DynamicDsonCodecRegistry dynamicDsonCodecRegistry = new DynamicDsonCodecRegistry(
            DsonCodecRegistries.FromRegistries(
                DsonCodecRegistries.FromCodecs(allPojoCodecList),
                DsonConverterUtils.GetDefaultCodecRegistry()),
            genericCodecConfig);

        return new DefaultDsonConverter(
            dynamicTypeMetaRegistry,
            dynamicDsonCodecRegistry,
            options);
    }

    /**
     * @param registryList     可以包含一些特殊的registry
     * @param typeMetaRegistry 所有的类型id信息，包括protobuf的类
     * @param options          一些可选项
     * @return
     */
    public static DefaultDsonConverter NewInstance2(ITypeMetaRegistry typeMetaRegistry,
                                                    IList<IDsonCodecRegistry> registryList,
                                                    IGenericCodecConfig genericCodecConfig,
                                                    ConverterOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        // 添加默认Registry
        if (!registryList.Contains(DsonConverterUtils.GetDefaultCodecRegistry())) {
            List<IDsonCodecRegistry> copied = new List<IDsonCodecRegistry>(registryList.Count + 1);
            copied.AddRange(registryList);
            copied.Add(DsonConverterUtils.GetDefaultCodecRegistry());
            registryList = copied;
        }
        // 泛型支持
        DynamicTypeMetaRegistry dynamicTypeMetaRegistry = new DynamicTypeMetaRegistry(
            TypeMetaRegistries.FromRegistries(typeMetaRegistry,
                DsonConverterUtils.GetDefaultTypeMetaRegistry()));
        DynamicDsonCodecRegistry dynamicDsonCodecRegistry = new DynamicDsonCodecRegistry(
            DsonCodecRegistries.FromRegistries(registryList),
            genericCodecConfig);

        return new DefaultDsonConverter(
            dynamicTypeMetaRegistry,
            dynamicDsonCodecRegistry,
            options);
    }

    #endregion
}
}