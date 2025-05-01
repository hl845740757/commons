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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
/// <code>DsonSerializableAttribute</code>注解处理器
///
/// 1.最终序列化的都是字段，自动属性只是定义字段的快捷方法，自动属性字段的编码名默认为属性名。
/// 2.C#的代码生成器处理和Java不太一样 
/// </summary>
[Generator]
public class CodecProcessor : IIncrementalGenerator
{
    #region consts

    private const string CNAME_WireType = "Wjybxx.Dson.WireType";
    private const string CNAME_NumberStyle = "Wjybxx.Dson.Text.NumberStyle";
    private const string CNAME_StringStyle = "Wjybxx.Dson.Text.StringStyle";
    private const string CNAME_ObjectStyle = "Wjybxx.Dson.Text.ObjectStyle";
    private const string CNAME_ObjectPtr = "Wjybxx.Dson.Types.ObjectPtr";
    private const string CNAME_ObjectLitePtr = "Wjybxx.Dson.Types.ObjectLitePtr";
    private const string CNAME_Timestamp = "Wjybxx.Dson.Types.Timestamp";
    private const string CNAME_NumberStyles = "Wjybxx.Dson.Text.NumberStyles"; // 生成器直接指向工具类

    private const string CNAME_NonSerialize = "System.NonSerializedAttribute";
    private const string CNAME_TypeInfo = "Wjybxx.Commons.TypeInfo";
    private const string CNAME_TypeName = "Wjybxx.Commons.TypeName";

    // dson
    private const string CNAME_SERIALIZABLE = "Wjybxx.Dson.Codec.Attributes.DsonSerializableAttribute";
    private const string CNAME_PROPERTY = "Wjybxx.Dson.Codec.Attributes.DsonPropertyAttribute";
    private const string CNAME_DSON_IGNORE = "Wjybxx.Dson.Codec.Attributes.DsonIgnoreAttribute";
    private const string CNAME_DSON_READER = "Wjybxx.Dson.Codec.IDsonObjectReader";
    private const string CNAME_DSON_WRITER = "Wjybxx.Dson.Codec.IDsonObjectWriter";
    private const string CNAME_OPTIONS = "Wjybxx.Dson.Codec.ConverterOptions";
    // linker
    private const string CNAME_CODEC_LINKER_GROUP = "Wjybxx.Dson.Codec.Attributes.DsonCodecLinkerGroupAttribute";
    private const string CNAME_CODEC_LINKER = "Wjybxx.Dson.Codec.Attributes.DsonCodecLinkerAttribute";
    private const string CNAME_CODEC_LINKER_BEAN = "Wjybxx.Dson.Codec.Attributes.DsonCodecLinkerBeanAttribute";
    private const string MNAME_OUTPUT = "OutputNamespace"; // 输出命名空间
    private const string MNAME_TARGET = "Target"; // 链接的目标--C#是构造函数

    // codec
    internal const string CNAME_CODEC = "Wjybxx.Dson.Codec.IDsonCodec`1";
    internal const string MNAME_READ_OBJECT = "ReadObject";
    internal const string MNAME_WRITE_OBJECT = "WriteObject";
    // AbstractCodec
    internal const string CNAME_ABSTRACT_CODEC = "Wjybxx.Dson.Codec.AbstractDsonCodec`1";
    internal const string MNAME_GET_ENCODER_TYPE = "GetEncoderType";
    internal const string MNAME_BEFORE_ENCODE = "BeforeEncode";
    internal const string MNAME_WRITE_FIELDS = "WriteFields";
    internal const string MNAME_NEW_INSTANCE = "NewInstance";
    internal const string MNAME_READ_FIELDS = "ReadFields";
    internal const string MNAME_AFTER_DECODE = "AfterDecode";

    internal static readonly ClassName typeName_WireType = AptUtils.ClassNameOfCanonicalName(CNAME_WireType);
    internal static readonly ClassName typeName_NumberStyle = AptUtils.ClassNameOfCanonicalName(CNAME_NumberStyle);
    internal static readonly ClassName typeName_StringStyle = AptUtils.ClassNameOfCanonicalName(CNAME_StringStyle);
    internal static readonly ClassName typeName_ObjectStyle = AptUtils.ClassNameOfCanonicalName(CNAME_ObjectStyle);
    internal static readonly ClassName typeName_NumberStyles = AptUtils.ClassNameOfCanonicalName(CNAME_NumberStyles);

    private static readonly AttributeSpec processorInfoAnnotation = AptUtils.NewProcessorInfoAnnotation(typeof(CodecProcessor));

    #endregion

#nullable disable

    #region 字段

    // Dson
    internal INamedTypeSymbol anno_DsonSerializable;
    internal INamedTypeSymbol anno_DsonProperty;
    internal INamedTypeSymbol anno_DsonIgnore;
    internal INamedTypeSymbol type_DsonReader;
    internal INamedTypeSymbol type_DsonWriter;
    internal INamedTypeSymbol type_Options;

    // linker
    internal INamedTypeSymbol anno_CodecLinkerGroup;
    internal INamedTypeSymbol anno_CodecLinker;
    internal INamedTypeSymbol anno_CodecLinkerBean;

    // abstractCodec{T} -- 由于C#需要动态构建类型，才能重写方法，因此这里不缓存方法
    internal INamedTypeSymbol type_DsonCodec;
    internal INamedTypeSymbol type_AbstractCodec;

    // 基础类型
    internal INamedTypeSymbol type_String;
    internal INamedTypeSymbol type_Object;
    internal INamedTypeSymbol type_Ptr;
    internal INamedTypeSymbol type_LitePtr;
    internal INamedTypeSymbol type_LocalDateTime;
    internal INamedTypeSymbol type_Timestamp;

    internal INamedTypeSymbol type_NumberStyle;
    internal INamedTypeSymbol type_StringStyle;
    internal INamedTypeSymbol type_ObjectStyle;

    private SourceProductionContext sourceProductionContext;
    private readonly CodeWriter _codeWriter = new CodeWriter();

    #endregion

    public CodecProcessor() {
    }

    #region init

    private void EnsureInited(SourceProductionContext sourceProductionContext, Compilation compilation) {
        if (anno_DsonSerializable != null) return;
        this.sourceProductionContext = sourceProductionContext;

        // dson
        anno_DsonSerializable = compilation.GetTypeByMetadataName(CNAME_SERIALIZABLE);
        anno_DsonProperty = compilation.GetTypeByMetadataName(CNAME_PROPERTY);
        anno_DsonIgnore = compilation.GetTypeByMetadataName(CNAME_DSON_IGNORE);
        type_DsonReader = compilation.GetTypeByMetadataName(CNAME_DSON_READER);
        type_DsonWriter = compilation.GetTypeByMetadataName(CNAME_DSON_WRITER);
        type_Options = compilation.GetTypeByMetadataName(CNAME_OPTIONS);
        // linker
        anno_CodecLinkerGroup = compilation.GetTypeByMetadataName(CNAME_CODEC_LINKER_GROUP);
        anno_CodecLinker = compilation.GetTypeByMetadataName(CNAME_CODEC_LINKER);
        anno_CodecLinkerBean = compilation.GetTypeByMetadataName(CNAME_CODEC_LINKER_BEAN);
        // codec
        type_DsonCodec = compilation.GetTypeByMetadataName(CNAME_CODEC);
        type_AbstractCodec = compilation.GetTypeByMetadataName(CNAME_ABSTRACT_CODEC);

        // 基础类型
        type_String = compilation.GetSpecialType(SpecialType.System_String);
        type_Object = compilation.GetSpecialType(SpecialType.System_Object);
        type_LocalDateTime = compilation.GetSpecialType(SpecialType.System_DateTime);
        type_Ptr = compilation.GetTypeByMetadataName(CNAME_ObjectPtr);
        type_LitePtr = compilation.GetTypeByMetadataName(CNAME_ObjectLitePtr);
        type_Timestamp = compilation.GetTypeByMetadataName(CNAME_Timestamp);

        type_NumberStyle = compilation.GetTypeByMetadataName(CNAME_NumberStyle);
        type_StringStyle = compilation.GetTypeByMetadataName(CNAME_StringStyle);
        type_ObjectStyle = compilation.GetTypeByMetadataName(CNAME_ObjectStyle);
    }

    private void ReportDiagnostic(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args) {
        Location? location = symbol.Locations == null ? null : symbol.Locations[0];
        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }

    private void ReportException(Exception ex, ISymbol symbol) {
        ReportDiagnostic(new DiagnosticDescriptor("DS0000",
                "Exception",
                "Generator Code Caught Exception message: {0}, stackTrace: {1}", "DsonCodec",
                DiagnosticSeverity.Error, true),
            symbol, ex.Message, ex.StackTrace);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // DsonSerializable
        {
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName(CNAME_SERIALIZABLE,
                (node, _) => node.GetLocation().IsInSource,
                (node, _) => node);
            context.RegisterSourceOutput(provider, (a, b) => {
                try {
                    ProcessDirectType(a, b);
                }
                catch (Exception ex) {
                    ReportException(ex, b.TargetSymbol);
                }
            });
        }
        // LinkerGroup
        {
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName(CNAME_CODEC_LINKER_GROUP,
                (node, _) => node.GetLocation().IsInSource,
                (node, _) => node);
            context.RegisterSourceOutput(provider, (a, b) => {
                try {
                    ProcessLinkerGroup(a, b);
                }
                catch (Exception ex) {
                    ReportException(ex, b.TargetSymbol);
                }
            });
        }
        // LinkerBean
        {
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName(CNAME_CODEC_LINKER_BEAN,
                (node, _) => node.GetLocation().IsInSource,
                (node, _) => node);
            context.RegisterSourceOutput(provider, (a, b) => {
                try {
                    ProcessLinkerBean(a, b);
                }
                catch (Exception ex) {
                    ReportException(ex, b.TargetSymbol);
                }
            });
        }
    }

    #endregion

    #region process

    /// <summary>
    /// 不是为自己生成，当前类是Codec配置类，为绑定的类型生成
    /// </summary>
    private void ProcessLinkerBean(SourceProductionContext sourceProductionContext, GeneratorAttributeSyntaxContext node) {
        EnsureInited(sourceProductionContext, node.SemanticModel.Compilation);
        AttributeData linkerBeanAttribute = AptUtils.GetAttribute(node.Attributes, CNAME_CODEC_LINKER_BEAN);
        Debug.Assert(linkerBeanAttribute != null);
        // LinkerBean
        Context linkerBeanContext = new Context(node.TargetSymbol as INamedTypeSymbol);
        linkerBeanContext.linkerBeanAttribute = linkerBeanAttribute;
        // Target是构造函数参数，而Namespace是属性参数
        INamedTypeSymbol targetType = linkerBeanAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
        string? outNamespace = null;
        if (AptUtils.GetAttributeValue(linkerBeanAttribute, MNAME_OUTPUT, out TypedConstant typedConstant)) {
            outNamespace = typedConstant.GetValueAsString();
        }
        outNamespace = GetOutputNamespace(linkerBeanContext.type, outNamespace);

        // 真实需要生成Codec的类型
        AptClassProps aptClassProps = AptClassProps.Parse(linkerBeanAttribute);
        // 创建模拟数据
        Context context = new Context(targetType);
        context.linkerBeanAttribute = linkerBeanAttribute;
        context.outputNamespace = outNamespace;

        context.aptClassProps = aptClassProps;
        context.additionalAnnotations = GetAdditionalAnnotations(aptClassProps);
        CacheFields(context);
        CacheFieldProps(context);
        // 修正字段的Props —— 将LinkerBean上的注解信息转移到目标类
        {
            CacheFields(linkerBeanContext);
            CacheFieldProps(linkerBeanContext);

            // 按name缓存，提高效率
            Dictionary<string, AptFieldProps> fieldName2FieldPropsMap = new(linkerBeanContext.fieldPropsMap.Count);
            foreach (KeyValuePair<IFieldSymbol, AptFieldProps> pair in linkerBeanContext.fieldPropsMap) {
                fieldName2FieldPropsMap[pair.Key.Name] = pair.Value;
            }
            foreach (IFieldSymbol fieldInfo in context.allFields) {
                if (fieldName2FieldPropsMap.TryGetValue(fieldInfo.Name, out AptFieldProps? aptFieldProps)) {
                    context.fieldPropsMap[fieldInfo] = aptFieldProps;
                }
            }
        }
        // 绑定CodecProxy
        {
            aptClassProps.codecProxyType = linkerBeanContext.type;
            aptClassProps.codecProxyClassName = AptUtils.ParseType(linkerBeanContext.type);
        }
        // 检查数据
        {
            CheckType(context);
        }
        // 生成Codec
        {
            GenericCodec(context);
        }
    }

    /// <summary>
    /// 不是为自己生成，当前类是配置类，为字段类型生成
    /// </summary>
    /// <param name="sourceProductionContext"></param>
    /// <param name="node"></param>
    private void ProcessLinkerGroup(SourceProductionContext sourceProductionContext, GeneratorAttributeSyntaxContext node) {
        EnsureInited(sourceProductionContext, node.SemanticModel.Compilation);
        AttributeData linkerGroupAttribute = AptUtils.GetAttribute(node.Attributes, CNAME_CODEC_LINKER_GROUP);
        Debug.Assert(linkerGroupAttribute != null);

        Context linkerGroupContext = new Context(node.TargetSymbol as INamedTypeSymbol);
        linkerGroupContext.linkerGroupAttribute = linkerGroupAttribute;
        // Namespace是属性参数
        string? outNamespace = null;
        if (AptUtils.GetAttributeValue(linkerGroupAttribute, MNAME_OUTPUT, out TypedConstant typedConstant)) {
            outNamespace = typedConstant.GetValueAsString();
        }
        outNamespace = GetOutputNamespace(linkerGroupContext.type, outNamespace);

        CacheFields(linkerGroupContext);
        foreach (IFieldSymbol fieldInfo in linkerGroupContext.allFields) {
            // 查找字段的配置
            AttributeData linkerAttribute = AptUtils.GetAttribute(fieldInfo.GetAttributes(), CNAME_CODEC_LINKER);
            AptClassProps aptClassProps = AptClassProps.Parse(linkerAttribute);

            // 泛型字段需要转换为泛型定义类 -- 不能连接到特殊类型
            INamedTypeSymbol targetType = fieldInfo.Type as INamedTypeSymbol;
            if (targetType == null) {
                continue;
            }
            if (targetType.IsGenericType) {
                targetType = targetType.ConstructedFrom;
            }

            Context context = new Context(targetType);
            context.linkerGroupAttribute = linkerGroupAttribute;
            context.outputNamespace = outNamespace;

            context.aptClassProps = aptClassProps;
            context.additionalAnnotations = GetAdditionalAnnotations(aptClassProps);
            CacheFields(context);
            CacheFieldProps(context);
            // 检查数据
            {
                CheckType(context);
            }
            // 生成Codec
            {
                GenericCodec(context);
            }
        }
    }

    private void ProcessDirectType(SourceProductionContext sourceProductionContext, GeneratorAttributeSyntaxContext node) {
        EnsureInited(sourceProductionContext, node.SemanticModel.Compilation);
        AttributeData serializableAttribute = AptUtils.GetAttribute(node.Attributes, CNAME_SERIALIZABLE);
        Debug.Assert(serializableAttribute != null);

        Context context = new Context(node.TargetSymbol as INamedTypeSymbol);
        context.dsonSerilAttribute = serializableAttribute;

        CacheFields(context);
        CacheFieldProps(context);
        context.aptClassProps = AptClassProps.Parse(context.dsonSerilAttribute);
        context.additionalAnnotations = GetAdditionalAnnotations(context.aptClassProps);
        // 检查数据
        {
            CheckType(context);
        }
        // 生成Codec
        {
            GenericCodec(context);
        }
    }

    // --------------------------------------------------------

    private void GenericCodec(Context context) {
        INamedTypeSymbol type = context.type; // C#不需要处理Enum
        INamedTypeSymbol superDeclaredType = type_AbstractCodec.Construct(type);
        InitTypeBuilder(context, type, superDeclaredType);

        SchemaGenerator schemaGenerator = new SchemaGenerator(this, context);
        schemaGenerator.Execute();

        PojoCodecGenerator codecGenerator = new PojoCodecGenerator(this, context);
        codecGenerator.Execute();

        // 写入文件
        string outputNamespace = GetOutputNamespace(type, context.outputNamespace);
        CsharpFile csharpFile = CsharpFile.NewBuilder(context.typeBuilder.name)
            .AddSpec(new MacroSpec("pragma", "warning disable CS1591"))
            .AddSpec(NamespaceSpec.Of(outputNamespace, context.typeBuilder.Build()))
            .Build();

        _codeWriter.Reset();
        sourceProductionContext.AddSource(context.typeBuilder.name,
            _codeWriter.Write(csharpFile));
    }

    private void CacheFields(Context context) {
        context.allFieldsAndMethodWithInherit = BeanUtils.GetAllMembersWithInherit(context.type);
        // 包含自动属性字段
        context.allFields = context.allFieldsAndMethodWithInherit
            .Where(e => e.Kind == SymbolKind.Field && !e.IsStatic)
            .Cast<IFieldSymbol>()
            .ToList();
    }

    private void CacheFieldProps(Context context) {
        foreach (IFieldSymbol fieldInfo in context.allFields) {
            // 最终序列化的都是字段，自动属性是定义字段的快捷方法
            ISymbol attributeHolder;
            if (BeanUtils.IsAutoPropertyField(fieldInfo.Name)) {
                attributeHolder = BeanUtils.FindProperty(fieldInfo, context.allFieldsAndMethodWithInherit)!;
            } else {
                attributeHolder = fieldInfo;
            }
            AptFieldProps aptFieldProps = AptFieldProps.Parse(attributeHolder, CNAME_PROPERTY,
                type_NumberStyle, type_StringStyle, type_ObjectStyle);
            aptFieldProps.ParseIgnore(attributeHolder, CNAME_DSON_IGNORE);

            aptFieldProps.autoProperty = attributeHolder as IPropertySymbol;
            context.fieldPropsMap[fieldInfo] = aptFieldProps;
        }
    }

    /** 获取输出命名空间 -- 默认为配置类的命名空间 */
    private string GetOutputNamespace(INamedTypeSymbol type, string? outNamespace) {
        if (string.IsNullOrWhiteSpace(outNamespace)) {
            return type.ContainingNamespace.ToDisplayString() ?? throw new Exception();
        }
        return outNamespace;
    }

    /** 获取为生成的Codec附加的注解 */
    private List<AttributeSpec> GetAdditionalAnnotations(AptClassProps aptClassProps) {
        List<INamedTypeSymbol> attributes = aptClassProps.additionalAnnotations;
        List<AttributeSpec> result = new List<AttributeSpec>(attributes.Count);
        foreach (INamedTypeSymbol attribute in attributes) {
            ClassName className = (ClassName)AptUtils.ParseType(attribute);
            result.Add(AttributeSpec.NewBuilder(className)
                .Build());
        }
        return result;
    }

    private void InitTypeBuilder(Context context, INamedTypeSymbol type, INamedTypeSymbol superDeclaredType) {
        context.superDeclaredType = superDeclaredType;
        context.typeBuilder = TypeSpec.NewClassBuilder(GetCodecName(type))
            .AddModifiers(Modifiers.Public | Modifiers.Sealed) // 禁止手写类重写生成类
            .AddAttribute(processorInfoAnnotation)
            .AddBaseClass(AptUtils.ParseType(superDeclaredType));

        // 拷贝泛型参数 -- Codec泛型参数和原始类型泛型参数相同
        ClassName srcClassName = (ClassName)AptUtils.ParseType(type);
        foreach (TypeName typeArgument in srcClassName.typeArguments) {
            context.typeBuilder.AddTypeVariable((TypeVariableName)typeArgument);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetCodecName(INamedTypeSymbol type) {
        return AptUtils.GetProxyClassName(type, "Codec");
    }

    #endregion

    #region check

    /// <summary>
    /// 检查期间会收集需要序列化的字段
    /// </summary>
    /// <param name="context"></param>
    private void CheckType(Context context) {
        AptClassProps aptClassProps = context.aptClassProps;
        if (aptClassProps.IsSingleton) {
            return;
        }
        INamedTypeSymbol targetType = context.type;
        CheckConstructor(targetType, aptClassProps);

        List<ISymbol> allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
        List<ISymbol> instMethodWithInherit = allFieldsAndMethodWithInherit
            .Where(e => e.Kind == SymbolKind.Method || e.Kind == SymbolKind.Property)
            .Where(e => !e.IsStatic)
            .ToList();

        foreach (IFieldSymbol fieldInfo in context.allFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (!IsSerializableField(fieldInfo, instMethodWithInherit, aptFieldProps!)) {
                continue;
            }
            context.serialFields.Add(fieldInfo);

            if (IsAutoWriteField(fieldInfo, aptClassProps, aptFieldProps)) {
                CheckAutoWriteField(fieldInfo, aptFieldProps, allFieldsAndMethodWithInherit);
            }
            if (IsAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                CheckAutoReadField(fieldInfo, aptFieldProps, allFieldsAndMethodWithInherit);
            }
        }
    }

    /** 检查自动读字段 */
    private void CheckAutoReadField(IFieldSymbol fieldInfo, AptFieldProps aptFieldProps, List<ISymbol> allFieldsAndMethodWithInherit) {
        if (!string.IsNullOrWhiteSpace(aptFieldProps.readProxy)) {
            return;
        }
        // 工具读：需要是public或包含public setter
        if (!CanSetDirectly(fieldInfo)
            && string.IsNullOrWhiteSpace(aptFieldProps.setter)
            && FindPublicSetter(fieldInfo, allFieldsAndMethodWithInherit, aptFieldProps) == null) {
            //
            ReportDiagnostic(new DiagnosticDescriptor(
                    id: "DC1002",
                    title: "Setter Absent",
                    messageFormat: "auto write field {0} must be public or contains a public setter",
                    category: "DsonCodec",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                fieldInfo, fieldInfo.Name);
        }
    }

    /** 检查自动写字段 */
    private void CheckAutoWriteField(IFieldSymbol fieldInfo, AptFieldProps aptFieldProps, List<ISymbol> allFieldsAndMethodWithInherit) {
        if (!string.IsNullOrWhiteSpace(aptFieldProps.writeProxy)) {
            return;
        }
        // 工具写：需要是public字段或包含public getter
        if (!CanGetDirectly(fieldInfo)
            && string.IsNullOrWhiteSpace(aptFieldProps.getter)
            && FindPublicGetter(fieldInfo, allFieldsAndMethodWithInherit, aptFieldProps) == null) {
            //
            ReportDiagnostic(new DiagnosticDescriptor(
                    id: "DC1002",
                    title: "Getter Absent",
                    messageFormat: "auto write field {0} must be public or contains a public getter",
                    category: "DsonCodec",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                fieldInfo, fieldInfo.Name);
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void CheckConstructor(INamedTypeSymbol typeSymbol, AptClassProps aptClassProps) {
        if (typeSymbol.IsAbstract || typeSymbol.IsValueType) {
            return;
        }
        // 静态代理包含NewInstance方法
        if (aptClassProps.ContainsHookMethod(MNAME_NEW_INSTANCE)) {
            return;
        }
        if (BeanUtils.ContainsNoArgsConstructor(typeSymbol)
            || ContainsReaderConstructor(typeSymbol)
            || ContainsNewInstanceMethod(typeSymbol)) {
            return;
        }
        ReportDiagnostic(new DiagnosticDescriptor(
                id: "DC1003",
                title: "Constructor Absent",
                messageFormat: "SerializableClass {0} must contains no-args constructor or reader-args constructor!",
                category: "DsonCodec",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            typeSymbol, typeSymbol.Name);
    }

    #endregion

    #region 钩子查询

    /** 是否包含 T(Reader reader) 构造方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsReaderConstructor(INamedTypeSymbol typeElement) {
        return BeanUtils.ContainsOneArgsConstructor(typeElement, type_DsonReader);
    }

    /** 是否包含 newInstance(reader) 静态解码方法 -- 只能从当前类型查询 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsNewInstanceMethod(INamedTypeSymbol typeElement) {
        var staticMembers = typeElement.GetMembers().Where(e => e.IsStatic);
        return ContainsHookMethod(staticMembers, MNAME_NEW_INSTANCE, type_DsonReader);
    }

    /** 是否包含 readerObject(reader) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsReadObjectMethod(List<ISymbol> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_READ_OBJECT, type_DsonReader);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsWriteObjectMethod(List<ISymbol> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_WRITE_OBJECT, type_DsonWriter);
    }

    /** 是否包含 beforeEncode 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsBeforeEncodeMethod(List<ISymbol> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_BEFORE_ENCODE, type_Options);
    }

    /** 是否包含 afterDecode 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsAfterDecodeMethod(List<ISymbol> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_AFTER_DECODE, type_Options);
    }

    /** 是否包含指定参数的钩子方法 */
    private static bool ContainsHookMethod(IEnumerable<ISymbol> allFieldsAndMethodWithInherit, string methodName, ITypeSymbol argType) {
        return allFieldsAndMethodWithInherit
            .Where(e => e.Kind == SymbolKind.Method)
            .Cast<IMethodSymbol>()
            .Any(e => {
                if (!e.IsPublic() || e.Name != methodName) {
                    return false;
                }
                ImmutableArray<IParameterSymbol> parameterInfos = e.Parameters;
                if (parameterInfos.Length == 0) {
                    return false;
                }
                return parameterInfos[0].Type.Equals(argType, SymbolEqualityComparer.Default);
            });
    }

    #endregion

    #region 字段检查

    /// <summary>
    /// 测试是否可以直接读取字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接取值，则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanGetDirectly(IFieldSymbol fieldInfo) {
        return fieldInfo.IsPublic();
    }

    /// <summary>
    /// 测试是否可以直接写字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接赋值，则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanSetDirectly(IFieldSymbol fieldInfo) {
        if (fieldInfo.IsReadOnly) {
            return false;
        }
        return fieldInfo.IsPublic();
    }

    /**
     * 查找非private的getter方法
     *
     * @param allMethodWithInherit 所有的字段和方法，可能在父类中
     */
    internal IPropertySymbol? FindPublicGetter(IFieldSymbol fieldSymbol, List<ISymbol> allMethodWithInherit, AptFieldProps aptFieldProps) {
        IPropertySymbol? autoProperty = aptFieldProps.autoProperty;
        if (autoProperty != null) {
            IMethodSymbol? getMethod = autoProperty.GetMethod;
            return (getMethod != null && getMethod.IsPublic()) ? autoProperty : null;
        }
        return BeanUtils.FindPublicGetter(fieldSymbol, allMethodWithInherit);
    }

    /**
     * 查找非private的setter方法
     *
     * @param allMethodWithInherit 所有的字段和方法，可能在父类中
     */
    internal IPropertySymbol? FindPublicSetter(IFieldSymbol fieldSymbol, List<ISymbol> allMethodWithInherit, AptFieldProps aptFieldProps) {
        IPropertySymbol? autoProperty = aptFieldProps.autoProperty;
        if (autoProperty != null) {
            IMethodSymbol? setMethod = autoProperty.SetMethod;
            return (setMethod != null && setMethod.IsPublic()) ? autoProperty : null;
        }
        return BeanUtils.FindPublicSetter(fieldSymbol, allMethodWithInherit);
    }

    /**
     * 是否是可序列化的字段
     * 1.默认只序列化 public 字段
     * 2.默认忽略 <see cref="NonSerializedAttribute"/> 字段
     */
    internal bool IsSerializableField(IFieldSymbol fieldInfo, List<ISymbol> allMethodWithInherit, AptFieldProps aptFieldProps) {
        if (fieldInfo.IsStatic) {
            return false;
        }
        // 有注解的情况取决于注解的值，需取反 -- 注解已提前解析
        if (aptFieldProps.ignore.HasValue) {
            return !aptFieldProps.ignore.Value;
        }
        // 无注解的情况下，默认忽略 NonSerialized 字段
        if (AptUtils.GetAttribute(fieldInfo.GetAttributes(), CNAME_NonSerialize) != null) {
            return false;
        }
        // 判断public和getter/setter
        if (fieldInfo.IsPublic()) {
            return true;
        }
        // 自动属性优化
        if (aptFieldProps.autoProperty != null) {
            IMethodSymbol? getMethod = aptFieldProps.autoProperty.GetMethod;
            IMethodSymbol? setMethod = aptFieldProps.autoProperty.SetMethod;
            return (getMethod != null && getMethod.IsPublic())
                   && (setMethod != null && setMethod.IsPublic());
        }
        // setter更容易失败
        return BeanUtils.ContainsPublicSetter(fieldInfo, allMethodWithInherit)
               && BeanUtils.ContainsPublicGetter(fieldInfo, allMethodWithInherit);
    }

    /** 是否是托管写的字段 */
    internal bool IsAutoWriteField(IFieldSymbol fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.IsSingleton) {
            return false;
        }
        if (IsSkipField(fieldInfo, aptClassProps, aptFieldProps)) {
            return false;
        }
        return true;
    }

    /** 是否是托管读的字段 */
    internal bool IsAutoReadField(IFieldSymbol fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.IsSingleton) {
            return false;
        }
        // readonly或无setter的字段只能构造方法读
        if (fieldInfo.IsReadOnly) {
            return false;
        }
        if (IsSkipField(fieldInfo, aptClassProps, aptFieldProps)) {
            return false;
        }
        return true;
    }

    /** skip仅仅代表不自动读 */
    private static bool IsSkipField(IFieldSymbol fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.skipFields.Count == 0) {
            return false;
        }
        // 如果是自动属性，则使用属性名
        string fieldName = aptFieldProps.autoProperty != null ? aptFieldProps.autoProperty.Name : fieldInfo.Name;
        if (aptClassProps.skipFields.Contains(fieldName)) {
            return true; // 完全匹配
        }
        if (!aptClassProps.clippedSkipFields.Contains(fieldName)) {
            return false; // 简单名不存在
        }
        // 测试类名 -- 不测试FullName，C#的FullName并不易编写 
        string declaringTypeName = fieldInfo.ContainingType!.Name;
        if (aptClassProps.skipFields.Contains(declaringTypeName + "." + fieldName)) {
            return true;
        }
        return false;
    }

    #endregion

    #region overring util

    public MethodSpec NewGetEncoderTypeMethod(INamedTypeSymbol superDeclaredType, TypeName encoderTypeName) {
        IMethodSymbol? methodInfo = superDeclaredType.GetFirstMethod(MNAME_GET_ENCODER_TYPE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        // 需要处理泛型
        return AptUtils.Overriding(methodInfo)
            .Code(CodeBlock.Of("typeof($T)", encoderTypeName).WithExpressionStyle())
            .Build();
    }

    public MethodSpec.Builder NewNewInstanceMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = superDeclaredType.GetFirstMethod(MNAME_NEW_INSTANCE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewReadFieldsMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = superDeclaredType.GetFirstMethod(MNAME_READ_FIELDS);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewAfterDecodeMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = superDeclaredType.GetFirstMethod(MNAME_AFTER_DECODE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewBeforeEncodeMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = superDeclaredType.GetFirstMethod(MNAME_BEFORE_ENCODE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewWriteFieldsMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = superDeclaredType.GetFirstMethod(MNAME_WRITE_FIELDS);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    #endregion
}
}