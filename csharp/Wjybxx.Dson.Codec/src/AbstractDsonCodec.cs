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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
internal static class AbstractDsonCodec
{
    private static readonly ConcurrentDictionary<Type, bool> cache = new ConcurrentDictionary<Type, bool>();

    public static bool IsOverwriteBeforeEncode(Type type) {
        if (type.IsGenericType) {
            type = type.GetGenericTypeDefinition();
        }
        if (cache.TryGetValue(type, out bool r)) {
            return r;
        }
        MethodInfo methodInfo = type.GetMethod("BeforeEncode", BindingFlags.NonPublic | BindingFlags.Instance);
        if (methodInfo == null) throw new AssertionError();

        Type declaringType = methodInfo.DeclaringType!;
        if (declaringType.IsGenericType) {
            declaringType = declaringType.GetGenericTypeDefinition();
        }
        r = declaringType != typeof(AbstractDsonCodec<>);
        cache.TryAdd(type, r);
        return r;
    }
}

/// <summary>
/// 生成代码默认都会实现该类
/// (建议手写代码也继承该类)
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AbstractDsonCodec<T> : IDsonCodec<T>
{
    private readonly bool _isOverwriteBeforeEncode;

    protected AbstractDsonCodec() {
        _isOverwriteBeforeEncode = AbstractDsonCodec.IsOverwriteBeforeEncode(GetType());
    }
    //

    [StableName]
    public virtual Type GetEncoderType() => typeof(T);

    public virtual bool AutoStartEnd => true;

    public virtual bool IsWriteAsArray => DsonConverterUtils.IsEncodeAsArray(GetEncoderType());

    #region Write

    public void WriteObject(IDsonObjectWriter writer, in T inst, Type declaredType, ObjectStyle style) {
        if (_isOverwriteBeforeEncode && writer.Options.enableBeforeEncode) {
            T copiedInst = inst;
            BeforeEncode(writer, ref copiedInst);
            WriteFields(writer, in copiedInst);
        } else {
            WriteFields(writer, in inst);
        }
    }

    // 结构体可能也有序列化前钩子
    [StableName]
    protected virtual void BeforeEncode(IDsonObjectWriter writer, ref T inst) {
    }

    [StableName]
    protected abstract void WriteFields(IDsonObjectWriter writer, in T inst);

    #endregion

    #region Read

    [StableName]
    public T ReadObject(IDsonObjectReader reader, Func<object>? factory = null) {
        // cast失败则抛出异常，不能测试类型导致隐藏错误
        T inst = factory != null ? (T)factory() : NewInstance(reader);
        ReadFields(reader, ref inst);
        if (reader.Options.enableAfterDecode) {
            AfterDecode(reader, ref inst);
        }
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
    /// 读取字段到指定实例（可以是子类实例）
    /// 需要使用ref，否则结构体会产生拷贝，导致无法读取到指定实例上。
    /// </summary>
    [StableName]
    protected abstract void ReadFields(IDsonObjectReader reader, ref T inst);

    /// <summary>
    /// 解码后调用
    /// 需要使用ref，否则结构体会产生拷贝，导致无法读取到指定实例上。
    /// </summary>
    [StableName]
    protected virtual void AfterDecode(IDsonObjectReader reader, ref T inst) {
    }

    #endregion
}
}