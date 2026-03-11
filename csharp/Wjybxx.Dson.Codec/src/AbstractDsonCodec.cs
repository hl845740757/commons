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
using System.Collections.Concurrent;
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using static Wjybxx.Dson.Codec.AbstractDsonCodec;

namespace Wjybxx.Dson.Codec
{
internal static class AbstractDsonCodec
{
    private static readonly ConcurrentDictionary<Type, int> cache = new();
    internal const int MASK_BEFORE_ENCODE = 0x01;
    internal const int MASK_AFTER_DECODE = 0x02;
    internal const int MASK_WRITE_OBJECT = 0x04;
    internal const int MASK_READ_OBJECT = 0x08;
    internal const int MASK_READ_FIELD = 0x10;

    public static int GetOverrides(Type type) {
        if (type.IsGenericType) {
            type = type.GetGenericTypeDefinition();
        }
        if (cache.TryGetValue(type, out int r)) {
            return r;
        }
        r = 0XFF;
        if (!IsOverwrite(type, "BeforeEncode")) r &= ~MASK_BEFORE_ENCODE;
        if (!IsOverwrite(type, "AfterDecode")) r &= ~MASK_AFTER_DECODE;
        if (!IsOverwrite(type, "WriteObject")) r &= ~MASK_AFTER_DECODE;
        if (!IsOverwrite(type, "ReadObject")) r &= ~MASK_AFTER_DECODE;
        if (!IsOverwrite(type, "ReadField")) r &= ~MASK_AFTER_DECODE;
        cache.TryAdd(type, r);
        return r;
    }

    private static bool IsOverwrite(Type type, string methodName) {
        MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (methodInfo == null) throw new AssertionError();
        Type declaringType = methodInfo.DeclaringType!;
        if (declaringType.IsGenericType) {
            declaringType = declaringType.GetGenericTypeDefinition();
        }
        return declaringType != typeof(AbstractDsonCodec<>);
    }
}

/// <summary>
/// 生成代码默认都会实现该类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AbstractDsonCodec<T> : IDsonCodec<T>
{
    private readonly int _overrides;

    protected AbstractDsonCodec() {
        _overrides = GetOverrides(GetType());
    }

    [StableName]
    public virtual Type GetEncoderType() => typeof(T);

    #region Write

    private bool IsWriteAsArray(SerializeFeatures features, TypeMeta typeMeta, ConverterOptions options) {
        // 这一波波测试真的有点浪费开销，还好我现在不那么追求性能了...
        return (features & SerializeFeatures.WriteAsArray) != 0
               || (typeMeta.encodeFeatures & SerializeFeatures.WriteAsArray) != 0
               || (options.encodeFeatures & SerializeFeatures.WriteAsArray) != 0;
    }

    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, SerializeFeatures features) {
        Type encoderType = GetEncoderType();
        TypeMeta typeMeta = writer.TypeMetaRegistry.OfType(encoderType);
        if (typeMeta == null) {
            throw DsonCodecException.UnsupportedKeyType(encoderType);
        }
        bool isWriteAsArray = IsWriteAsArray(features, typeMeta, writer.Options);
        if (isWriteAsArray) {
            writer.WriteStartArray(typeMeta, features);
        } else {
            writer.WriteStartObject(typeMeta, features);
        }
        writer.WriteHeader(encoderType, declaredType, features);
        //
        if ((_overrides & MASK_BEFORE_ENCODE) != 0 && writer.Options.enableBeforeEncode) {
            BeforeEncode(writer, ref inst);
        }
        if ((_overrides & MASK_WRITE_OBJECT) != 0) {
            WriteObject(writer, ref inst);
        }
        WriteFields(writer, ref inst);
        //
        if (isWriteAsArray) {
            writer.WriteEndArray();
        } else {
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// 调用用户的BeforeEncode钩子方法
    /// </summary>
    [StableName]
    protected virtual void BeforeEncode(IDsonObjectWriter writer, ref T inst) {
    }

    /// <summary>
    /// 调用用户的WriteObject钩子方法
    /// </summary>
    [StableName]
    protected virtual void WriteObject(IDsonObjectWriter writer, ref T inst) {
    }

    /// <summary>
    /// 写入托管字段，可能是子类实例
    /// </summary>
    [StableName]
    protected abstract void WriteFields(IDsonObjectWriter writer, ref T inst);

    #endregion

    #region Read

    [StableName]
    public T ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        DsonType containerType = reader.CurrentDsonType;
        if (containerType == DsonType.Object) {
            bool passiveReading = (_overrides & MASK_READ_FIELD) != 0;
            reader.ReadStartObject(GetEncoderType(), passiveReading ? DeserializeFeatures.PassiveReading : 0);
        } else {
            reader.ReadStartArray(GetEncoderType());
        }
        // cast失败则抛出异常，不能测试类型，可能隐藏错误
        T inst = factory != null ? (T)factory() : NewInstance(reader);
        if (!typeof(T).IsValueType) {
            reader.PublishReference(inst);
        }
        if ((_overrides & MASK_READ_OBJECT) != 0) {
            ReadObject(reader, ref inst);
        }
        if ((_overrides & MASK_READ_FIELD) != 0 && containerType == DsonType.Object) {
            while (reader.ReadDsonType() != DsonType.EndOfObject) {
                string name = reader.ReadName();
                if (!ReadField(reader, ref inst, name)) {
                    reader.SkipValue();
                }
            }
        } else {
            ReadFields(reader, ref inst);
        }
        if ((_overrides & MASK_AFTER_DECODE) != 0 && reader.Options.enableAfterDecode) {
            AfterDecode(reader, ref inst);
        }
        //
        if (containerType == DsonType.Object) {
            reader.ReadEndObject();
        } else {
            reader.ReadEndArray();
        }
        // 值类型需要在完全解码之后才可发布引用 - 由外部发布引用的开销更低
        // if (typeof(T).IsValueType) {
        //     reader.PublishReference(in inst);
        // }
        return inst;
    }

    /// <summary>
    /// 创建一个实例（可以是子类实例）
    /// 1. 如果是抽象类，应当抛出异常
    /// 2. 该方法可解决readonly字段问题。
    /// </summary>
    [StableName]
    protected abstract T NewInstance(IDsonObjectReader reader);

    /// <summary>
    /// 调用用户的ReadObject钩子方法
    ///
    /// 该方法与<see cref="ReadFields"/>方法分离，以方便用户重写<see cref="ReadFields"/>方法；
    /// 同时方便Switch-Case随机读实现。
    /// </summary>
    [StableName]
    protected virtual void ReadObject(IDsonObjectReader reader, ref T inst) {
    }

    /// <summary>
    /// 读取所有字段
    ///
    /// 注：如果支持随机读，请重写<see cref="ReadField"/>方法。
    /// </summary>
    [StableName]
    protected abstract void ReadFields(IDsonObjectReader reader, ref T inst);

    /// <summary>
    /// 读取单个字段
    ///
    /// 1.如果用户实现了该方法，则表示支持Switch-Case随机读，则由框架类完成输入流的读取。
    /// 2.如果输入流为数组类型，则不会调用该方法；仍需要实现<see cref="ReadFields"/>方法。
    /// 3.返回值用于判断超类是否成功读取了字段。
    /// 4.该方法主要用于简化POJO生成代码。
    /// </summary>
    [StableName]
    protected virtual bool ReadField(IDsonObjectReader reader, ref T inst, string name) {
        return false;
    }

    /// <summary>
    /// 调用用户的AfterDecode方法
    /// </summary>
    [StableName]
    protected virtual void AfterDecode(IDsonObjectReader reader, ref T inst) {
    }

    #endregion
}
}