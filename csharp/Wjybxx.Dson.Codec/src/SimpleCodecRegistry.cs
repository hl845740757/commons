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

using System;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// </summary>
public sealed class SimpleCodecRegistry : IDsonCodecRegistry
{
    private readonly IDictionary<Type, DsonCodecImpl> encoderDic;
    private readonly IDictionary<Type, DsonCodecImpl> decoderDic;

    public SimpleCodecRegistry() {
        encoderDic = new Dictionary<Type, DsonCodecImpl>();
        decoderDic = new Dictionary<Type, DsonCodecImpl>();
    }

    private SimpleCodecRegistry(IDictionary<Type, DsonCodecImpl> encoderDic, IDictionary<Type, DsonCodecImpl> decoderDic) {
        this.encoderDic = encoderDic.ToImmutableLinkedDictionary();
        this.decoderDic = decoderDic.ToImmutableLinkedDictionary(); // 避免系统库依赖，无法引入Unity
    }

    /** 根据codecs创建一个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry FromCodecs(IEnumerable<IDsonCodec> codecs) {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        foreach (IDsonCodec codec in codecs) {
            result.AddCodec(codec);
        }
        return result.ToImmutable();
    }

    /** 转换为不可变实例 */
    public SimpleCodecRegistry ToImmutable() {
        return new SimpleCodecRegistry(encoderDic, decoderDic);
    }

    /** 清理数据 */
    public void Clear() {
        encoderDic.Clear();
        decoderDic.Clear();
    }

    /** 合并配置 */
    public SimpleCodecRegistry MergeFrom(SimpleCodecRegistry other) {
        foreach (KeyValuePair<Type, DsonCodecImpl> pair in other.encoderDic) {
            encoderDic[pair.Key] = pair.Value;
        }
        foreach (KeyValuePair<Type, DsonCodecImpl> pair in other.decoderDic) {
            decoderDic[pair.Key] = pair.Value;
        }
        return this;
    }

    /** 配置编解码器 */
    public SimpleCodecRegistry AddCodec(IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        Type clazz = codecImpl.GetEncoderType();
        encoderDic[clazz] = codecImpl;
        decoderDic[clazz] = codecImpl;
        return this;
    }

    /**
     * 配置编解码器
     * 适用超类Codec的默认解码实例可赋值给当前类型的情况，eg：IntList => IntCollectionCodec。
     */
    public SimpleCodecRegistry AddCodec(Type clazz, IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        encoderDic[clazz] = codecImpl;
        decoderDic[clazz] = codecImpl;
        return this;
    }

    /** 配置编码器 */
    public SimpleCodecRegistry AddEncoder(Type clazz, IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        encoderDic[clazz] = codecImpl;
        return this;
    }

    /** 配置解码器 */
    public SimpleCodecRegistry AddDecoder(Type clazz, IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        decoderDic[clazz] = codecImpl;
        return this;
    }

    public DsonCodecImpl? GetEncoder(Type typeInfo) {
        encoderDic.TryGetValue(typeInfo, out DsonCodecImpl codecImpl);
        return codecImpl;
    }

    public DsonCodecImpl? GetDecoder(Type typeInfo) {
        decoderDic.TryGetValue(typeInfo, out DsonCodecImpl codecImpl);
        return codecImpl;
    }
}
}