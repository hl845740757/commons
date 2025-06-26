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
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Types;
using ClassName = Wjybxx.Commons.Poet.ClassName;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.Dson.Apt2
{
/// <summary>
/// 为普通对象生成Codec
/// </summary>
internal class PojoCodecGenerator
{
    private readonly CodecProcessor processor;
    private readonly Context context;

#nullable disable
    private Type typeElement;
    private TypeSpec.Builder typeBuilder;
    private List<MemberInfo> allFieldsAndMethodWithInherit;

    private ClassName rawTypeName;
    private bool containsReaderConstructor;
    private bool containsNewInstanceMethod;
    private bool containsReadObjectMethod;
    private bool containsWriteObjectMethod;
    private (bool contains, int argCount) containsBeforeEncodeMethod;
    private (bool contains, int argCount) containsAfterDecodeMethod;

    private MethodSpec.Builder newInstanceMethodBuilder;
    private MethodSpec.Builder readFieldsMethodBuilder;
    private MethodSpec.Builder afterDecodeMethodBuilder;
    private MethodSpec.Builder beforeEncodeMethodBuilder;
    private MethodSpec.Builder writeFieldsMethodBuilder;

    public PojoCodecGenerator(CodecProcessor processor, Context context) {
        this.processor = processor;
        this.context = context;

        this.typeElement = context.type;
        this.typeBuilder = context.typeBuilder;
        this.allFieldsAndMethodWithInherit = context.allMembers;
    }
#nullable enable

    public void Execute() {
        Init();
        Gen();
    }

    private void Init() {
        rawTypeName = ClassName.Get(typeElement);
        containsReaderConstructor = processor.ContainsReaderConstructor(typeElement);
        containsNewInstanceMethod = processor.ContainsNewInstanceMethod(typeElement);
        containsReadObjectMethod = processor.ContainsReadObjectMethod(allFieldsAndMethodWithInherit);
        containsWriteObjectMethod = processor.ContainsWriteObjectMethod(allFieldsAndMethodWithInherit);
        containsBeforeEncodeMethod = processor.ContainsBeforeEncodeMethod(allFieldsAndMethodWithInherit);
        containsAfterDecodeMethod = processor.ContainsAfterDecodeMethod(allFieldsAndMethodWithInherit);

        // 需要先初始化superDeclaredType
        Type superDeclaredType = context.superDeclaredType;
        newInstanceMethodBuilder = processor.NewNewInstanceMethodBuilder(superDeclaredType);
        readFieldsMethodBuilder = processor.NewReadFieldsMethodBuilder(superDeclaredType);
        afterDecodeMethodBuilder = processor.NewAfterDecodeMethodBuilder(superDeclaredType);
        beforeEncodeMethodBuilder = processor.NewBeforeEncodeMethodBuilder(superDeclaredType);
        writeFieldsMethodBuilder = processor.NewWriteFieldsMethodBuilder(superDeclaredType);
    }

    private void Gen() {
        AptClassProps aptClassProps = context.aptClassProps;
        GenNewInstanceMethod(aptClassProps);
        if (!aptClassProps.IsSingleton) {
            GenWriteObjectMethod(aptClassProps);
            GenReadObjectMethod(aptClassProps);
            // 普通字段读写
            foreach (AptFieldInfo? fieldInfo in context.serialFields) {
                AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
                if (processor.IsAutoWriteField(fieldInfo, aptClassProps, aptFieldProps)) {
                    AddWriteStatement(fieldInfo, aptFieldProps, aptClassProps);
                }
                if (processor.IsAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                    AddReadStatement(fieldInfo, aptFieldProps, aptClassProps);
                }
            }
        }
        // 控制方法生成顺序
        // GetEncoderType
        typeBuilder.AddMethod(processor.NewGetEncoderTypeMethod(context.superDeclaredType, rawTypeName));
        // BeforeEncode回调
        if (GenBeforeEncodeMethod(aptClassProps)) {
            typeBuilder.AddMethod(beforeEncodeMethodBuilder.Build());
        }
        typeBuilder.AddMethod(writeFieldsMethodBuilder.Build(true));
        typeBuilder.AddMethod(newInstanceMethodBuilder.Build())
            .AddMethod(readFieldsMethodBuilder.Build(true));
        // AfterDecode回调
        if (GenAfterDecodeMethod(aptClassProps)) {
            typeBuilder.AddMethod(afterDecodeMethodBuilder.Build());
        }
        // 额外注解
        if (context.additionalAnnotations != null) {
            typeBuilder.AddAttributes(context.additionalAnnotations);
        }
    }

    #region hook

    /** 调用用户的readObject方法 */
    private bool GenReadObjectMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyType != null) {
            if (aptClassProps.ContainsHookMethod(CodecProcessor.MNAME_READ_OBJECT)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(ref inst, reader)"
                    : "$T.$L(inst, reader)";
                // CodecProxy.ReadObject(inst, reader);
                readFieldsMethodBuilder.codeBuilder.AddStatement(format,
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_READ_OBJECT);
                return true;
            }
        } else {
            if (containsReadObjectMethod) {
                // inst.ReadObject(reader);
                readFieldsMethodBuilder.codeBuilder.AddStatement("inst.$L(reader)",
                    CodecProcessor.MNAME_READ_OBJECT);
                return true;
            }
        }
        return false;
    }

    /** 调用用户的writeObject方法 */
    private bool GenWriteObjectMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyType != null) {
            if (aptClassProps.ContainsHookMethod(CodecProcessor.MNAME_WRITE_OBJECT)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(in inst, writer)"
                    : "$T.$L(inst, writer)";
                // CodecProxy.WriteObject(inst, writer);
                writeFieldsMethodBuilder.codeBuilder.AddStatement(format,
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_WRITE_OBJECT);
                return true;
            }
        } else {
            if (containsWriteObjectMethod) {
                // inst.WriteObject(writer);
                writeFieldsMethodBuilder.codeBuilder.AddStatement("inst.$L(writer)",
                    CodecProcessor.MNAME_WRITE_OBJECT);
                return true;
            }
        }
        return false;
    }

    /** 调用用户BeforeEncode钩子方法 -- 需要支持codecProxy来处理 */
    private bool GenBeforeEncodeMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyType != null) {
            if (aptClassProps.ContainsHookMethod(CodecProcessor.MNAME_BEFORE_ENCODE)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(ref inst, writer.Options)"
                    : "$T.$L(inst, writer.Options)";
                // CodecProxy.BeforeEncode(inst, writer.Options);
                beforeEncodeMethodBuilder.codeBuilder.AddStatement(format,
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_BEFORE_ENCODE);
                return true;
            }
        } else {
            if (containsBeforeEncodeMethod.contains) {
                if (containsBeforeEncodeMethod.argCount > 0) {
                    // inst.BeforeEncode(writer.Options);
                    beforeEncodeMethodBuilder.codeBuilder.AddStatement("inst.$L(writer.Options)",
                        CodecProcessor.MNAME_BEFORE_ENCODE);
                } else {
                    // inst.BeforeEncode();
                    beforeEncodeMethodBuilder.codeBuilder.AddStatement("inst.$L()",
                        CodecProcessor.MNAME_BEFORE_ENCODE);
                }
                return true;
            }
        }
        return false;
    }

    /** 调用用户AfterDecode钩子方法 -- 需要支持CodecProxy来处理 */
    private bool GenAfterDecodeMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyType != null) {
            if (aptClassProps.ContainsHookMethod(CodecProcessor.MNAME_AFTER_DECODE)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(ref inst, reader.Options)"
                    : "$T.$L(inst, reader.Options)";
                // CodecProxy.AfterDecode(inst, reader.Options);
                afterDecodeMethodBuilder.codeBuilder.AddStatement(format,
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_AFTER_DECODE);
                return true;
            }
        } else {
            if (containsAfterDecodeMethod.contains) {
                if (containsAfterDecodeMethod.argCount > 0) {
                    // inst.AfterDecode(reader.Options);
                    afterDecodeMethodBuilder.codeBuilder.AddStatement("inst.$L(reader.Options)",
                        CodecProcessor.MNAME_AFTER_DECODE);
                } else {
                    // inst.AfterDecode();
                    afterDecodeMethodBuilder.codeBuilder.AddStatement("inst.$L()",
                        CodecProcessor.MNAME_AFTER_DECODE);
                }
                return true;
            }
        }
        return false;
    }

    /** 调用用户的NewInstance方法 */
    private void GenNewInstanceMethod(AptClassProps aptClassProps) {
        if (aptClassProps.IsSingleton) {
            // 有CodecProxy的情况下，单例也交由CodecProxy实现 -- 方法名是CodecProxy指定的，因此应当存在，不做校验
            Type? holder;
            TypeName holderTypeName;
            if (aptClassProps.codecProxyType != null) {
                holder = aptClassProps.codecProxyType;
                holderTypeName = aptClassProps.codecProxyClassName!;
            } else {
                holder = typeElement;
                holderTypeName = rawTypeName;
            }
            // c#还需要处理属性和方法的兼容...如果不存在对应的方法，则认为是属性
            string format = holder.GetFirstMethod(aptClassProps.singleton!) != null
                ? "return $T.$L()"
                : "return $T.$L";
            newInstanceMethodBuilder.codeBuilder.AddStatement(format,
                holderTypeName, aptClassProps.singleton!);
            return;
        }
        if (typeElement.IsAbstract) { // 抽象类
            newInstanceMethodBuilder.codeBuilder.AddStatement("throw new $T()", typeof(NotImplementedException));
            return;
        }

        if (aptClassProps.codecProxyType != null) {
            if (aptClassProps.ContainsHookMethod(CodecProcessor.MNAME_NEW_INSTANCE)) {
                // CodecProxy.NewInstance(reader);
                newInstanceMethodBuilder.codeBuilder.AddStatement("return $T.$L(reader)",
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_NEW_INSTANCE);
                return;
            }
        }
        if (containsNewInstanceMethod) { // 静态解析方法，优先级更高
            newInstanceMethodBuilder.codeBuilder.AddStatement("return $T.$L(reader)", rawTypeName,
                CodecProcessor.MNAME_NEW_INSTANCE);
        } else if (containsReaderConstructor) { // 解析构造方法
            newInstanceMethodBuilder.codeBuilder.AddStatement("return new $T(reader)", rawTypeName);
        } else if (typeElement.IsValueType) { // 值类型
            newInstanceMethodBuilder.codeBuilder.AddStatement("return default");
        } else {
            newInstanceMethodBuilder.codeBuilder.AddStatement("return new $T()", rawTypeName);
        }
    }

    #endregion

    #region field

    private void AddReadStatement(AptFieldInfo fieldInfo, AptFieldProps fieldProps, AptClassProps aptClassProps) {
        MethodSpec.Builder builder = readFieldsMethodBuilder;
        string fieldName = fieldInfo.Name;
        string? readProxy = fieldProps.readProxy;
        if (!string.IsNullOrWhiteSpace(readProxy)) { // 自定义读
            if (aptClassProps.codecProxyType != null) {
                // CodexProxy.ReadName(inst, reader, dsonName) 方法名是CodecProxy指定的，因此应当存在，不做校验
                builder.codeBuilder.AddStatement("$T.$L(inst, reader, $L)",
                    aptClassProps.codecProxyClassName, readProxy, SerialName(fieldName));
            } else {
                // inst.ReadName(reader, dsonName)
                builder.codeBuilder.AddStatement("inst.$L(reader, $L)",
                    readProxy, SerialName(fieldName));
            }
            return;
        }
        // 只有字段存在的情况下才读取
        builder.codeBuilder.Add("if (reader.ReadName($L)) ", SerialName(fieldName));

        string readMethodName = GetReadMethodName(fieldInfo);
        // 优先用setter，否则直接赋值 -- C#的属性和字段样式一致
        bool hasCustomSetter = !string.IsNullOrWhiteSpace(fieldProps.setter);
        string fieldAccess;
        if (hasCustomSetter || fieldInfo.HasPublicSetter) {
            fieldAccess = hasCustomSetter ? fieldProps.setter! : fieldInfo.propertyInfo!.Name;
        } else {
            fieldAccess = fieldName;
        }
        if (readMethodName == MNAME_READ_OBJECT) {
            TypeName fieldTypeName = fieldInfo.typeName!;
            // 读对象时要传入类型信息和Factory -- C#还要传泛型参数；name在前面已读，因此这里传入null
            // inst.name = reader.readObject<Type>(names_name, factories_name)
            string? toImmutableMethod;
            if (fieldProps.isImmutable && (toImmutableMethod = GetToImmutableMethodName(fieldInfo.FieldType)) != null) {
                // 需要动态引入Util类，因此不能使用扩展方法
                builder.codeBuilder.AddStatement("inst.$L = $T.$L(reader.$L<$T>(null, $L))",
                    fieldAccess,
                    CodecProcessor.typeName_CollectionUtil, toImmutableMethod,
                    readMethodName, fieldTypeName,
                    fieldProps.implTypeName == null ? "null" : SerialFactory(fieldName));
            } else {
                builder.codeBuilder.AddStatement("inst.$L = reader.$L<$T>(null, $L)",
                    fieldAccess, readMethodName, fieldTypeName,
                    fieldProps.implTypeName == null ? "null" : SerialFactory(fieldName));
            }
        } else {
            // inst.name = reader.readString(names_name)
            builder.codeBuilder.AddStatement("inst.$L = reader.$L(null)",
                fieldAccess, readMethodName);
        }
    }

    private string? GetToImmutableMethodName(Type fieldType) {
        if (fieldType.Name.StartsWith("Immutable")) return null; // 避免再套一层
        if (DsonConverterUtils.IsSet(fieldType)) return "ToImmutableSet2";
        if (DsonConverterUtils.IsList(fieldType)) return "ToImmutableList2";
        if (DsonConverterUtils.IsDictionary(fieldType)) return "ToImmutableDictionary2";
        return null;
    }

    private void AddWriteStatement(AptFieldInfo fieldInfo, AptFieldProps fieldProps, AptClassProps aptClassProps) {
        string fieldName = fieldInfo.Name;
        MethodSpec.Builder builder = this.writeFieldsMethodBuilder;
        if (!string.IsNullOrWhiteSpace(fieldProps.writeProxy)) { // 自定义写
            if (aptClassProps.codecProxyType != null) {
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                builder.codeBuilder.AddStatement("$T.$L(inst, writer, $L)",
                    aptClassProps.codecProxyClassName, fieldProps.writeProxy, SerialName(fieldName));
            } else {
                builder.codeBuilder.AddStatement("inst.$L(writer, $L)",
                    fieldProps.writeProxy, SerialName(fieldName));
            }
            return;
        }
        // 优先用getter，否则直接访问 -- C#的属性和字段样式一致
        string fieldAccess;
        bool hasCustomGetter = !string.IsNullOrWhiteSpace(fieldProps.getter);
        if (hasCustomGetter) {
            fieldAccess = fieldProps.getter!;
        } else if (fieldInfo.HasPublicGetter) {
            fieldAccess = fieldInfo.propertyInfo!.Name;
        } else {
            fieldAccess = fieldName;
        }

        // 处理数字 -- 涉及WireType和Style，注解使用的是枚举，我们转换为NumberStyles静态类
        string writeMethodName = GetWriteMethodName(fieldInfo);
        if (numberTypes.Contains(fieldInfo.FieldType)) {
            // int,long,float,double,uint,ulong,short,ushort,byte,sbyte...
            // writer.writeInt(names_fieldName, inst.field, NumberStyles.Simple)
            builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, $T.$L)",
                writeMethodName, SerialName(fieldName), fieldAccess,
                CodecProcessor.typeName_NumberStyles, fieldProps.numberStyle);
            return;
        }

        // 其它类型
        switch (writeMethodName) {
            case MNAME_WRITE_STRING: {
                // writer.writeString(names_fieldName, inst.getName(), StringStyle.AUTO)
                builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, $T.$L)",
                    writeMethodName, SerialName(fieldName), fieldAccess,
                    CodecProcessor.typeName_StringStyle, fieldProps.stringStyle);
                break;
            }
            case MNAME_WRITE_OBJECT: {
                // 写Object时传入类型信息和Style -- 会自动匹配泛型方法
                // writer.writeObject(names_fieldName, inst.getName(), ObjectStyle.INDENT)
                if (!string.IsNullOrWhiteSpace(fieldProps.objectStyle)) {
                    builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, $T.$L)",
                        writeMethodName, SerialName(fieldName), fieldAccess,
                        CodecProcessor.typeName_ObjectStyle, fieldProps.objectStyle);
                } else {
                    builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, null)",
                        writeMethodName, SerialName(fieldName), fieldAccess);
                }
                break;
            }
            default: {
                // 未对DateTime等结构体做in优化，因为通过属性访问时，无法使用in
                // writer.writeBytes(names_fieldName, inst.field)
                // writer.writeBool(names_fieldName, inst.getName())
                builder.codeBuilder.AddStatement("writer.$L($L, inst.$L)",
                    writeMethodName, SerialName(fieldName), fieldAccess);
                break;
            }
        }
    }


    private static string SerialName(string fieldName) {
        return SchemaGenerator.GetNameFieldName(fieldName);
    }

    private static string SerialFactory(string fieldName) {
        return SchemaGenerator.GetFactoryFieldName(fieldName);
    }

    /** 获取writer写字段的方法名 */
    private string GetWriteMethodName(AptFieldInfo fieldInfo) {
        Type fieldType = fieldInfo.FieldType;
        if (fieldType.IsPrimitive) {
            return primitiveWriteMethodNameMap[fieldType];
        }
        if (fieldType == typeof(string)) {
            return MNAME_WRITE_STRING;
        }
        if (fieldType == typeof(byte[])) {
            return MNAME_WRITE_BYTES;
        }
        if (fieldType == typeof(Binary)) {
            return MNAME_WRITE_BINARY;
        }
        if (fieldType == typeof(ObjectPtr)) {
            return MNAME_WRITE_PTR;
        }
        if (fieldType == typeof(ObjectLitePtr)) {
            return MNAME_WRITE_LITE_PTR;
        }
        if (fieldType == typeof(DateTime)) {
            return MNAME_WRITE_DATETIME;
        }
        if (fieldType == typeof(Timestamp)) {
            return MNAME_WRITE_TIMESTAMP;
        }
        return MNAME_WRITE_OBJECT;
    }

    /** 获取reader读字段的方法名 */
    private string GetReadMethodName(AptFieldInfo fieldInfo) {
        Type fieldType = fieldInfo.FieldType;
        if (fieldType.IsPrimitive) {
            return primitiveReadMethodNameMap[fieldType];
        }
        if (fieldType == typeof(string)) {
            return MNAME_READ_STRING;
        }
        if (fieldType == typeof(byte[])) {
            return MNAME_READ_BYTES;
        }
        if (fieldType == typeof(Binary)) {
            return MNAME_READ_BINARY;
        }
        if (fieldType == typeof(ObjectPtr)) {
            return MNAME_READ_PTR;
        }
        if (fieldType == typeof(ObjectLitePtr)) {
            return MNAME_READ_LITE_PTR;
        }
        if (fieldType == typeof(DateTime)) { // 系统库日期时间
            return MNAME_READ_DATETIME;
        }
        if (fieldType == typeof(Timestamp)) {
            return MNAME_READ_TIMESTAMP;
        }
        return MNAME_READ_OBJECT;
    }

    private const string MNAME_READ_STRING = "ReadString";
    private const string MNAME_READ_BYTES = "ReadBytes";
    private const string MNAME_READ_BINARY = "ReadBinary";
    private const string MNAME_READ_OBJECT = "ReadObject";

    private const string MNAME_READ_PTR = "ReadPtr";
    private const string MNAME_READ_LITE_PTR = "ReadLitePtr";
    private const string MNAME_READ_DATETIME = "ReadDateTime";
    private const string MNAME_READ_TIMESTAMP = "ReadTimestamp";

    private const string MNAME_WRITE_STRING = "WriteString";
    private const string MNAME_WRITE_BYTES = "WriteBytes";
    private const string MNAME_WRITE_BINARY = "WriteBinary";
    private const string MNAME_WRITE_OBJECT = "WriteObject";

    private const string MNAME_WRITE_PTR = "WritePtr";
    private const string MNAME_WRITE_LITE_PTR = "WriteLitePtr";
    private const string MNAME_WRITE_DATETIME = "WriteDateTime";
    private const string MNAME_WRITE_TIMESTAMP = "WriteTimestamp";

    private static readonly Dictionary<Type, string> primitiveReadMethodNameMap = new Dictionary<Type, string>(12);
    private static readonly Dictionary<Type, string> primitiveWriteMethodNameMap = new Dictionary<Type, string>(12);

    private static readonly HashSet<Type> numberTypes = new HashSet<Type>();

    static PojoCodecGenerator() {
        Dictionary<Type, string> type2KeywordDic = new Dictionary<Type, string>()
        {
            { typeof(int), "Int" },
            { typeof(long), "Long" },
            { typeof(float), "Float" },
            { typeof(double), "Double" },
            { typeof(bool), "Bool" },

            { typeof(uint), "UInt" },
            { typeof(ulong), "ULong" },
            { typeof(byte), "Byte" },
            { typeof(sbyte), "SByte" },
            { typeof(short), "Short" },
            { typeof(ushort), "UShort" },
            { typeof(char), "Char" },
        };
        foreach (KeyValuePair<Type, string> pair in type2KeywordDic) {
            primitiveReadMethodNameMap[pair.Key] = "Read" + pair.Value;
            primitiveWriteMethodNameMap[pair.Key] = "Write" + pair.Value;
        }

        numberTypes.AddAll(new[]
        {
            typeof(int),
            typeof(long),
            typeof(uint),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(short),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
        });
    }

    #endregion
}
}