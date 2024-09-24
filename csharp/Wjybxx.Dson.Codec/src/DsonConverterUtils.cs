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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Codecs;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// </summary>
public static class DsonConverterUtils
{
    #region 泛型

    /// <summary>
    /// 缓存泛型类型的泛型信息
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type[]> typeArgumentsCache = new ConcurrentDictionary<Type, Type[]>();

    /// <summary>
    /// 是否启用泛型参数缓存。
    /// 如果启用缓存，则会增加一定的查找开销；
    /// 如果不启用缓存，则可能创建大量的临时数组对象；
    /// 这个值在运行中通常并不需要修改，但我们暂先使用volatile。
    /// </summary>
    private static volatile bool enableCache = true;

    /// <summary>
    /// 启用缓存
    /// </summary>
    public static void EnableGenericCache() => enableCache = true;

    /// <summary>
    /// 禁用缓存
    /// </summary>
    public static void DisableGenericCache() => enableCache = false;

    /// <summary>
    /// 获取类型的泛型参数列表
    /// </summary>
    public static Type[] GetGenericArguments(Type type) {
        if (!type.IsGenericType || type.IsGenericTypeDefinition) {
            return Array.Empty<Type>();
        }
        if (enableCache) {
            if (!typeArgumentsCache.TryGetValue(type, out Type[] result)) {
                result = type.GenericTypeArguments;
                typeArgumentsCache.TryAdd(type, result);
            }
            return result;
        } else {
            return type.GenericTypeArguments;
        }
    }

    #endregion

    #region array

    /** 最大支持9阶 - 我都没见过3阶以上的数组... */
    private static readonly string[] arrayRankSymbols =
    {
        "[]",
        "[][]",
        "[][][]",
        "[][][][]",
        "[][][][][]",
        "[][][][][][]",
        "[][][][][][][]",
        "[][][][][][][][]",
        "[][][][][][][][][]"
    };

    /// <summary>
    /// 获取数组阶数对应的符号
    /// </summary>
    /// <param name="rank"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ArrayRankSymbol(int rank) {
        if (rank < 1 || rank > 9) {
            throw new ArgumentException("rank: " + rank);
        }
        return arrayRankSymbols[rank - 1];
    }

    /** 获取根元素的类型 -- 如果Type是数组，则返回最底层的元素类型；如果不是数组，则返回type */
    public static Type GetRootElementType(Type type) {
        while (type.IsArray) {
            type = type.GetElementType()!;
        }
        return type;
    }

    /** 获取数组的阶数 -- 如果不是数组，则返回0 */
    public static int GetArrayRank(Type type) {
        int r = 0;
        while (type.IsArray) {
            r++;
            type = type.GetElementType()!;
        }
        return r;
    }

    #endregion

    #region 其它

    /** 默认的类型元数据 */
    private static readonly ITypeMetaRegistry TYPE_META_REGISTRY = TypeMetaRegistries.FromMetas(BuiltinTypeMetas());
    private static readonly IDsonCodecRegistry CODEC_REGISTRY;

    private static TypeMeta TypeMetaOf(Type type, params string[] clsNames) {
        if (clsNames.Length == 0) {
            clsNames = new string[] { type.Name };
        }
        return TypeMeta.Of(type, ObjectStyle.Indent, clsNames);
    }

    private static List<TypeMeta> BuiltinTypeMetas() {
        return new List<TypeMeta>(40)
        {
            TypeMetaOf(typeof(int), DsonTexts.LabelInt32, "int", "int32"),
            TypeMetaOf(typeof(long), DsonTexts.LabelInt64, "long", "int64"),
            TypeMetaOf(typeof(float), DsonTexts.LabelFloat, "float"),
            TypeMetaOf(typeof(double), DsonTexts.LabelDouble, "double"),
            TypeMetaOf(typeof(bool), DsonTexts.LabelBool, "bool", "boolean"),
            TypeMetaOf(typeof(string), DsonTexts.LabelString, "string"),
            TypeMetaOf(typeof(Binary), DsonTexts.LabelBinary),
            TypeMetaOf(typeof(ObjectPtr), DsonTexts.LabelPtr),
            TypeMetaOf(typeof(ObjectLitePtr), DsonTexts.LabelLitePtr),
            TypeMetaOf(typeof(ExtDateTime), DsonTexts.LabelDateTime),
            TypeMetaOf(typeof(Timestamp), DsonTexts.LabelTimestamp),
            // uint
            TypeMetaOf(typeof(uint), DsonTexts.LabelUInt32, "uint", "uint32"),
            TypeMetaOf(typeof(ulong), DsonTexts.LabelUInt64, "ulong", "uint64"),
            // 特殊组件
            TypeMetaOf(typeof(DictionaryEncodeProxy<>), "DictionaryEncodeProxy", "MapEncodeProxy"),
            // c#的nullable支持，该类型信息是需要的，Codec是不需要的(走不到)
            TypeMetaOf(typeof(Nullable<>), "Nullable"),
        };
    }

    private static IList<IDsonCodec> BuiltinCodecs() {
        return new IDsonCodec[]
        {
            // dson内建结构
            new Int32Codec(),
            new Int64Codec(),
            new FloatCodec(),
            new DoubleCodec(),
            new BoolCodec(),
            new StringCodec(),
            new BinaryCodec(),
            new ObjectPtrCodec(),
            new ObjectLitePtrCodec(),
            new ExtDateTimeCodec(),
            new TimestampCodec(),
            // uint
            new UInt32Codec(),
            new UInt64Codec(),

            // 基本类型数组
            new MoreArrayCodecs.IntArrayCodec(),
            new MoreArrayCodecs.LongArrayCodec(),
            new MoreArrayCodecs.FloatArrayCodec(),
            new MoreArrayCodecs.DoubleArrayCodec(),
            new MoreArrayCodecs.BoolArrayCodec(),
            new MoreArrayCodecs.StringArrayCodec(),
            new MoreArrayCodecs.UIntArrayCodec(),
            new MoreArrayCodecs.ULongArrayCodec(),
            new MoreArrayCodecs.ObjectArrayCodec(),

            // 日期时间
            new DateTimeCodec(),
            new DateTimeOffsetCodec()
        }.ToImmutableList2();
    }

    /** 获取默认的类型元数据注册表 */
    public static ITypeMetaRegistry GetDefaultTypeMetaRegistry() {
        return TYPE_META_REGISTRY;
    }

    /** 获取默认的注册表 */
    public static IDsonCodecRegistry GetDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    /** 注意：默认情况下字典应该是一个数组对象，而不是普通的对象 */
    public static bool IsEncodeAsArray(Type encoderClass) {
        // c#不能直接测试是否是某个泛型原型的子类，好在字典也实现了IEnumerable，字典默认也需要编码为数组
        return encoderClass.IsArray || IsCollection(encoderClass, true);
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="ICollection{T}"/>类型
    /// </summary>
    /// <param name="type">要测试的类型</param>
    /// <param name="includeDictionary">是否包含字典类型</param>
    /// <returns></returns>
    public static bool IsCollection(Type type, bool includeDictionary = false) {
        Type target = type.GetInterface("ICollection`1");
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeof(ICollection<>);
        }
        return includeDictionary && IsDictionary(type);
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="ISet{T}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSet(Type type) {
        Type target = type.GetInterface("ISet`1");
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeof(ISet<>);
        }
        return false;
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="IGenericSet{T}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsGenericSet(Type type) {
        Type target = type.GetInterface("IGenericSet`1");
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target == typeof(IGenericSet<>);
        }
        return false;
    }

    /// <summary>
    /// 判断一个类型是否是<see cref="IDictionary{K,V}"/>类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsDictionary(Type type) {
        Type target = type.GetInterface("IDictionary`2");
        if (target != null) {
            if (!target.IsGenericTypeDefinition) target = target.GetGenericTypeDefinition();
            return target.GetGenericTypeDefinition() == typeof(IDictionary<,>);
        }
        return false;
    }

    #endregion

    static DsonConverterUtils() {
        {
            IList<IDsonCodec> builtinCodecs = BuiltinCodecs();
            List<DsonCodecImpl> codecImpls = new List<DsonCodecImpl>();
            foreach (IDsonCodec rawCodec in builtinCodecs) {
                Type genericType = typeof(DsonCodecImpl<>).MakeGenericType(rawCodec.GetEncoderClass());
                DsonCodecImpl codecImpl = (DsonCodecImpl)Activator.CreateInstance(genericType, rawCodec);
                codecImpls.Add(codecImpl);
            }
            CODEC_REGISTRY = DsonCodecRegistries.FromCodecs(codecImpls);
        }
    }
}
}