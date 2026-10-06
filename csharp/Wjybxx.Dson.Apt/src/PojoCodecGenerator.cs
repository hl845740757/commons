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
using System.Linq;
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
    private MethodSpec.Builder readFieldMethodBuilder;
    private MethodSpec.Builder writeFieldsMethodBuilder;
    private MethodSpec.Builder setFieldMethodBuilder;

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
        readFieldMethodBuilder = processor.NewReadFieldMethodBuilder(superDeclaredType);
        writeFieldsMethodBuilder = processor.NewWriteFieldsMethodBuilder(superDeclaredType);
        setFieldMethodBuilder = processor.NewSetFieldMethodBuilder(superDeclaredType);
    }

    private void Gen() {
        AptClassProps aptClassProps = context.aptClassProps;
        GenNewInstanceMethod(aptClassProps);
        GenWriteFieldsMethod();
        if (!aptClassProps.IsSingleton) {
            GenReadFieldMethod();
            if (!typeSymbol.IsValueType) {
                GenSetFieldMethod();
            }
        }
        // 控制方法生成顺序
        // GetEncoderType
        typeBuilder.AddMethod(processor.NewGetEncoderTypeMethod(context.superDeclaredType, rawTypeName));
        {
            // WriteFields
            typeBuilder.AddMethod(writeFieldsMethodBuilder.Build(true));
        }
        {
            // NewInstance
            typeBuilder.AddMethod(newInstanceMethodBuilder.Build(true));
            // ReadField
            if (!readFieldMethodBuilder.codeBuilder.IsEmpty) {
                typeBuilder.AddMethod(readFieldMethodBuilder.Build(true));
            }
        }
        if (!setFieldMethodBuilder.codeBuilder.IsEmpty) {
            typeBuilder.AddMethod(setFieldMethodBuilder.Build(true));
        }
        // 额外注解
        if (context.additionalAnnotations != null) {
            typeBuilder.AddAttributes(context.additionalAnnotations);
        }
    }

    #region hook

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
        } else if (typeSymbol.IsValueType) {
            // 虽然高版本值类型new()会执行，但还是避免依赖更好
            newInstanceMethodBuilder.codeBuilder.AddStatement("return default", rawTypeName);
        } else {
            newInstanceMethodBuilder.codeBuilder.AddStatement("return new $T()", rawTypeName);
        }
    }

    #endregion

    #region field

    private void GenReadFieldMethod() {
        AptClassProps aptClassProps = context.aptClassProps;
        CodeBlock.Builder codeBuilder = readFieldMethodBuilder.codeBuilder;
        // 如果用户实现了ReadField方法，则全权委托给用户
        const string methodName = CodecProcessor.MNAME_READ_FIELD;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            string format = typeSymbol.IsValueType ? "return $T.$L(ref inst, reader, name)" : "return $T.$L(inst, reader, name)";
            codeBuilder.AddStatement(format,
                linkerContext.rawTypeName, methodName);
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
            codeBuilder.BeginControlFlow("case $L:", SerialName(fieldInfo.Name));
            AddReadStatement(codeBuilder, fieldInfo, aptFieldProps, aptClassProps);
            codeBuilder.AddStatement("return true");
            codeBuilder.EndControlFlow();
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
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                // CodexProxy.ReadValue(inst, reader, dsonName)
                string format = typeSymbol.IsValueType ? "$T.$L(ref inst, reader, $L)" : "$T.$L(inst, reader, $L)";
                codeBuilder.AddStatement(format,
                    linkerContext.rawTypeName, readProxy, SerialName(fieldName));
            } else {
                // inst.ReadName(reader, dsonName)
                codeBuilder.AddStatement("inst.$L(reader, $L)",
                    readProxy, SerialName(fieldName));
            }
            return;
        }

        string readMethodName = GetReadMethodName(fieldInfo);
        if (readMethodName == MNAME_READ_OBJECT) {
            AddReadObjectStatement(codeBuilder, fieldInfo, fieldProps);
            return;
        }
        // 枚举需要传入类型信息
        if (readMethodName == MNAME_READ_ENUM) {
            TypeName fieldTypeName = fieldInfo.typeName!;
            AddSetFieldStatement(codeBuilder, fieldInfo, fieldProps,
                CodeBlock.Of("reader.$L<$T>(($T)$L)", readMethodName, fieldTypeName,
                    CodecProcessor.typeName_DecodeFeatures, fieldProps.decodeFeatures));
            return;
        }
        if (fieldProps.decodeFeatures != 0 && (fieldInfo.FieldType!.IsPrimitiveNumber()
                                               || readMethodName == MNAME_READ_BOOL
                                               || readMethodName == MNAME_READ_STRING
                                               || readMethodName == MNAME_READ_BYTES)) {
            // inst.name = reader.readString(features)
            AddSetFieldStatement(codeBuilder, fieldInfo, fieldProps,
                CodeBlock.Of("reader.$L(($T)$L)", readMethodName,
                    CodecProcessor.typeName_DecodeFeatures, fieldProps.decodeFeatures));
        } else {
            // inst.name = reader.readString()
            AddSetFieldStatement(codeBuilder, fieldInfo, fieldProps, CodeBlock.Of("reader.$L()", readMethodName));
        }
    }

    private CodeBlock GetFieldValue(AptFieldInfo fieldInfo, AptFieldProps fieldProps) {
        if (!string.IsNullOrWhiteSpace(fieldProps.getter)) {
            return CodeBlock.Of("inst.$L", fieldProps.getter);
        }
        if (fieldInfo.HasPublicGetter) {
            return CodeBlock.Of("inst.$L", fieldInfo.propertySymbol!.Name);
        }
        if (processor.CanGetDirectly(fieldInfo)) {
            return CodeBlock.Of("inst.$L", fieldInfo.Name);
        }
        return CodeBlock.Of("$L(inst)", SchemaGenerator.GetGetValueMethodName(fieldInfo.Name));
    }

    private void AddSetFieldStatement(CodeBlock.Builder codeBuilder, AptFieldInfo fieldInfo,
                                      AptFieldProps fieldProps, CodeBlock value) {
        if (!string.IsNullOrWhiteSpace(fieldProps.setter)) {
            codeBuilder.AddStatement("inst.$L = $L", fieldProps.setter, value);
        } else if (fieldInfo.HasPublicSetter) {
            codeBuilder.AddStatement("inst.$L = $L", fieldInfo.propertySymbol!.Name, value);
        } else if (processor.CanSetDirectly(fieldInfo)) {
            codeBuilder.AddStatement("inst.$L = $L", fieldInfo.Name, value);
        } else {
            codeBuilder.AddStatement("$L(inst, $L)", SchemaGenerator.GetSetValueMethodName(fieldInfo.Name), value);
        }
    }

    private void GenSetFieldMethod() {
        CodeBlock.Builder codeBuilder = setFieldMethodBuilder.codeBuilder;
        int count = 0;
        codeBuilder.BeginControlFlow("switch (name)");
        foreach (AptFieldInfo fieldInfo in context.serialFields) {
            AptFieldProps props = context.fieldPropsMap[fieldInfo];
            if (!processor.IsAutoReadField(fieldInfo, context.aptClassProps, props)
                || GetReadMethodName(fieldInfo) != MNAME_READ_OBJECT) {
                continue;
            }
            // 只有序列化引用的字段和不可变集合需要处理
            bool readReference = (props.decodeFeatures & 0x01) != 0;
            if (!readReference && !(processor.IsImmutableCollection(fieldInfo.FieldType)
                                    || processor.IsImmutableDictionary(fieldInfo.FieldType))) {
                continue;
            }

            codeBuilder.Add("case $L: ", SerialName(fieldInfo.Name));
            codeBuilder.BeginControlFlow();
            AddSetFieldStatement(codeBuilder, fieldInfo, props, CodeBlock.Of("($T)value", fieldInfo.typeName));
            codeBuilder.AddStatement("return true");
            codeBuilder.EndControlFlow();
            count++;
        }
        codeBuilder.AddStatement("default: return false");
        codeBuilder.EndControlFlow();
        if (count == 0) codeBuilder.Clear();
    }

    private void AddReadObjectStatement(CodeBlock.Builder codeBuilder, AptFieldInfo fieldInfo,
                                        AptFieldProps props) {
        ITypeSymbol fieldType = fieldInfo.FieldType!;
        bool readReference = (props.decodeFeatures & 0x01) != 0;
        bool immutableDictionary = processor.IsImmutableDictionary(fieldType);
        bool immutableCollection = processor.IsImmutableCollection(fieldType);
        if (!readReference && !immutableDictionary && !immutableCollection) {
            AddReadObjectValue(codeBuilder, fieldInfo, props, fieldInfo.typeName);
            return;
        }

        ITypeSymbol readType = fieldType;
        ITypeSymbol targetType = props.targetType ?? fieldType;
        bool probeReference = false;
        // 字典读取为Dictionary，其它集合和数组读取为List - C#的数组实现了集合接口，需要先测试
        if (immutableDictionary || processor.IsDictionary(fieldType)) {
            INamedTypeSymbol name = (INamedTypeSymbol)fieldType;
            readType = processor.type_Dictionary.Construct(name.TypeArguments.ToArray());
        } else if (fieldType.TypeKind == TypeKind.Array) {
            IArrayTypeSymbol array = (IArrayTypeSymbol)fieldType;
            readType = processor.type_List.Construct(array.ElementType);
        } else if (immutableCollection || processor.IsCollection(fieldType)) {
            INamedTypeSymbol name = (INamedTypeSymbol)fieldType;
            readType = processor.type_List.Construct(name.TypeArguments.ToArray());
        } else {
            probeReference = true;
        }

        bool convert = props.targetType != null || !readType.IsSameType(fieldType);
        if (convert) {
            CheckDeferredField(fieldInfo);
            TypeName readTypeName = AptUtils.ParseType(readType).RemoveAllNullableAttribute();
            TypeName targetTypeName = AptUtils.ParseType(targetType).RemoveAllNullableAttribute();
            codeBuilder.AddStatement("$T value = reader.ReadObject<$T>(($T)$L)",
                readTypeName, readTypeName, CodecProcessor.typeName_DecodeFeatures, props.decodeFeatures);
            codeBuilder.AddStatement("reader.DeferToTargetType(this, inst, $L, value, ($T)null)",
                SerialName(fieldInfo.Name), targetTypeName);
            return;
        }

        // 集合类型不会被直接序列化为引用
        if (probeReference) {
            CheckDeferredField(fieldInfo);
            codeBuilder.BeginControlFlow("if (reader.TryReadRefId(out int refId))");
            codeBuilder.AddStatement("reader.DeferReference(refId, this, inst, $L)", SerialName(fieldInfo.Name));
            codeBuilder.NextControlFlow("else");
        }
        AddReadObjectValue(codeBuilder, fieldInfo, props,
            AptUtils.ParseType(readType).RemoveAllNullableAttribute());
        if (probeReference) {
            codeBuilder.EndControlFlow();
        }
    }

    private void AddReadObjectValue(CodeBlock.Builder codeBuilder, AptFieldInfo fieldInfo,
                                    AptFieldProps props, TypeName typeName) {
        AddSetFieldStatement(codeBuilder, fieldInfo, props,
            CodeBlock.Of("reader.ReadObject<$T>(($T)$L)",
                typeName, CodecProcessor.typeName_DecodeFeatures, props.decodeFeatures));
    }

    private void CheckDeferredField(AptFieldInfo fieldInfo) {
        if (typeSymbol.IsValueType) {
            throw new InvalidOperationException($"字段 {typeSymbol.Name}.{fieldInfo.Name} 需要延迟赋值，"
                                                + "但struct实例会被复制；请使用SerializeRef<T>或自定义读取代理");
        }
    }

    private void GenWriteFieldsMethod() {
        AptClassProps aptClassProps = context.aptClassProps;
        CodeBlock.Builder codeBuilder = writeFieldsMethodBuilder.codeBuilder;
        // 如果用户实现了WriteFields方法，则全权委托给用户
        const string methodName = CodecProcessor.MNAME_WRITE_FIELDS;
        Context linkerContext = context.linkerContext;
        if (linkerContext != null && linkerContext.ContainsHookMethod(methodName)) {
            string format = typeSymbol.IsValueType ? "$T.$L(ref inst, writer)" : "$T.$L(inst, writer)";
            codeBuilder.AddStatement(format,
                linkerContext.rawTypeName, methodName);
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
                // CodexProxy.WriteValue(inst, reader, dsonName)
                string format = typeSymbol.IsValueType ? "$T.$L(ref inst, writer, $L)" : "$T.$L(inst, writer, $L)";
                codeBuilder.AddStatement(format,
                    linkerContext.rawTypeName, fieldProps.writeProxy, SerialName(fieldName));
            } else {
                codeBuilder.AddStatement("inst.$L(writer, $L)",
                    fieldProps.writeProxy, SerialName(fieldName));
            }
            return;
        }
        CodeBlock fieldValue = GetFieldValue(fieldInfo, fieldProps);

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
            codeBuilder.AddStatement("writer.$L($L, $L, ($T)$L)",
                writeMethodName, SerialName(fieldName), fieldValue,
                CodecProcessor.typeName_EncodeFeatures, fieldProps.encodeFeatures);
        } else if (fieldProps.elementNames != null && (writeMethodName == MNAME_WRITE_DOUBLE4
                                                       || writeMethodName == MNAME_WRITE_LONG4
                                                       || writeMethodName == MNAME_WRITE_FXP4)) {
            // writer.writeDouble4(names_fieldName, inst.field, elementNames)
            codeBuilder.AddStatement("writer.$L($L, $L, $S)",
                writeMethodName, SerialName(fieldName), fieldValue, fieldProps.elementNames);
        } else {
            // 未对DateTime等结构体做in优化，因为通过属性访问时，无法使用in
            // writer.writeInt(names_fieldName, inst.field)
            codeBuilder.AddStatement("writer.$L($L, $L)",
                writeMethodName, SerialName(fieldName), fieldValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string SerialName(string fieldName) {
        return SchemaGenerator.GetNameFieldName(fieldName);
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
        if (fieldType.IsSameType(processor.type_RefId)) return MNAME_WRITE_REF_ID;
        if (fieldType.SpecialType == SpecialType.System_DateTime) return MNAME_WRITE_DATETIME;
        if (fieldType.IsSameType(processor.type_Timestamp)) return MNAME_WRITE_TIMESTAMP;
        if (fieldType.IsSameType(processor.type_Double4)) return MNAME_WRITE_DOUBLE4;
        if (fieldType.IsSameType(processor.type_Long4)) return MNAME_WRITE_LONG4;
        if (fieldType.IsSameType(processor.type_Fxp64)) return MNAME_WRITE_FXP64;
        if (fieldType.IsSameType(processor.type_Fxp4)) return MNAME_WRITE_FXP4;
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
        if (fieldType.IsSameType(processor.type_RefId)) return MNAME_READ_REF_ID;
        if (fieldType.SpecialType == SpecialType.System_DateTime) return MNAME_READ_DATETIME;
        if (fieldType.IsSameType(processor.type_Timestamp)) return MNAME_READ_TIMESTAMP;
        if (fieldType.IsSameType(processor.type_Double4)) return MNAME_READ_DOUBLE4;
        if (fieldType.IsSameType(processor.type_Long4)) return MNAME_READ_LONG4;
        if (fieldType.IsSameType(processor.type_Fxp64)) return MNAME_READ_FXP64;
        if (fieldType.IsSameType(processor.type_Fxp4)) return MNAME_READ_FXP4;
        return MNAME_READ_OBJECT;
    }

    private const string MNAME_READ_BOOL = "ReadBool";
    private const string MNAME_READ_STRING = "ReadString";
    private const string MNAME_READ_BYTES = "ReadBytes";
    private const string MNAME_READ_BINARY = "ReadBinary";
    private const string MNAME_READ_OBJECT = "ReadObject";

    private const string MNAME_READ_DOUBLE4 = "ReadDouble4";
    private const string MNAME_READ_LONG4 = "ReadLong4";
    private const string MNAME_READ_FXP64 = "ReadFxp64";
    private const string MNAME_READ_FXP4 = "ReadFxp4";
    private const string MNAME_READ_REF_ID = "ReadRefId";
    private const string MNAME_READ_DATETIME = "ReadDateTime";
    private const string MNAME_READ_TIMESTAMP = "ReadTimestamp";
    private const string MNAME_READ_ENUM = "ReadEnum";

    private const string MNAME_WRITE_BOOL = "WriteBool";
    private const string MNAME_WRITE_STRING = "WriteString";
    private const string MNAME_WRITE_BYTES = "WriteBytes";
    private const string MNAME_WRITE_BINARY = "WriteBinary";
    private const string MNAME_WRITE_OBJECT = "WriteObject";

    private const string MNAME_WRITE_DOUBLE4 = "WriteDouble4";
    private const string MNAME_WRITE_LONG4 = "WriteLong4";
    private const string MNAME_WRITE_FXP64 = "WriteFxp64";
    private const string MNAME_WRITE_FXP4 = "WriteFxp4";
    private const string MNAME_WRITE_REF_ID = "WriteRefId";
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