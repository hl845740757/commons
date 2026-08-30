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
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
///
/// 其实二进制输入流是可以不缓存为<see cref="DsonCollectionReader{TName}"/>的，
/// 二进制流检索Header信息是很快的，只是不支持随机读。
/// 但对于需要频繁实例化对象的资产文件，用户层缓存为<see cref="DsonArray{TK}"/>会更好；
/// 因为即使二进制的反序列化速度很快，频繁构造字符串的成本也是很高的。
/// </summary>
[ThreadSafe]
public class DefaultDsonConverter : IDsonConverter
{
    private readonly DynamicTypeMetaRegistry typeMetaRegistry;
    private readonly DynamicCodecRegistry codecRegistry;
    private readonly TypeWriteHelper typeWriteHelper;
    private readonly ConverterOptions options;

    internal DefaultDsonConverter(DynamicTypeMetaRegistry typeMetaRegistry,
                                  DynamicCodecRegistry codecRegistry,
                                  TypeWriteHelper typeWriteHelper,
                                  ConverterOptions options) {
        this.typeMetaRegistry = typeMetaRegistry;
        this.codecRegistry = codecRegistry;
        this.typeWriteHelper = typeWriteHelper;
        this.options = options;
    }

    #region other

    public ConverterOptions Options => options;
    public DynamicTypeMetaRegistry TypeMetaRegistry => typeMetaRegistry;
    public DynamicCodecRegistry CodecRegistry => codecRegistry;
    ITypeMetaRegistry IDsonConverter.TypeMetaRegistry => typeMetaRegistry;
    IDsonCodecRegistry IDsonConverter.CodecRegistry => codecRegistry;
    internal TypeWriteHelper TypeWriteHelper => typeWriteHelper;

    public IDsonConverter WithOptions(ConverterOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new DefaultDsonConverter(typeMetaRegistry, codecRegistry, typeWriteHelper, options);
    }

    #endregion

    #region binary

    public byte[] Write(object value, Type declaredType, SerializeFeatures features) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        // 外部销毁流，确保buffer规划到池
        using var outputStream = DsonOutputs.NewInstance(options.bufferPool, options.bufferLength, options.maxBufferLength);
        using DsonBinaryWriter<string> dsonWriter = new DsonBinaryWriter<string>(options.binWriterSettings, outputStream, autoClose: false);
        EncodeObject(dsonWriter, value, declaredType, features);
        return ArrayUtil.CopyOf(outputStream.Buffer, 0, outputStream.Position);
    }

    public object Read(byte[] source, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        using IDsonInput inputStream = DsonInputs.NewInstance(source);
        using DsonBinaryReader<string> dsonReader = new DsonBinaryReader<string>(options.binReaderSettings, inputStream, autoClose: false);
        return DecodeObject(dsonReader, declaredType, features, factory);
    }

    public void Write(object value, Type declaredType, DsonChunk chunk, SerializeFeatures features) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        using IDsonOutput outputStream = DsonOutputs.NewInstance(chunk.Buffer, chunk.Offset, chunk.Length);
        using DsonBinaryWriter<string> dsonWriter = new DsonBinaryWriter<string>(options.binWriterSettings, outputStream, autoClose: false);
        EncodeObject(dsonWriter, value, declaredType, features);
        chunk.Used = outputStream.Position;
    }

    public object Read(DsonChunk chunk, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        using IDsonInput inputStream = DsonInputs.NewInstance(chunk.Buffer, chunk.Offset, chunk.Length);
        using DsonBinaryReader<string> dsonReader = new DsonBinaryReader<string>(options.binReaderSettings, inputStream, autoClose: false);
        object result = DecodeObject(dsonReader, declaredType, features, factory);
        chunk.Used = inputStream.Position;
        return result;
    }

    public void Write(object value, Type declaredType, IDsonOutput output, SerializeFeatures features) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (output == null) throw new ArgumentNullException(nameof(output));
        using DsonBinaryWriter<string> dsonWriter = new DsonBinaryWriter<string>(options.binWriterSettings, output, autoClose: false);
        EncodeObject(dsonWriter, value, declaredType, features);
    }

    public object Read(IDsonInput input, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        if (input == null) throw new ArgumentNullException(nameof(input));
        using DsonBinaryReader<string> dsonReader = new DsonBinaryReader<string>(options.binReaderSettings, input, autoClose: false);
        return DecodeObject(dsonReader, declaredType, features, factory);
    }

    public object CloneObject(object? value, Type declaredType, Type targetType, Func<object>? factory = null) {
        if (value == null) return null!;
        using var outputStream = DsonOutputs.NewInstance(options.bufferPool, options.bufferLength, options.maxBufferLength);
        using DsonBinaryWriter<string> dsonWriter = new DsonBinaryWriter<string>(options.binWriterSettings, outputStream, autoClose: false);
        EncodeObject(dsonWriter, value, declaredType, default);
        // 不销毁
        IDsonInput inputStream = DsonInputs.NewInstance(outputStream.Buffer, 0, outputStream.Position);
        using DsonBinaryReader<string> dsonReader = new DsonBinaryReader<string>(options.binReaderSettings, inputStream, autoClose: false);
        return DecodeObject(dsonReader, targetType, 0, factory);
    }

    private void EncodeObject(IDsonWriter<string> dsonWriter, object value, Type declaredType, SerializeFeatures features) {
        DefaultDsonObjectWriter wrapper = DefaultDsonObjectWriter.GetPooled();
        try {
            wrapper.Init(this, dsonWriter);
            wrapper.AddReference(value);
            wrapper.WriteAll(declaredType, features);
            wrapper.Flush();
        }
        finally {
            DefaultDsonObjectWriter.Release(wrapper);
        }
    }

    private object DecodeObject(IDsonReader<string> dsonReader, Type declaredType, DeserializeFeatures features, Func<object>? factory) {
        DsonArray<string> collection = Dsons.ReadCollection(dsonReader);
        DefaultDsonObjectReader wrapper = DefaultDsonObjectReader.GetPooled();
        try {
            wrapper.Init(this);
            wrapper.AddReferences(collection);
            return wrapper.ReadFirst(declaredType, features, factory);
        }
        finally {
            DefaultDsonObjectReader.Release(wrapper);
        }
    }

    #endregion

    #region dson-single

    public string WriteAsDson(object value, Type declaredType, SerializeFeatures features) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        StringBuilder stringBuilder = options.stringBuilderPool.Acquire();
        using DsonTextWriter textWriter = new DsonTextWriter(options.textWriterSettings, new StringWriter(stringBuilder), false);
        try {
            EncodeObject(textWriter, value, declaredType, features);
            return stringBuilder.ToString();
        }
        finally {
            options.stringBuilderPool.Release(stringBuilder);
        }
    }

    public object ReadFromDson(string source, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        DsonTextReader dsonReader = new DsonTextReader(options.textReaderSettings, source);
        return DecodeObject(dsonReader, declaredType, features, factory);
    }

    public void WriteAsDson(object value, Type declaredType, TextWriter writer, SerializeFeatures features) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        using DsonTextWriter textWriter = new DsonTextWriter(options.textWriterSettings, writer, false);
        EncodeObject(textWriter, value, declaredType, features);
    }

    public object ReadFromDson(TextReader source, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        using DsonTextReader dsonReader = new DsonTextReader(options.textReaderSettings, Dsons.NewStreamScanner(source, false));
        return DecodeObject(dsonReader, declaredType, features, factory);
    }

    public DsonArray<string> WriteAsDsonCollection(object value, Type declaredType, SerializeFeatures features = default) {
        using DsonCollectionWriter<string> dsonWriter = new DsonCollectionWriter<string>(options.binWriterSettings, new DsonArray<string>());
        EncodeObject(dsonWriter, value, declaredType, features);
        return dsonWriter.OutList;
    }

    public object ReadFromDsonCollection(DsonArray<string> collection, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        DefaultDsonObjectReader wrapper = DefaultDsonObjectReader.GetPooled();
        try {
            wrapper.Init(this);
            wrapper.AddReferences(collection);
            return wrapper.ReadFirst(declaredType, features, factory);
        }
        finally {
            DefaultDsonObjectReader.Release(wrapper);
        }
    }

    public object ReadFromDsonCollection(DsonArray<string> collection, long localId, Type declaredType, DeserializeFeatures features = default, Func<object>? factory = null) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        DefaultDsonObjectReader wrapper = DefaultDsonObjectReader.GetPooled();
        try {
            wrapper.Init(this);
            wrapper.AddReferences(collection);
            return wrapper.ReadFirst(declaredType, localId, features, factory);
        }
        finally {
            DefaultDsonObjectReader.Release(wrapper);
        }
    }

    #endregion

    #region dson-colletion

    public string WriteCollectionAsDson<T>(IEnumerable<T> collection, SerializeFeatures features = default) {
        StringBuilder stringBuilder = options.stringBuilderPool.Acquire();
        using DsonTextWriter dsonWriter = new DsonTextWriter(options.textWriterSettings, new StringWriter(stringBuilder), false);
        try {
            WriteCollection(dsonWriter, collection, features);
            return stringBuilder.ToString();
        }
        finally {
            options.stringBuilderPool.Release(stringBuilder);
        }
    }

    public List<T> ReadCollectionFromDson<T>(string dson, DeserializeFeatures features, Func<object>? factory = null) {
        using DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, dson);
        DsonArray<string> collection = Dsons.ReadCollection(textReader);
        return ReadCollection<T>(collection, features, factory);
    }

    public DsonArray<string> WriteCollectionAsDsonCollection<T>(IEnumerable<T> collection, SerializeFeatures features) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        using DsonCollectionWriter<string> dsonWriter = new DsonCollectionWriter<string>(options.binWriterSettings, new DsonArray<string>());
        WriteCollection(dsonWriter, collection, features);
        return dsonWriter.OutList;
    }

    public List<T> ReadCollectionFromDsonCollection<T>(DsonArray<string> collection,
                                                       DeserializeFeatures features, Func<object>? factory = null) {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        return ReadCollection<T>(collection, features, factory);
    }

    private List<T> ReadCollection<T>(DsonArray<string> collection, DeserializeFeatures features, Func<object>? factory = null) {
        DefaultDsonObjectReader wrapper = DefaultDsonObjectReader.GetPooled();
        try {
            wrapper.Init(this);
            wrapper.AddReferences(collection);
            return wrapper.ReadAll<T>(features, factory);
        }
        finally {
            DefaultDsonObjectReader.Release(wrapper);
        }
    }

    private void WriteCollection<T>(IDsonWriter<string> dsonWriter, IEnumerable<T> collection, SerializeFeatures features) {
        DefaultDsonObjectWriter wrapper = DefaultDsonObjectWriter.GetPooled();
        try {
            wrapper.Init(this, dsonWriter);
            wrapper.AddReferences(collection);
            wrapper.WriteAll(typeof(T), features);
            wrapper.Flush();
        }
        finally {
            DefaultDsonObjectWriter.Release(wrapper);
        }
    }

    #endregion
}
}