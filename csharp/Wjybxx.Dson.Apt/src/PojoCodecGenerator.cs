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
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Apt
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
    private bool containsBeforeEncodeMethod;
    private bool containsAfterDecodeMethod;

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
        this.allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
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
        if (!aptClassProps.IsSingleton()) {
            GenWriteObjectMethod(aptClassProps);
            GenReadObjectMethod(aptClassProps);
            // 普通字段读写
            foreach (FieldInfo fieldInfo in context.serialFields) {
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
        typeBuilder.AddMethod(processor.NewGetEncoderClassMethod(context.superDeclaredType, rawTypeName));
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

    private bool ContainsHookMethod(AptClassProps aptClassProps, string methodName) {
        return aptClassProps.codecProxyType!.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) != null;
    }

    private bool ContainsHookMethod(Type type, string methodName) {
        return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) != null;
    }

    /** 调用用户的readObject方法 */
    private bool GenReadObjectMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyType != null) {
            if (ContainsHookMethod(aptClassProps, CodecProcessor.MNAME_READ_OBJECT)) {
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
            if (ContainsHookMethod(aptClassProps, CodecProcessor.MNAME_WRITE_OBJECT)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(ref inst, writer)"
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
            if (ContainsHookMethod(aptClassProps, CodecProcessor.MNAME_BEFORE_ENCODE)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(ref inst, writer.Options)"
                    : "$T.$L(inst, writer.Options)";
                // CodecProxy.BeforeEncode(inst, writer.Options);
                beforeEncodeMethodBuilder.codeBuilder.AddStatement(format,
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_BEFORE_ENCODE);
                return true;
            }
        } else {
            if (containsBeforeEncodeMethod) {
                // inst.BeforeEncode(writer.Options);
                beforeEncodeMethodBuilder.codeBuilder.AddStatement("inst.$L(writer.Options)",
                    CodecProcessor.MNAME_BEFORE_ENCODE);
                return true;
            }
        }
        return false;
    }

    /** 调用用户AfterDecode钩子方法 -- 需要支持CodecProxy来处理 */
    private bool GenAfterDecodeMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyType != null) {
            if (ContainsHookMethod(aptClassProps, CodecProcessor.MNAME_AFTER_DECODE)) {
                string format = typeElement.IsValueType
                    ? "$T.$L(ref inst, reader.Options)"
                    : "$T.$L(inst, reader.Options)";
                // CodecProxy.AfterDecode(inst, reader.Options);
                afterDecodeMethodBuilder.codeBuilder.AddStatement(format,
                    aptClassProps.codecProxyClassName, CodecProcessor.MNAME_AFTER_DECODE);
                return true;
            }
        } else {
            if (containsAfterDecodeMethod) {
                // inst.AfterDecode(reader.Options);
                afterDecodeMethodBuilder.codeBuilder.AddStatement("inst.$L(reader.Options)",
                    CodecProcessor.MNAME_AFTER_DECODE);
                return true;
            }
        }
        return false;
    }

    /** 调用用户的NewInstance方法 */
    private void GenNewInstanceMethod(AptClassProps aptClassProps) {
        if (aptClassProps.IsSingleton()) {
            // 有CodecProxy的情况下，单例也交由CodecProxy实现 -- 方法名是CodecProxy指定的，因此应当存在，不做校验
            // c#还需要处理属性和方法的兼容...
            Type holder;
            TypeName holderTypeName;
            if (aptClassProps.codecProxyType != null) {
                holder = aptClassProps.codecProxyType;
                holderTypeName = aptClassProps.codecProxyClassName!;
            } else {
                holder = typeElement;
                holderTypeName = rawTypeName;
            }
            string format = ContainsHookMethod(holder, aptClassProps.Singleton!)
                ? "return $T.$L()"
                : "return $T.$L";
            newInstanceMethodBuilder.codeBuilder.AddStatement(format,
                holderTypeName, aptClassProps.Singleton!);
            return;
        }
        if (typeElement.IsAbstract) { // 抽象类
            newInstanceMethodBuilder.codeBuilder.AddStatement("throw new $T()", typeof(NotImplementedException));
            return;
        }

        if (aptClassProps.codecProxyType != null) {
            if (ContainsHookMethod(aptClassProps, CodecProcessor.MNAME_NEW_INSTANCE)) {
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
        } else {
            newInstanceMethodBuilder.codeBuilder.AddStatement("return new $T()", rawTypeName);
        }
    }

    #endregion

    #region field

    private void AddReadStatement(FieldInfo fieldInfo, AptFieldProps fieldProps, AptClassProps aptClassProps) {
        MethodSpec.Builder builder = readFieldsMethodBuilder;
        string fieldName = fieldInfo.Name;
        string? readProxy = fieldProps.attribute.ReadProxy;
        if (!string.IsNullOrWhiteSpace(readProxy)) { // 自定义读
            if (aptClassProps.codecProxyType != null) {
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                builder.codeBuilder.AddStatement("$T.$L(inst, reader, $L)",
                    aptClassProps.codecProxyClassName, readProxy, SerialName(fieldName));
            } else {
                builder.codeBuilder.AddStatement("inst.$L(reader, $L)",
                    readProxy, SerialName(fieldName));
            }
            return;
        }
        string readMethodName = GetReadMethodName(fieldInfo);
        PropertyInfo? setterMethod = processor.FindPublicSetter(fieldInfo, allFieldsAndMethodWithInherit, fieldProps);
        // 优先用setter，否则直接赋值 -- C#的属性和字段样式一致
        bool hasCustomSetter = !string.IsNullOrWhiteSpace(fieldProps.attribute.Setter);
        string fieldAccess;
        if (hasCustomSetter || setterMethod != null) {
            fieldAccess = hasCustomSetter ? fieldProps.attribute.Setter! : setterMethod!.Name;
        } else {
            fieldAccess = fieldName;
        }
        if (readMethodName == MNAME_READ_OBJECT) {
            // 读对象时要传入类型信息和Factory -- C#还要传泛型参数，有实现类时传实现类的类型，否则传声明类型
            // inst.name = reader.readObject(names_name, types_name, factories_name)
            if (fieldProps.implType != null) {
                builder.codeBuilder.AddStatement("inst.$L = reader.$L<$T>($L, typeof($T), $L)",
                    fieldAccess, readMethodName, fieldProps.implType,
                    SerialName(fieldName), fieldInfo.FieldType, SerialFactory(fieldName));
            } else {
                builder.codeBuilder.AddStatement("inst.$L = reader.$L<$T>($L, typeof($T), null)",
                    fieldAccess, readMethodName, fieldInfo.FieldType,
                    SerialName(fieldName), fieldInfo.FieldType);
            }
        } else {
            // inst.name = reader.readString(names_name)
            builder.codeBuilder.AddStatement("inst.$L = reader.$L($L)",
                fieldAccess, readMethodName,
                SerialName(fieldName));
        }
    }

    private void AddWriteStatement(FieldInfo fieldInfo, AptFieldProps fieldProps, AptClassProps aptClassProps) {
        string fieldName = fieldInfo.Name;
        MethodSpec.Builder builder = this.writeFieldsMethodBuilder;
        if (!string.IsNullOrWhiteSpace(fieldProps.attribute.WriteProxy)) { // 自定义写
            if (aptClassProps.codecProxyType != null) {
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                builder.codeBuilder.AddStatement("$T.$L(inst, writer, $L)",
                    aptClassProps.codecProxyClassName, fieldProps.attribute.WriteProxy, SerialName(fieldName));
            } else {
                builder.codeBuilder.AddStatement("inst.$L(writer, $L)",
                    fieldProps.attribute.WriteProxy, SerialName(fieldName));
            }
            return;
        }
        // 优先用getter，否则直接访问 -- C#的属性和字段样式一致
        string fieldAccess;
        bool hasCustomGetter = !string.IsNullOrWhiteSpace(fieldProps.attribute.Getter);
        PropertyInfo? getterMethod = processor.FindPublicGetter(fieldInfo, allFieldsAndMethodWithInherit, fieldProps);
        if (hasCustomGetter) {
            fieldAccess = fieldProps.attribute.Getter!;
        } else if (getterMethod != null) {
            fieldAccess = getterMethod.Name;
        } else {
            fieldAccess = fieldName;
        }

        // 处理数字 -- 涉及WireType和Style，注解使用的是枚举，我们转换为NumberStyles静态类
        string writeMethodName = GetWriteMethodName(fieldInfo);
        Type fieldType = fieldInfo.FieldType;
        if (fieldType.IsPrimitive) {
            if (numberTypes.Contains(fieldType)) {
                // writer.WriteInt(names_fieldName, inst.field, WireType.VarInt, NumberStyles.Simple)
                builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, $T.$L, $T.$L)",
                    writeMethodName, SerialName(fieldName), fieldAccess,
                    processor.typeName_WireType, Enum.GetName(typeof(WireType),fieldProps.attribute.WireType),
                    processor.typeName_NumberStyle, Enum.GetName(typeof(NumberStyle),fieldProps.attribute.NumberStyle));
                return;
            }
            if (fieldType == typeof(float) || fieldType == typeof(double)) {
                // writer.writeInt(names_fieldName, inst.field, NumberStyles.Simple)
                builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, $T.$L)",
                    writeMethodName, SerialName(fieldName), fieldAccess,
                    processor.typeName_NumberStyle, Enum.GetName(typeof(NumberStyle), fieldProps.attribute.NumberStyle));
                return;
            }
        }

        // 其它类型
        switch (writeMethodName) {
            case MNAME_WRITE_STRING: {
                // writer.writeString(names_fieldName, inst.getName(), StringStyle.AUTO)
                builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, $T.$L)",
                    writeMethodName, SerialName(fieldName), fieldAccess,
                    processor.typeName_StringStyle, Enum.GetName(typeof(StringStyle), fieldProps.attribute.StringStyle));
                break;
            }
            case MNAME_WRITE_OBJECT: {
                // 写Object时传入类型信息和Style
                // writer.writeObject(names_fieldName, inst.getName(), types_name, ObjectStyle.INDENT)
                if (fieldProps.attribute.ObjectStyle.HasValue) {
                    builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, typeof($T), $T.$L)",
                        writeMethodName, SerialName(fieldName), fieldAccess, fieldType,
                        processor.typeName_ObjectStyle, Enum.GetName(typeof(ObjectStyle),fieldProps.attribute.ObjectStyle.Value));
                } else {
                    builder.codeBuilder.AddStatement("writer.$L($L, inst.$L, typeof($T), null)",
                        writeMethodName, SerialName(fieldName), fieldAccess, fieldType);
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
    private string GetWriteMethodName(FieldInfo fieldInfo) {
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
        if (fieldType == typeof(ObjectPtr)) {
            return MNAME_WRITE_PTR;
        }
        if (fieldType == typeof(ObjectPtr)) {
            return MNAME_WRITE_LITE_PTR;
        }
        if (fieldType == typeof(DateTime)) {
            return MNAME_WRITE_DATETIME;
        }
        return MNAME_WRITE_OBJECT;
    }

    /** 获取reader读字段的方法名 */
    private string GetReadMethodName(FieldInfo fieldInfo) {
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
        if (fieldType == typeof(ObjectPtr)) {
            return MNAME_READ_PTR;
        }
        if (fieldType == typeof(ObjectPtr)) {
            return MNAME_READ_LITE_PTR;
        }
        if (fieldType == typeof(DateTime)) {
            return MNAME_READ_DATETIME;
        }
        return MNAME_READ_OBJECT;
    }

    private const string MNAME_READ_STRING = "ReadString";
    private const string MNAME_READ_BYTES = "ReadBytes";
    private const string MNAME_READ_OBJECT = "ReadObject";

    private const string MNAME_READ_PTR = "ReadPtr";
    private const string MNAME_READ_LITE_PTR = "ReadLitePtr";
    private const string MNAME_READ_DATETIME = "ReadDateTime";
    private const string MNAME_READ_TIMESTAMP = "ReadTimestamp";

    private const string MNAME_WRITE_STRING = "WriteString";
    private const string MNAME_WRITE_BYTES = "WriteBytes";
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
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(char), "char" },
        };
        foreach (KeyValuePair<Type, string> pair in type2KeywordDic) {
            string name = BeanUtils.FirstCharToUpperCase(pair.Value);
            primitiveReadMethodNameMap[pair.Key] = "Read" + name;
            primitiveWriteMethodNameMap[pair.Key] = "Write" + name;
        }

        numberTypes.AddAll(new[]
        {
            typeof(int),
            typeof(long),
            typeof(uint),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(char)
        });
    }

    #endregion
}
}