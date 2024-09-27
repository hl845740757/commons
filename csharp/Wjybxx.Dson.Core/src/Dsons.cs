#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Ext;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson工具类
/// (C#是真泛型，因此只需一个工具类)
/// </summary>
public static class Dsons
{
    #region 基础常量

    /** {@link DsonType}占用的比特位 */
    public const int DsonTypeBites = 5;
    /** {@link DsonType}的最大类型编号 */
    public const int DsonTypeMaxValue = 31;

    /** {@link WireType}占位的比特位数 */
    public const int WireTypeBits = 3;
    public const int WireTypeMask = (1 << WireTypeBits) - 1;
    /** wireType看做数值时的最大值 */
    public const int WireTypeMaxValue = 7;

    /** 完整类型信息占用的比特位数 */
    public const int FullTypeBits = DsonTypeBites + WireTypeBits;
    public const int FullTypeMask = (1 << FullTypeBits) - 1;

    /** 二进制数据的最大长度 */
    public const int MaxBinaryLength = int.MaxValue - 6;

    #endregion

    #region 二进制常量

    /** 继承深度占用的比特位 */
    private const int IdepBits = 3;
    private const int IdepMask = (1 << IdepBits) - 1;
    /**
     * 支持的最大继承深度 - 7
     * 1.idep的深度不包含Object，没有显式继承其它类的类，idep为0
     * 2.超过7层我认为是你的代码有问题，而不是框架问题
     */
    public const int IdepMaxValue = IdepMask;

    /** 类字段最大number */
    private const short LnumberMaxValue = 8191;
    /** 类字段占用的最大比特位数 - 暂不对外开放 */
    private const int LnumberMaxBits = 13;

    #endregion

    #region Other

    /// <summary>
    /// 池化字段名；避免创建大量相同内容的字符串，有一定的查找开销，但对内存友好
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string InternField(string fieldName) {
        // 长度异常的数据不池化
        return fieldName.Length <= 32 ? string.Intern(fieldName) : fieldName;
    }

    /** 检查具备类型标签的数据的子类型是否合法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckSubType(int type) {
        if (type < 0) {
            throw new ArgumentException("type cant be negative");
        }
    }

    /** 检查二进制数据的长度是否合法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBinaryLength(int length) {
        if (length < 0 || length > MaxBinaryLength) {
            throw new ArgumentException($"the length of data must between[0, {MaxBinaryLength}], but found: {length}");
        }
    }

    /** 检查hasValue标记和值是否匹配 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckHasValue(int value, bool hasVal) {
        if (!hasVal && value != 0) {
            throw new ArgumentException();
        }
    }

    /** 检查hasValue标记和值是否匹配 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckHasValue(long value, bool hasVal) {
        if (!hasVal && value != 0) {
            throw new ArgumentException();
        }
    }

    /** 检查hasValue标记和值是否匹配 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckHasValue(double value, bool hasVal) {
        if (!hasVal && value != 0) {
            throw new ArgumentException();
        }
    }

    #endregion

    #region FullType

    /// <summary>
    /// 计算FullType     
    /// </summary>
    /// <param name="dsonType">Dson数据类型</param>
    /// <param name="wireType">特殊编码类型</param>
    /// <returns>完整类型</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullType(DsonType dsonType, WireType wireType) {
        return ((int)dsonType << WireTypeBits) | (int)wireType;
    }

    /// <summary>
    /// 用于非常规类型计算FullType
    /// </summary>
    /// <param name="dsonType">Dson数据类型 5bits[0~31]</param>
    /// <param name="wireType">特殊编码类型 3bits[0~7]</param>
    /// <returns>完整类型</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullType(int dsonType, int wireType) {
        return (dsonType << WireTypeBits) | wireType;
    }

    /// <summary>
    /// 通过FullType计算
    /// </summary>
    /// <param name="fullType"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DsonTypeOfFullType(int fullType) {
        return MathCommon.LogicalShiftRight(fullType, WireTypeBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WireTypeOfFullType(int fullType) {
        return (fullType & WireTypeMask);
    }

    #endregion

    #region FieldNumber

    /// <summary>
    /// 计算字段的完整数字
    /// </summary>
    /// <param name="idep">继承深度[0~7]</param>
    /// <param name="lnumber">字段在类本地的编号</param>
    /// <returns>字段的完整编号</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullNumber(int idep, int lnumber) {
        return (lnumber << IdepBits) | idep;
    }

    /// <summary>
    /// 通过字段完整编号计算本地编号
    /// </summary>
    /// <param name="fullNumber"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LnumberOfFullNumber(int fullNumber) {
        return MathCommon.LogicalShiftRight(fullNumber, IdepBits);
    }

    /// <summary>
    /// 通过字段完整编号计算继承深度
    /// </summary>
    /// <param name="fullNumber"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte IdepOfFullNumber(int fullNumber) {
        return (byte)(fullNumber & IdepMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeFullNumberZeroIdep(int lnumber) {
        return lnumber << IdepBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long MakeClassGuid(int ns, int classId) {
        return ((long)ns << 32) | (classId & 0xFFFF_FFFFL);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NamespaceOfClassGuid(long guid) {
        return (int)MathCommon.LogicalShiftRight(guid, 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LclassIdOfClassGuid(long guid) {
        return (int)guid;
    }

    /// <summary>
    /// 计算一个类的继承深度
    /// </summary>
    /// <param name="clazz"></param>
    /// <returns></returns>
    public static int CalIdep(Type clazz) {
        if (clazz.IsInterface || clazz.IsPrimitive) {
            throw new ArgumentException();
        }
        if (clazz == typeof(object)) {
            return 0;
        }
        int r = -1; // 去除Object；简单说：Object和Object的直接子类的idep都记为0，这很有意义。
        while ((clazz = clazz.BaseType) != null) {
            r++;
        }
        return r;
    }

    #endregion

    #region Read/Write

    /**
     * 读取顶层集合
     * 会将独立的header合并到容器中，会将分散的元素读取存入数组
     */
    public static DsonArray<TName> ReadCollection<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonArray<TName> collection = new DsonArray<TName>(4);
        DsonType dsonType;
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            if (dsonType == DsonType.Header) {
                ReadHeader(reader, collection.Header);
            } else if (dsonType == DsonType.Object) {
                collection.Add(ReadObject(reader));
            } else if (dsonType == DsonType.Array) {
                collection.Add(ReadArray(reader));
            } else {
                throw DsonIOException.InvalidTopDsonType(dsonType);
            }
        }
        return collection;
    }

    /**
     * 写入顶层集合
     * 顶层容器的header和元素将被展开，而不是嵌套在数组中
     */
    public static void WriteCollection<TName>(IDsonWriter<TName> writer,
                                              DsonArray<TName> collection) where TName : IEquatable<TName> {
        if (collection.Header.Count > 0) {
            WriteHeader(writer, collection.Header);
        }
        foreach (DsonValue dsonValue in collection) {
            if (dsonValue.DsonType == DsonType.Object) {
                WriteObject(writer, dsonValue.AsObject<TName>());
            } else if (dsonValue.DsonType == DsonType.Array) {
                WriteArray(writer, dsonValue.AsArray<TName>());
            } else {
                throw DsonIOException.InvalidTopDsonType(dsonValue.DsonType);
            }
        }
    }


    /// <summary>
    /// 写入一个顶层值
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="dsonValue">顶层对象，可以是Header</param>
    /// <param name="style">文本格式</param>
    /// <typeparam name="TName"></typeparam>
    public static void WriteTopDsonValue<TName>(IDsonWriter<TName> writer, DsonValue dsonValue,
                                                ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        if (dsonValue.DsonType == DsonType.Object) {
            WriteObject(writer, dsonValue.AsObject<TName>(), style);
        } else if (dsonValue.DsonType == DsonType.Array) {
            WriteArray(writer, dsonValue.AsArray<TName>(), style);
        } else if (dsonValue.DsonType == DsonType.Header) {
            WriteHeader(writer, dsonValue.AsHeader<TName>());
        } else {
            throw DsonIOException.InvalidTopDsonType(dsonValue.DsonType);
        }
    }

    /// <summary>
    /// 读取一个顶层对象值
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="fileHeader">用于接收文件头信息;如果读取到header，则存储给定参数中，并返回给定对象</param>
    /// <typeparam name="TName"></typeparam>
    /// <returns>如果到达文件尾部，则返回null</returns>
    public static DsonValue? ReadTopDsonValue<TName>(IDsonReader<TName> reader,
                                                     DsonHeader<TName>? fileHeader = null) where TName : IEquatable<TName> {
        DsonType dsonType = reader.ReadDsonType();
        if (dsonType == DsonType.EndOfObject) {
            return null;
        }
        if (dsonType == DsonType.Object) {
            return ReadObject(reader);
        } else if (dsonType == DsonType.Array) {
            return ReadArray(reader);
        } else if (dsonType == DsonType.Header) {
            return ReadHeader(reader, fileHeader);
        }
        throw DsonIOException.InvalidTopDsonType(dsonType);
    }

    /** 如果需要写入名字，外部写入 */
    public static void WriteObject<TName>(IDsonWriter<TName> writer, DsonObject<TName> dsonObject,
                                          ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        writer.WriteStartObject(style);
        if (dsonObject.Header.Count > 0) {
            WriteHeader(writer, dsonObject.Header);
        }
        foreach (var pair in dsonObject) {
            WriteDsonValue(writer, pair.Value, pair.Key);
        }
        writer.WriteEndObject();
    }

    public static DsonObject<TName> ReadObject<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonObject<TName> dsonObject = new DsonObject<TName>();
        DsonType dsonType;
        TName name;
        DsonValue value;
        reader.ReadStartObject();
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            if (dsonType == DsonType.Header) {
                ReadHeader(reader, dsonObject.Header);
            } else {
                name = reader.ReadName();
                value = ReadDsonValue(reader);
                dsonObject[name] = value;
            }
        }
        reader.ReadEndObject();
        return dsonObject;
    }

    /** 如果需要写入名字，外部写入 */
    public static void WriteArray<TName>(IDsonWriter<TName> writer, DsonArray<TName> dsonArray,
                                         ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        writer.WriteStartArray(style);
        if (dsonArray.Header.Count > 0) {
            WriteHeader(writer, dsonArray.Header);
        }
        foreach (DsonValue dsonValue in dsonArray) {
            WriteDsonValue(writer, dsonValue, default);
        }
        writer.WriteEndArray();
    }

    public static DsonArray<TName> ReadArray<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonArray<TName> dsonArray = new DsonArray<TName>();
        DsonType dsonType;
        DsonValue value;
        reader.ReadStartArray();
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            if (dsonType == DsonType.Header) {
                ReadHeader(reader, dsonArray.Header);
            } else {
                value = ReadDsonValue(reader);
                dsonArray.Add(value);
            }
        }
        reader.ReadEndArray();
        return dsonArray;
    }

    public static void WriteHeader<TName>(IDsonWriter<TName> writer, DsonHeader<TName> header) where TName : IEquatable<TName> {
        if (header.Count == 1 && typeof(TName) == typeof(string)) {
            if (header.AsHeader<string>().TryGetValue(DsonHeaders.Names_ClassName, out DsonValue clsName)) {
                writer.WriteSimpleHeader(clsName.AsString()); // header只包含clsName时打印为简单模式
                return;
            }
        }
        writer.WriteStartHeader();
        foreach (var pair in header) {
            WriteDsonValue(writer, pair.Value, pair.Key);
        }
        writer.WriteEndHeader();
    }

    public static DsonHeader<TName> ReadHeader<TName>(IDsonReader<TName> reader, DsonHeader<TName>? header) where TName : IEquatable<TName> {
        if (header == null) header = new DsonHeader<TName>();
        DsonType dsonType;
        TName name;
        DsonValue value;
        reader.ReadStartHeader();
        while ((dsonType = reader.ReadDsonType()) != DsonType.EndOfObject) {
            Debug.Assert(dsonType != DsonType.Header);
            name = reader.ReadName();
            value = ReadDsonValue(reader);
            header[name] = value;
        }
        reader.ReadEndHeader();
        return header;
    }

    public static void WriteDsonValue<TName>(IDsonWriter<TName> writer, DsonValue dsonValue, TName? name) where TName : IEquatable<TName> {
        if (writer.IsAtName) {
            writer.WriteName(name);
        }
        switch (dsonValue.DsonType) {
            case DsonType.Int32:
                writer.WriteInt32(name, dsonValue.AsInt32(), WireType.VarInt, NumberStyles.Typed); // 必须能精确反序列化
                break;
            case DsonType.Int64:
                writer.WriteInt64(name, dsonValue.AsInt64(), WireType.VarInt, NumberStyles.Typed);
                break;
            case DsonType.Float:
                writer.WriteFloat(name, dsonValue.AsFloat(), NumberStyles.Typed);
                break;
            case DsonType.Double:
                writer.WriteDouble(name, dsonValue.AsDouble(), NumberStyles.Simple);
                break;
            case DsonType.Bool:
                writer.WriteBool(name, dsonValue.AsBool());
                break;
            case DsonType.String:
                writer.WriteString(name, dsonValue.AsString());
                break;
            case DsonType.Null:
                writer.WriteNull(name);
                break;
            case DsonType.Binary:
                writer.WriteBinary(name, dsonValue.AsBinary());
                break;
            case DsonType.Pointer:
                writer.WritePtr(name, dsonValue.AsPointer());
                break;
            case DsonType.LitePointer:
                writer.WriteLitePtr(name, dsonValue.AsLitePointer());
                break;
            case DsonType.DateTime:
                writer.WriteDateTime(name, dsonValue.AsDateTime());
                break;
            case DsonType.Timestamp: {
                writer.WriteTimestamp(name, dsonValue.AsTimestamp());
                break;
            }
            case DsonType.Header:
                WriteHeader(writer, dsonValue.AsHeader<TName>());
                break;
            case DsonType.Array:
                WriteArray(writer, dsonValue.AsArray<TName>(), ObjectStyle.Indent);
                break;
            case DsonType.Object:
                WriteObject(writer, dsonValue.AsObject<TName>(), ObjectStyle.Indent);
                break;
            case DsonType.EndOfObject:
            default:
                throw new InvalidOperationException();
        }
    }

    public static DsonValue ReadDsonValue<TName>(IDsonReader<TName> reader) where TName : IEquatable<TName> {
        DsonType dsonType = reader.CurrentDsonType;
        reader.SkipName();
        TName name = default;
        switch (dsonType) {
            case DsonType.Int32: return new DsonInt32(reader.ReadInt32(name));
            case DsonType.Int64: return new DsonInt64(reader.ReadInt64(name));
            case DsonType.Float: return new DsonFloat(reader.ReadFloat(name));
            case DsonType.Double: return new DsonDouble(reader.ReadDouble(name));
            case DsonType.Bool: return new DsonBool(reader.ReadBool(name));
            case DsonType.String: return new DsonString(reader.ReadString(name));
            case DsonType.Null: {
                reader.ReadNull(name);
                return DsonNull.NULL;
            }
            case DsonType.Binary: return new DsonBinary(reader.ReadBinary(name));
            case DsonType.Pointer: return new DsonPointer(reader.ReadPtr(name));
            case DsonType.LitePointer: return new DsonLitePointer(reader.ReadLitePtr(name));
            case DsonType.DateTime: return new DsonDateTime(reader.ReadDateTime(name));
            case DsonType.Timestamp: return new DsonTimestamp(reader.ReadTimestamp(name));
            case DsonType.Header: {
                DsonHeader<TName> header = new DsonHeader<TName>();
                ReadHeader(reader, header);
                return header;
            }
            case DsonType.Object: return ReadObject(reader);
            case DsonType.Array: return ReadArray(reader);
            case DsonType.EndOfObject:
            default: throw new InvalidOperationException();
        }
    }

    #endregion

    #region Copy

    /** 深度拷贝为可变对象 */
    public static DsonValue MutableDeepCopy<TName>(DsonValue dsonValue) {
        return MutableDeepCopy<TName>(dsonValue, 0);
    }

    /**
     * 深度拷贝为可变对象
     *
     * @param stack 当前栈深度
     */
    private static DsonValue MutableDeepCopy<TName>(DsonValue dsonValue, int stack) {
        if (stack > 100) throw new InvalidOperationException("Check for circular references");
        switch (dsonValue.DsonType) {
            case DsonType.Object: {
                DsonObject<TName> src = dsonValue.AsObject<TName>();
                DsonObject<TName> result = new DsonObject<TName>(src.Count);
                CopyKvPair(src.Header, result.Header, stack);
                CopyKvPair(src, result, stack);
                return result;
            }
            case DsonType.Array: {
                DsonArray<TName> src = dsonValue.AsArray<TName>();
                DsonArray<TName> result = new DsonArray<TName>(src.Count);
                CopyKvPair(src.Header, result.Header, stack);
                CopyElements<TName>(src, result, stack);
                return result;
            }
            case DsonType.Header: {
                DsonHeader<TName> src = dsonValue.AsHeader<TName>();
                DsonHeader<TName> result = new DsonHeader<TName>();
                CopyKvPair(src, result, stack);
                return result;
            }
            case DsonType.Binary: {
                return new DsonBinary(dsonValue.AsBinary().DeepCopy());
            }
            default: {
                return dsonValue;
            }
        }
    }

    private static void CopyKvPair<TName>(AbstractDsonObject<TName> src, AbstractDsonObject<TName> dest, int stack) {
        if (src.Count > 0) {
            foreach (KeyValuePair<TName, DsonValue> pair in src) {
                dest[pair.Key] = MutableDeepCopy<TName>(pair.Value, stack + 1);
            }
        }
    }

    private static void CopyElements<TName>(AbstractDsonArray src, AbstractDsonArray dest, int stack) {
        if (src.Count > 0) {
            foreach (DsonValue dsonValue in src) {
                dest.Add(MutableDeepCopy<TName>(dsonValue, stack + 1));
            }
        }
    }

    #endregion

    #region 快捷方法

    /** 该接口用于写顶层数组容器，所有元素将被展开 */
    public static string ToCollectionDson(this DsonArray<string> collection, DsonTextWriterSettings? settings = null) {
        if (settings == null) settings = DsonTextWriterSettings.Default;

        StringBuilder sb = ConcurrentObjectPool.SharedStringBuilderPool.Acquire();
        try {
            using (DsonTextWriter writer = new DsonTextWriter(settings, new StringWriter(sb))) {
                WriteCollection(writer, collection);
            }
            return sb.ToString();
        }
        finally {
            ConcurrentObjectPool.SharedStringBuilderPool.Release(sb);
        }
    }

    /** 该接口用于读取多顶层对象dson文本 */
    public static DsonArray<string> FromCollectionDson(string dsonString) {
        using (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, dsonString)) {
            return ReadCollection(reader);
        }
    }

    public static string ToDson(this DsonValue dsonValue, ObjectStyle style = ObjectStyle.Indent) {
        return ToDson(dsonValue, style, DsonTextWriterSettings.Default);
    }

    public static string ToDson(this DsonValue dsonValue, ObjectStyle style, DsonTextWriterSettings settings) {
        if (!dsonValue.DsonType.IsContainerOrHeader()) {
            throw new InvalidOperationException("invalid dsonType " + dsonValue.DsonType);
        }
        StringBuilder sb = ConcurrentObjectPool.SharedStringBuilderPool.Acquire();
        try {
            using (DsonTextWriter writer = new DsonTextWriter(settings, new StringWriter(sb))) {
                WriteTopDsonValue(writer, dsonValue, style);
            }
            return sb.ToString();
        }
        finally {
            ConcurrentObjectPool.SharedStringBuilderPool.Release(sb);
        }
    }

    /** 默认只读取第一个值 */
    public static DsonValue FromDson(string dsonString) {
        using DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, dsonString);
        return ReadTopDsonValue(reader)!;
    }

    /// <summary>
    /// 将原始Dson字符串按照给定投影信息进行投影
    /// </summary>
    /// <param name="dsonString">原始的dson字符串</param>
    /// <param name="projectInfo">投影描述</param>
    /// <returns>如果存在可映射对象则返回对应值</returns>
    public static DsonValue? Project(string dsonString, string projectInfo) {
        return new Projection(projectInfo).Project(dsonString);
    }

    /// <summary>
    /// 将原始Dson字符串按照给定投影信息进行投影
    /// </summary>
    /// <param name="dsonString">原始的dson字符串</param>
    /// <param name="projectInfo">投影描述</param>
    /// <returns>如果存在可映射对象则返回对应值</returns>
    public static DsonValue? Project(string dsonString, DsonObject<string> projectInfo) {
        return new Projection(projectInfo).Project(dsonString);
    }

    /** 获取dsonValue的clsName -- dson的约定之一 */
    public static string? GetClassName(DsonValue dsonValue) {
        DsonHeader<string> header;
        if (dsonValue is DsonObject<string> dsonObject) {
            header = dsonObject.Header;
        } else if (dsonValue is DsonArray<string> dsonArray) {
            header = dsonArray.Header;
        } else {
            return null;
        }
        if (header.TryGetValue(DsonHeaders.Names_ClassName, out DsonValue wrapped)) {
            return wrapped is DsonString dsonString ? dsonString.Value : null;
        }
        return null;
    }

    /** 获取dsonValue的localId -- dson的约定之一 */
    public static string? GetLocalId(DsonValue dsonValue) {
        DsonHeader<string> header;
        if (dsonValue is DsonObject<string> dsonObject) {
            header = dsonObject.Header;
        } else if (dsonValue is DsonArray<string> dsonArray) {
            header = dsonArray.Header;
        } else {
            return null;
        }
        if (header.TryGetValue(DsonHeaders.Names_LocalId, out DsonValue wrapped)) {
            return wrapped is DsonString dsonString ? dsonString.Value : null;
        }
        return null;
    }

    #endregion

    #region 工厂方法

    public static DsonScanner NewStringScanner(string dsonString) {
        return new DsonScanner(IDsonCharStream.NewCharStream(dsonString));
    }

    public static DsonScanner NewStreamScanner(TextReader reader, bool autoClose = true) {
        return new DsonScanner(IDsonCharStream.NewBufferedCharStream(reader, autoClose));
    }

    #endregion
}
}