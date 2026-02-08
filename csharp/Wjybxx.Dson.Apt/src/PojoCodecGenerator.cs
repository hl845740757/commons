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
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;
using ClassName = Wjybxx.Commons.Poet.ClassName;
using TypeName = Wjybxx.Commons.Poet.TypeName;

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
    private readonly INamedTypeSymbol typeSymbol;
    private readonly TypeSpec.Builder typeBuilder;
    private readonly List<ISymbol> allMembers;

    private readonly ClassName rawTypeName;
    private MethodSpec.Builder newInstanceMethodBuilder;
    private MethodSpec.Builder readObjectMethodBuilder;
    private MethodSpec.Builder readFieldsMethodBuilder;
    private MethodSpec.Builder readFieldMethodBuilder;
    private MethodSpec.Builder afterDecodeMethodBuilder;

    private MethodSpec.Builder beforeEncodeMethodBuilder;
    private MethodSpec.Builder writeObjectMethodBuilder;
    private MethodSpec.Builder writeFieldsMethodBuilder;

    public PojoCodecGenerator(CodecProcessor processor, Context context) {
        this.processor = processor;
        this.context = context;

        this.typeSymbol = context.type;
        this.rawTypeName = context.rawTypeName;
        this.typeBuilder = context.typeBuilder;
        this.allMembers = context.allMembers;
    }
#nullable restore

    public void Execute() {
        Init();
        Gen();
    }

    private void Init() {
        // 需要先初始化superDeclaredType
        INamedTypeSymbol superDeclaredType = context.superDeclaredType;
        newInstanceMethodBuilder = processor.NewNewInstanceMethodBuilder(superDeclaredType);
        readObjectMethodBuilder = processor.NewReadObjectMethodBuilder(superDeclaredType);
        readFieldsMethodBuilder = processor.NewReadFieldsMethodBuilder(superDeclaredType);
        readFieldMethodBuilder = processor.NewReadFieldMethodBuilder(superDeclaredType);
        afterDecodeMethodBuilder = processor.NewAfterDecodeMethodBuilder(superDeclaredType);

        beforeEncodeMethodBuilder = processor.NewBeforeEncodeMethodBuilder(superDeclaredType);
        writeObjectMethodBuilder = processor.NewWriteObjectMethodBuilder(superDeclaredType);
        writeFieldsMethodBuilder = processor.NewWriteFieldsMethodBuilder(superDeclaredType);
    }

    private void Gen() {
        AptClassProps aptClassProps = context.aptClassProps;
        GenNewInstanceMethod(aptClassProps);
        GenReadFieldsMethod();
        GenWriteFieldsMethod();
        if (!aptClassProps.IsSingleton) {
            GenReadObjectMethod(aptClassProps);
            GenReadFieldMethod();
            GenAfterDecodeMethod(aptClassProps);
            //
            GenBeforeEncodeMethod(aptClassProps);
            GenWriteObjectMethod(aptClassProps);
        }
        // 控制方法生成顺序
        // GetEncoderType
        typeBuilder.AddMethod(processor.NewGetEncoderTypeMethod(context.superDeclaredType, rawTypeName));
        {
            // BeforeEncode回调
            if (!beforeEncodeMethodBuilder.codeBuilder.IsEmpty) {
                typeBuilder.AddMethod(beforeEncodeMethodBuilder.Build());
            }
            // WriteObject回调
            if (!writeObjectMethodBuilder.codeBuilder.IsEmpty) {
                typeBuilder.AddMethod(writeObjectMethodBuilder.Build());
            }
            // WriteFields
            typeBuilder.AddMethod(writeFieldsMethodBuilder.Build(true));
        }
        {
            // NewInstance
            typeBuilder.AddMethod(newInstanceMethodBuilder.Build());
            // ReadObject回调
            if (!readObjectMethodBuilder.codeBuilder.IsEmpty) {
                typeBuilder.AddMethod(readObjectMethodBuilder.Build());
            }
            // ReadFields
            typeBuilder.AddMethod(readFieldsMethodBuilder.Build(true));
            // ReadField
            if (!readFieldMethodBuilder.codeBuilder.IsEmpty) {
                typeBuilder.AddMethod(readFieldMethodBuilder.Build(true));
            }
            // AfterDecode回调
            if (!afterDecodeMethodBuilder.codeBuilder.IsEmpty) {
                typeBuilder.AddMethod(afterDecodeMethodBuilder.Build());
            }
        }
        // 额外注解
        if (context.additionalAnnotations != null) {
            typeBuilder.AddAttributes(context.additionalAnnotations);
        }
    }

    #region hook

    /** 调用用户的readObject方法 */
    private bool GenReadObjectMethod(AptClassProps aptClassProps) {
        const string methodName = CodecProcessor.MNAME_READ_OBJECT;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            string format = typeSymbol.IsValueType
                ? "$T.$L(ref inst, reader)"
                : "$T.$L(inst, reader)";
            // CodecProxy.ReadObject(inst, reader);
            readObjectMethodBuilder.codeBuilder.AddStatement(format,
                linkerContext.rawTypeName, methodName);
            return true;
        }
        if (processor.ContainsReadObjectMethod(allMembers)) {
            // inst.ReadObject(reader);
            readObjectMethodBuilder.codeBuilder.AddStatement("inst.$L(reader)", methodName);
            return true;
        }
        return false;
    }

    /** 调用用户的writeObject方法 */
    private bool GenWriteObjectMethod(AptClassProps aptClassProps) {
        const string methodName = CodecProcessor.MNAME_WRITE_OBJECT;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            // 允许CodecProxy不存在的情况下回滚到类型定义的代理
            string format = typeSymbol.IsValueType
                ? "$T.$L(ref inst, writer)"
                : "$T.$L(inst, writer)";
            // CodecProxy.WriteObject(inst, writer);
            writeObjectMethodBuilder.codeBuilder.AddStatement(format,
                linkerContext.rawTypeName, methodName);
            return true;
        }
        if (processor.ContainsWriteObjectMethod(allMembers)) {
            // inst.WriteObject(writer);
            writeObjectMethodBuilder.codeBuilder.AddStatement("inst.$L(writer)", methodName);
            return true;
        }
        return false;
    }

    /** 调用用户BeforeEncode钩子方法 -- 需要支持codecProxy来处理 */
    private bool GenBeforeEncodeMethod(AptClassProps aptClassProps) {
        const string methodName = CodecProcessor.MNAME_BEFORE_ENCODE;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            string format = typeSymbol.IsValueType
                ? "$T.$L(ref inst, writer.Options)"
                : "$T.$L(inst, writer.Options)";
            // CodecProxy.BeforeEncode(inst, writer.Options);
            beforeEncodeMethodBuilder.codeBuilder.AddStatement(format,
                linkerContext.rawTypeName, methodName);
            return true;
        }
        (bool contains, int argCount) tuple = processor.ContainsBeforeEncodeMethod(allMembers);
        if (tuple.contains) {
            if (tuple.argCount == 1) {
                // inst.BeforeEncode(writer.Options);
                beforeEncodeMethodBuilder.codeBuilder.AddStatement("inst.$L(writer.Options)", methodName);
            } else {
                // inst.BeforeEncode();
                beforeEncodeMethodBuilder.codeBuilder.AddStatement("inst.$L()", methodName);
            }
            return true;
        }
        return false;
    }

    /** 调用用户AfterDecode钩子方法 -- 需要支持CodecProxy来处理 */
    private bool GenAfterDecodeMethod(AptClassProps aptClassProps) {
        const string methodName = CodecProcessor.MNAME_AFTER_DECODE;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            string format = typeSymbol.IsValueType
                ? "$T.$L(ref inst, reader.Options)"
                : "$T.$L(inst, reader.Options)";
            // CodecProxy.AfterDecode(inst, reader.Options);
            afterDecodeMethodBuilder.codeBuilder.AddStatement(format,
                linkerContext.rawTypeName, methodName);
            return true;
        }
        (bool contains, int argCount) tuple = processor.ContainsAfterDecodeMethod(allMembers);
        if (tuple.contains) {
            if (tuple.argCount == 1) {
                // inst.AfterDecode(reader.Options);
                afterDecodeMethodBuilder.codeBuilder.AddStatement("inst.$L(reader.Options)", methodName);
            } else {
                // inst.AfterDecode();
                afterDecodeMethodBuilder.codeBuilder.AddStatement("inst.$L()", methodName);
            }
            return true;
        }
        return false;
    }

    /** 调用用户的NewInstance方法 */
    private void GenNewInstanceMethod(AptClassProps aptClassProps) {
        Context linkerContext = context.linkerContext;
        if (aptClassProps.IsSingleton) {
            // 有CodecProxy的情况下，单例也交由CodecProxy实现 -- 方法名是CodecProxy指定的，因此应当存在，不做校验
            INamedTypeSymbol? holder;
            TypeName holderTypeName;
            if (linkerContext != null) {
                holder = linkerContext.type;
                holderTypeName = linkerContext.rawTypeName!;
            } else {
                holder = typeSymbol;
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
        // 抽象类
        if (typeSymbol.IsAbstract) {
            newInstanceMethodBuilder.codeBuilder.AddStatement("throw new $T()", typeof(NotImplementedException));
            return;
        }
        //
        const string methodName = CodecProcessor.MNAME_NEW_INSTANCE;
        if (linkerContext != null
            && linkerContext.ContainsHookMethod(methodName)) {
            // CodecProxy.NewInstance(reader);
            newInstanceMethodBuilder.codeBuilder.AddStatement("return $T.$L(reader)",
                linkerContext.rawTypeName, methodName);
            return;
        }
        //
        if (processor.ContainsNewInstanceMethod(typeSymbol)) { // 静态解析方法，优先级更高
            // MyBean.NewInstance(reader);
            newInstanceMethodBuilder.codeBuilder.AddStatement("return $T.$L(reader)", rawTypeName, methodName);
        } else if (processor.ContainsReaderConstructor(typeSymbol)) { // 解析构造方法
            // return new MyBean(reader);
            newInstanceMethodBuilder.codeBuilder.AddStatement("return new $T(reader)", rawTypeName);
        } else if (typeSymbol.IsValueType) { // 值类型
            newInstanceMethodBuilder.codeBuilder.AddStatement("return default");
        } else {
            newInstanceMethodBuilder.codeBuilder.AddStatement("return new $T()", rawTypeName);
        }
    }

    #endregion

    #region field

    private void GenReadFieldsMethod() {
        AptClassProps aptClassProps = context.aptClassProps;
        CodeBlock.Builder codeBuilder = readFieldsMethodBuilder.codeBuilder;
        // 如果用户实现了ReadFields方法，则全权委托给用户
        const string methodName = CodecProcessor.MNAME_READ_FIELDS;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            if (typeSymbol.IsValueType) {
                codeBuilder.AddStatement("$T.$L(ref inst, reader)",
                    linkerContext.rawTypeName, methodName);
            } else {
                codeBuilder.AddStatement("$T.$L(inst, reader)",
                    linkerContext.rawTypeName, methodName);
            }
            return;
        }
        if (processor.ContainsReadFieldsMethod(context.allMembers)) {
            codeBuilder.AddStatement("inst.$L(reader)", methodName);
            return;
        }
        // array格式
        foreach (AptFieldInfo? fieldInfo in context.serialFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (!processor.IsAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                continue;
            }
            AddReadStatement(codeBuilder, fieldInfo, aptFieldProps, aptClassProps);
            codeBuilder.AddStatement("");
        }
    }

    private void GenReadFieldMethod() {
        AptClassProps aptClassProps = context.aptClassProps;
        CodeBlock.Builder codeBuilder = readFieldMethodBuilder.codeBuilder;
        // 如果用户实现了ReadField方法，则全权委托给用户
        const string methodName = CodecProcessor.MNAME_READ_FIELD;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            if (typeSymbol.IsValueType) {
                codeBuilder.AddStatement("return $T.$L(ref inst, reader, name)",
                    linkerContext.rawTypeName, methodName);
            } else {
                codeBuilder.AddStatement("return $T.$L(inst, reader, name)",
                    linkerContext.rawTypeName, methodName);
            }
            return;
        }
        if (processor.ContainsReadFieldMethod(context.allMembers)) {
            codeBuilder.AddStatement("return inst.$L(reader, name)", methodName);
            return;
        }

        // object样式
        int count = 0;
        codeBuilder.BeginControlFlow("switch (name)");
        foreach (AptFieldInfo? fieldInfo in context.serialFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (!processor.IsAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                continue;
            }
            codeBuilder.Add("case $L: ", SerialName(fieldInfo.Name));
            AddReadStatement(codeBuilder, fieldInfo, aptFieldProps, aptClassProps);
            codeBuilder.AddStatement("; return true");
            count++;
        }
        if (count > 0) {
            codeBuilder.AddStatement("default: return false");
            codeBuilder.EndControlFlow();
        } else {
            codeBuilder.Clear();
            codeBuilder.AddStatement("return false");
        }
    }

    private void AddReadStatement(CodeBlock.Builder codeBuilder, AptFieldInfo fieldInfo,
                                  AptFieldProps fieldProps, AptClassProps aptClassProps) {
        string fieldName = fieldInfo.Name;
        // 自定义读 -- 传入name以支持处理多个字段
        string? readProxy = fieldProps.readProxy;
        if (!string.IsNullOrWhiteSpace(readProxy)) {
            Context linkerContext = context.linkerContext;
            if (linkerContext != null) {
                // CodexProxy.ReadName(inst, reader, dsonName) 方法名是CodecProxy指定的，因此应当存在，不做校验
                codeBuilder.Add("$T.$L(inst, reader, $L)",
                    linkerContext.rawTypeName, readProxy, SerialName(fieldName));
            } else {
                // inst.ReadName(reader, dsonName)
                codeBuilder.Add("inst.$L(reader, $L)",
                    readProxy, SerialName(fieldName));
            }
            return;
        }

        // 优先用setter，否则直接赋值 -- C#的属性和字段样式一致
        bool hasCustomSetter = !string.IsNullOrWhiteSpace(fieldProps.setter);
        string fieldAccess;
        if (hasCustomSetter || fieldInfo.HasPublicSetter) {
            fieldAccess = hasCustomSetter ? fieldProps.setter! : fieldInfo.propertySymbol!.Name;
        } else {
            fieldAccess = fieldName;
        }
        // 处理需要传入Features的类型
        string readMethodName = GetReadMethodName(fieldInfo);
        if (readMethodName == MNAME_READ_OBJECT) {
            // 读Object时需要传入类型信息和Factory -- C#还要传泛型参数，泛型方法自动匹配
            // inst.name = reader.readObject<Type>(features, factories_name)
            TypeName fieldTypeName = fieldInfo.typeName!;
            if (fieldProps.implTypeName != null) {
                codeBuilder.Add("inst.$L = reader.$L<$T>(($T)$L, $L)",
                    fieldAccess, readMethodName, fieldTypeName,
                    CodecProcessor.typeName_DecodeFeatures, fieldProps.decodeFeatures,
                    SerialFactory(fieldName));
            } else {
                codeBuilder.Add("inst.$L = reader.$L<$T>(($T)$L)",
                    fieldAccess, readMethodName, fieldTypeName,
                    CodecProcessor.typeName_DecodeFeatures, fieldProps.decodeFeatures);
            }
            return;
        }
        // 枚举需要传入类型信息
        if (readMethodName == MNAME_READ_ENUM) {
            TypeName fieldTypeName = fieldInfo.typeName!;
            codeBuilder.Add("inst.$L = reader.$L<$T>(($T)$L)",
                fieldAccess, readMethodName, fieldTypeName,
                CodecProcessor.typeName_DecodeFeatures, fieldProps.decodeFeatures);
            return;
        }
        if (fieldProps.decodeFeatures != 0 && (fieldInfo.FieldType!.IsPrimitiveNumber()
                                               || readMethodName == MNAME_READ_BOOL
                                               || readMethodName == MNAME_READ_STRING
                                               || readMethodName == MNAME_READ_BYTES)) {
            // inst.name = reader.readString(features)
            codeBuilder.Add("inst.$L = reader.$L(($T)$L)",
                fieldAccess, readMethodName,
                CodecProcessor.typeName_DecodeFeatures, fieldProps.decodeFeatures);
        } else {
            // inst.name = reader.readString()
            codeBuilder.Add("inst.$L = reader.$L()",
                fieldAccess, readMethodName);
        }
    }

    private void GenWriteFieldsMethod() {
        AptClassProps aptClassProps = context.aptClassProps;
        CodeBlock.Builder codeBuilder = writeFieldsMethodBuilder.codeBuilder;
        // 如果用户实现了WriteFields方法，则全权委托给用户
        const string methodName = CodecProcessor.MNAME_WRITE_FIELDS;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            if (typeSymbol.IsValueType) {
                codeBuilder.AddStatement("$T.$L(ref inst, writer)",
                    linkerContext.rawTypeName, methodName);
            } else {
                codeBuilder.AddStatement("$T.$L(inst, writer)",
                    linkerContext.rawTypeName, methodName);
            }
            return;
        }
        if (processor.ContainsWriteFieldsMethod(context.allMembers)) {
            codeBuilder.AddStatement("inst.$L(writer)", methodName);
            return;
        }
        //
        foreach (AptFieldInfo? fieldInfo in context.serialFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (processor.IsAutoWriteField(fieldInfo, aptClassProps, aptFieldProps)) {
                AddWriteStatement(codeBuilder, fieldInfo, aptFieldProps, aptClassProps);
            }
        }
    }

    private void AddWriteStatement(CodeBlock.Builder codeBuilder, AptFieldInfo fieldInfo,
                                   AptFieldProps fieldProps, AptClassProps aptClassProps) {
        string fieldName = fieldInfo.Name;
        if (!string.IsNullOrWhiteSpace(fieldProps.writeProxy)) { // 自定义写
            Context linkerContext = context.linkerContext;
            if (linkerContext != null) {
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                codeBuilder.AddStatement("$T.$L(inst, writer, $L)",
                    linkerContext.rawTypeName, fieldProps.writeProxy, SerialName(fieldName));
            } else {
                codeBuilder.AddStatement("inst.$L(writer, $L)",
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
            fieldAccess = fieldInfo.propertySymbol!.Name;
        } else {
            fieldAccess = fieldName;
        }

        // 处理需要传入Features的类型
        string writeMethodName = GetWriteMethodName(fieldInfo);
        if (fieldProps.encodeFeatures != 0 && (fieldInfo.FieldType!.IsPrimitiveNumber()
                                               || writeMethodName == MNAME_WRITE_BOOL
                                               || writeMethodName == MNAME_WRITE_STRING
                                               || writeMethodName == MNAME_WRITE_ENUM
                                               || writeMethodName == MNAME_WRITE_BYTES
                                               || writeMethodName == MNAME_WRITE_OBJECT)) {
            // int,long,float,double,uint,ulong,short,ushort,byte,sbyte...
            // writer.writeInt(names_fieldName, inst.field, (SerializeFeatures)0x01)
            codeBuilder.AddStatement("writer.$L($L, inst.$L, ($T)$L)",
                writeMethodName, SerialName(fieldName), fieldAccess,
                CodecProcessor.typeName_EncodeFeatures, fieldProps.encodeFeatures);
        } else {
            // 未对DateTime等结构体做in优化，因为通过属性访问时，无法使用in
            // writer.writeInt(names_fieldName, inst.field)
            codeBuilder.AddStatement("writer.$L($L, inst.$L)",
                writeMethodName, SerialName(fieldName), fieldAccess);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SerialName(string fieldName) {
        return SchemaGenerator.GetNameFieldName(fieldName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SerialFactory(string fieldName) {
        return SchemaGenerator.GetFactoryFieldName(fieldName);
    }

    /** 获取writer写字段的方法名 */
    private string GetWriteMethodName(AptFieldInfo fieldInfo) {
        ITypeSymbol fieldType = fieldInfo.FieldType!;
        if (primitiveWriteMethodNameMap.TryGetValue(fieldType.SpecialType, out string r)) {
            return r;
        }
        if (fieldType.TypeKind == TypeKind.Enum) return MNAME_WRITE_ENUM;
        if (fieldType.SpecialType == SpecialType.System_String) return MNAME_WRITE_STRING;
        if (fieldType.IsByteArray()) return MNAME_WRITE_BYTES;
        if (fieldType.IsSameType(processor.type_Binary)) return MNAME_WRITE_BINARY;
        if (fieldType.IsSameType(processor.type_Ptr)) return MNAME_WRITE_PTR;
        if (fieldType.SpecialType == SpecialType.System_DateTime) return MNAME_WRITE_DATETIME;
        if (fieldType.IsSameType(processor.type_Timestamp)) return MNAME_WRITE_TIMESTAMP;
        return MNAME_WRITE_OBJECT;
    }

    /** 获取reader读字段的方法名 */
    private string GetReadMethodName(AptFieldInfo fieldInfo) {
        ITypeSymbol fieldType = fieldInfo.FieldType!;
        if (primitiveReadMethodNameMap.TryGetValue(fieldType.SpecialType, out string r)) {
            return r;
        }
        if (fieldType.TypeKind == TypeKind.Enum) return MNAME_READ_ENUM;
        if (fieldType.SpecialType == SpecialType.System_String) return MNAME_READ_STRING;
        if (fieldType.IsByteArray()) return MNAME_READ_BYTES;
        if (fieldType.IsSameType(processor.type_Binary)) return MNAME_READ_BINARY;
        if (fieldType.IsSameType(processor.type_Ptr)) return MNAME_READ_PTR;
        if (fieldType.SpecialType == SpecialType.System_DateTime) return MNAME_READ_DATETIME;
        if (fieldType.IsSameType(processor.type_Timestamp)) return MNAME_READ_TIMESTAMP;
        return MNAME_READ_OBJECT;
    }

    private const string MNAME_READ_BOOL = "ReadBool";
    private const string MNAME_READ_STRING = "ReadString";
    private const string MNAME_READ_BYTES = "ReadBytes";
    private const string MNAME_READ_BINARY = "ReadBinary";
    private const string MNAME_READ_OBJECT = "ReadObject";

    private const string MNAME_READ_PTR = "ReadPtr";
    private const string MNAME_READ_DATETIME = "ReadDateTime";
    private const string MNAME_READ_TIMESTAMP = "ReadTimestamp";
    private const string MNAME_READ_ENUM = "ReadEnum";

    private const string MNAME_WRITE_BOOL = "WriteBool";
    private const string MNAME_WRITE_STRING = "WriteString";
    private const string MNAME_WRITE_BYTES = "WriteBytes";
    private const string MNAME_WRITE_BINARY = "WriteBinary";
    private const string MNAME_WRITE_OBJECT = "WriteObject";

    private const string MNAME_WRITE_PTR = "WritePtr";
    private const string MNAME_WRITE_DATETIME = "WriteDateTime";
    private const string MNAME_WRITE_TIMESTAMP = "WriteTimestamp";
    private const string MNAME_WRITE_ENUM = "WriteEnum";

    private static readonly Dictionary<SpecialType, string> primitiveReadMethodNameMap = new(12);
    private static readonly Dictionary<SpecialType, string> primitiveWriteMethodNameMap = new(12);

    static PojoCodecGenerator() {
        Dictionary<SpecialType, string> type2KeywordDic = new Dictionary<SpecialType, string>()
        {
            { SpecialType.System_Int32, "Int" },
            { SpecialType.System_Int64, "Long" },
            { SpecialType.System_Single, "Float" },
            { SpecialType.System_Double, "Double" },
            { SpecialType.System_Boolean, "Bool" },

            { SpecialType.System_UInt32, "UInt" },
            { SpecialType.System_UInt64, "ULong" },
            { SpecialType.System_Byte, "Byte" },
            { SpecialType.System_SByte, "SByte" },
            { SpecialType.System_Int16, "Short" },
            { SpecialType.System_UInt16, "UShort" },
            { SpecialType.System_Char, "Char" },
        };
        foreach (KeyValuePair<SpecialType, string> pair in type2KeywordDic) {
            primitiveReadMethodNameMap[pair.Key] = "Read" + pair.Value;
            primitiveWriteMethodNameMap[pair.Key] = "Write" + pair.Value;
        }
    }

    #endregion
}
}