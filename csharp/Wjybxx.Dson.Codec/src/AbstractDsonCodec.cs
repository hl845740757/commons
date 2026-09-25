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
using System.Runtime.CompilerServices;
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
        if (!IsOverwrite(type, "WriteObject")) r &= ~MASK_WRITE_OBJECT;
        if (!IsOverwrite(type, "ReadObject")) r &= ~MASK_READ_OBJECT;
        if (!IsOverwrite(type, "ReadField")) r &= ~MASK_READ_FIELD;
        cache.TryAdd(type, r);
        return r;
    }

    private static bool IsOverwrite(Type type, string methodName) {
        MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (methodInfo == null) throw new AssertionError(methodName);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Type GetEncoderType() => typeof(T);

    #region Write

    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, SerializeFeatures features) {
        Type encoderType = GetEncoderType();
        writer.WriteStartObject(encoderType, features);
        writer.WriteHeader(encoderType, declaredType);
        //
        if ((_overrides & MASK_BEFORE_ENCODE) != 0 && writer.Options.enableBeforeEncode) {
            BeforeEncode(writer, ref inst);
        }
        if ((_overrides & MASK_WRITE_OBJECT) != 0) {
            WriteObject(writer, ref inst); // 似乎WriteObject也可以充当BeforeEncode呢...
        }
        WriteFields(writer, ref inst);
        writer.WriteEndObject();
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
    public T ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features) {
        T inst = NewInstance(reader);
        reader.ReadStartObject(GetEncoderType());
        //
        if ((_overrides & MASK_READ_OBJECT) != 0) {
            ReadObject(reader, ref inst);
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string name = reader.ReadName();
            if (!ReadField(reader, ref inst, name)) {
                reader.SkipValue();
            }
        }
        if ((_overrides & MASK_AFTER_DECODE) != 0 && reader.Options.enableAfterDecode) {
            reader.DeferInvokeAfterDecode(this, inst);
        }
        reader.ReadEndObject();
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
    /// </summary>
    [StableName]
    protected virtual void ReadObject(IDsonObjectReader reader, ref T inst) {
    }

    /// <summary>
    /// 读取单个字段
    /// 1.新版限定为必须通过Switch-Case解码，以简化其它设计。
    /// 2.返回值用于判断超类是否成功读取了字段。
    /// </summary>
    [StableName]
    protected abstract bool ReadField(IDsonObjectReader reader, ref T inst, string name);

    /// <summary>
    /// 设置字段的值
    /// 注：除string/bytes以外的引用类型通常都需要在该方法中处理。
    /// </summary>
    [StableName]
    public virtual bool SetField(T inst, string name, object value) {
        return false;
    }

    /// <summary>
    /// 调用用户的AfterDecode方法
    /// </summary>
    [StableName]
    public virtual void AfterDecode(IDsonObjectReader reader, T inst) {
    }

    #endregion
}
}