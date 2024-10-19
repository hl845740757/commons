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
using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 生成代码默认都会实现该类
/// (建议手写代码也继承该类)
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AbstractDsonCodec<T> : IDsonCodec<T>
{
    [StableName]
    public virtual Type GetEncoderType() => typeof(T);

    public virtual bool AutoStartEnd => true;

    public virtual bool IsWriteAsArray => DsonConverterUtils.IsEncodeAsArray(GetEncoderType());

    #region Write

    public void WriteObject(IDsonObjectWriter writer, ref T inst, Type declaredType, ObjectStyle style) {
        if (writer.Options.enableBeforeEncode) {
            BeforeEncode(writer, ref inst);
        }
        WriteFields(writer, ref inst);
    }

    [StableName]
    protected virtual void BeforeEncode(IDsonObjectWriter writer, ref T inst) {
    }

    [StableName]
    protected abstract void WriteFields(IDsonObjectWriter writer, ref T inst);

    #endregion

    #region Read

    [StableName]
    public T ReadObject(IDsonObjectReader reader, Func<T>? factory = null) {
        T inst = factory != null ? factory() : NewInstance(reader);
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