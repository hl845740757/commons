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
/// 默认实现
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
    ITypeMetaRegistry IDsonConverter.TypeMetaRegistry => typeMetaRegistry;
    IDsonCodecRegistry IDsonConverter.CodecRegistry => codecRegistry;

    public IDsonConverter WithOptions(ConverterOptions options) {
        if (options == null) throw new ArgumentNullException(nameof(options));
        return new DefaultDsonConverter(typeMetaRegistry, codecRegistry, typeWriteHelper, options);
    }

    /// <summary>
    /// 暴露注册表以允许用户提前缓存
    /// </summary>
    public DynamicTypeMetaRegistry TypeMetaRegistry => typeMetaRegistry;

    /// <summary>
    /// 暴露注册表以允许用户提前缓存
    /// </summary>
    public DynamicCodecRegistry CodecRegistry => codecRegistry;

    #endregion

    #region binary

    public byte[] Write(object value, Type declaredType) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        // 外部销毁流，确保buffer规划到池
        using var outputStream = DsonOutputs.NewInstance(options.bufferPool, options.bufferLength, options.maxBufferLength);
        EncodeObject(outputStream, value, declaredType);
        return ArrayUtil.CopyOf(outputStream.Buffer, 0, outputStream.Position);
    }

    public object Read(byte[] source, Type declaredType, Func<object>? factory = null) {
        using IDsonInput inputStream = DsonInputs.NewInstance(source);
        return DecodeObject(inputStream, declaredType, factory);
    }

    public void Write(object value, Type declaredType, DsonChunk chunk) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        using IDsonOutput outputStream = DsonOutputs.NewInstance(chunk.Buffer, chunk.Offset, chunk.Length);
        EncodeObject(outputStream, value, declaredType);
        chunk.Used = outputStream.Position;
    }

    public object Read(DsonChunk chunk, Type declaredType, Func<object>? factory = null) {
        using IDsonInput inputStream = DsonInputs.NewInstance(chunk.Buffer, chunk.Offset, chunk.Length);
        object result = DecodeObject(inputStream, declaredType, factory);
        chunk.Used = inputStream.Position;
        return result;
    }

    public object CloneObject(object? value, Type declaredType, Type targetType, Func<object>? factory = null) {
        if (value == null) return null!;
        if (value.GetType().IsValueType) return value;

        using var dsonOutput = DsonOutputs.NewInstance(options.bufferPool, options.bufferLength, options.maxBufferLength);
        EncodeObject(dsonOutput, value, declaredType);
        // 不销毁
        IDsonInput inputStream = DsonInputs.NewInstance(dsonOutput.Buffer, 0, dsonOutput.Position);
        return DecodeObject(inputStream, targetType, factory);
    }

    /** 注意：由外部销毁输出流 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EncodeObject(IDsonOutput outputStream, object value, Type declaredType) {
        DsonBinaryWriter<string> binaryWriter = new DsonBinaryWriter<string>(options.binWriterSettings, outputStream, autoClose: false);
        using DefaultDsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, typeWriteHelper, binaryWriter);
        wrapper.WriteObject(null, value, declaredType);
        wrapper.Flush();
    }

    /** 注意：由外部销毁输入流 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object DecodeObject(IDsonInput inputStream, Type declaredType, Func<object>? factory) {
        DsonBinaryReader<string> binaryReader = new DsonBinaryReader<string>(options.binReaderSettings, inputStream, autoClose: false);
        using IDsonObjectReader wrapper = WrapReader(binaryReader);
        return wrapper.ReadObject(null, declaredType, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IDsonObjectReader WrapReader(IDsonReader<string> reader) {
        if (options.randomRead) {
            return new BufferedDsonObjectReader(this, ToDsonCollectionReader(reader));
        } else {
            return new DefaultDsonObjectReader(this, reader);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DsonCollectionReader<string> ToDsonCollectionReader(IDsonReader<string> dsonReader) {
        Debug.Assert(dsonReader is not DsonCollectionReader<string>);
        // 如果要优化gc的话，需要传入DsonObject和DsonArray的对象池... 这和外部缓存DsonValue是两个优化
        DsonValue dsonValue = Dsons.ReadTopDsonValue(dsonReader) ?? throw new DsonCodecException("eof");
        return DsonCollectionReader<string>.UnsafeCreate(options.binReaderSettings, dsonValue);
    }

    #endregion

    #region text

    public string WriteAsDson(object value, Type declaredType, ObjectStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        StringBuilder stringBuilder = options.stringBuilderPool.Acquire();
        try {
            WriteAsDson(value, declaredType, new StringWriter(stringBuilder), style);
            return stringBuilder.ToString();
        }
        finally {
            options.stringBuilderPool.Release(stringBuilder);
        }
    }

    public object ReadFromDson(string source, Type declaredType, Func<object>? factory = null) {
        DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, source);
        using IDsonObjectReader wrapper = WrapReader(textReader);
        return wrapper.ReadObject(null, declaredType, factory);
    }

    public void WriteAsDson(object value, Type declaredType, TextWriter writer, ObjectStyle? style = null) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        DsonTextWriter textWriter = new DsonTextWriter(options.textWriterSettings, writer, false);
        using DefaultDsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, typeWriteHelper, textWriter);
        wrapper.WriteObject(null, value, declaredType, style);
        wrapper.Flush();
    }

    public object ReadFromDson(TextReader source, Type declaredType, Func<object>? factory = null) {
        DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.NewStreamScanner(source, false));
        using IDsonObjectReader wrapper = WrapReader(textReader);
        return wrapper.ReadObject(null, declaredType, factory);
    }

    public DsonValue WriteAsDsonValue(object value, Type declaredType) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        DsonArray<string> outList = new DsonArray<string>(1);
        IDsonWriter<string> objectWriter = new DsonCollectionWriter<string>(options.binWriterSettings, outList);
        using IDsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, typeWriteHelper, objectWriter);

        wrapper.WriteObject(null, value, declaredType, ObjectStyle.Flow);
        DsonValue dsonValue = outList[0];
        if (dsonValue.DsonType.IsContainer()) {
            return dsonValue;
        }
        throw new AggregateException("value must be container");
    }

    public object ReadFromDsonValue(DsonValue source, Type declaredType, Func<object>? factory = null) {
        if (!source.DsonType.IsContainer()) {
            throw new ArgumentException("value must be container");
        }
        DsonCollectionReader<string> objectReader =
            DsonCollectionReader<string>.UnsafeCreate(options.binReaderSettings, source);
        using IDsonObjectReader wrapper = new BufferedDsonObjectReader(this, objectReader);
        return wrapper.ReadObject(null, declaredType, factory);
    }

    public DsonValue ReadAsDsonValue(TextReader source) {
        using DsonTextReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.NewStreamScanner(source, false));
        return Dsons.ReadTopDsonValue(textReader)!;
    }

    #endregion
}
}