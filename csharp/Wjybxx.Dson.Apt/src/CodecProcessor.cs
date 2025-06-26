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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
///
/// 最初的实现为<code>IIncrementalGenerator</code>，但考虑到Unity兼容问题，Roslyn依赖降级为<code>改为3.8.0</code>，
/// 便只能实现为<see cref="ISourceGenerator"/>。
///
/// Q：为什么在编译时不能加载第三方程序集时，我们要生成不完整的codec代码？
/// A：这允许用户再通过反射为第三方类型生成Codec，然后再组装起来构成最终的Codec。
/// </summary>
[Generator]
public class CodecProcessor : ISourceGenerator
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

    private const string CNAME_COLLECTION_UTIL = "Wjybxx.Commons.Collections.CollectionUtil";
    private const string CNAME_IList = "System.Collections.Generic.IList`1";
    private const string CNAME_ISet = "System.Collections.Generic.ISet`1";
    private const string CNAME_IDictionary = "System.Collections.Generic.IDictionary`2";

    // dson
    private const string CNAME_SERIALIZABLE = "Wjybxx.Dson.Codec.Attributes.DsonSerializableAttribute";
    internal const string CNAME_PROPERTY = "Wjybxx.Dson.Codec.Attributes.DsonPropertyAttribute";
    internal const string CNAME_DSON_IGNORE = "Wjybxx.Dson.Codec.Attributes.DsonIgnoreAttribute";
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

    internal static readonly ClassName typeName_CollectionUtil = AptUtils.ClassNameOfCanonicalName(CNAME_COLLECTION_UTIL);
    internal static readonly ClassName typeName_WireType = AptUtils.ClassNameOfCanonicalName(CNAME_WireType);
    internal static readonly ClassName typeName_NumberStyle = AptUtils.ClassNameOfCanonicalName(CNAME_NumberStyle);
    internal static readonly ClassName typeName_StringStyle = AptUtils.ClassNameOfCanonicalName(CNAME_StringStyle);
    internal static readonly ClassName typeName_ObjectStyle = AptUtils.ClassNameOfCanonicalName(CNAME_ObjectStyle);
    internal static readonly ClassName typeName_NumberStyles = AptUtils.ClassNameOfCanonicalName(CNAME_NumberStyles);

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

    internal INamedTypeSymbol type_ILIST;
    internal INamedTypeSymbol type_ISET;
    internal INamedTypeSymbol type_IDICTIONARY;

    internal INamedTypeSymbol type_NumberStyle;
    internal INamedTypeSymbol type_StringStyle;
    internal INamedTypeSymbol type_ObjectStyle;

    private GeneratorExecutionContext sourceProductionContext;
    private Compilation compilation;
    private string buildingAssemblyName;
    private AttributeSpec processorInfoAnnotation;
    private readonly CodeWriter _codeWriter = new CodeWriter(indent: "    ");

    #endregion

    public CodecProcessor() {
    }

    #region init

    private void EnsureInited(GeneratorExecutionContext sourceProductionContext, Compilation compilation) {
        if (this.compilation != null) return;
        this.sourceProductionContext = sourceProductionContext;
        this.compilation = compilation;
        this.buildingAssemblyName = compilation.Assembly.Identity.Name;
        this.processorInfoAnnotation = AptUtils.NewProcessorInfoAnnotation(typeof(CodecProcessor),
            assembly: buildingAssemblyName);

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

        type_ILIST = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T);
        type_ISET = compilation.GetTypeByMetadataName(CNAME_ISet);
        type_IDICTIONARY = compilation.GetTypeByMetadataName(CNAME_IDictionary);

        type_NumberStyle = compilation.GetTypeByMetadataName(CNAME_NumberStyle);
        type_StringStyle = compilation.GetTypeByMetadataName(CNAME_StringStyle);
        type_ObjectStyle = compilation.GetTypeByMetadataName(CNAME_ObjectStyle);
    }

    private void ReportDiagnostic(DiagnosticSeverity severity, ISymbol? symbol, int code, string msgFormat, params object[] args) {
        Location? location = symbol == null ? null : symbol.GetFirstLocation();
        DiagnosticDescriptor descriptor = new DiagnosticDescriptor("DsonApt" + code, "", msgFormat, "DsonApt", severity, true);
        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
    }

    private void ReportException(Exception ex, ISymbol? symbol) {
        ReportDiagnostic(DiagnosticSeverity.Error, symbol, 0001, "Generator Caught Exception message: {0}, stackTrace: {1}",
            ex.Message, ex.StackTrace);
    }

    public void Initialize(GeneratorInitializationContext context) {
        context.RegisterForSyntaxNotifications(() => new OptionsSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context) {
        // 在Unity下可能会处理其它程序集的文件...
        if (context.Compilation.GetTypeByMetadataName(CNAME_SERIALIZABLE) == null) {
            return;
        }
        EnsureInited(context, context.Compilation);
        if (context.SyntaxReceiver is not OptionsSyntaxReceiver optionsSyntaxReceiver) {
            return;
        }
        foreach (var declarationSyntax in optionsSyntaxReceiver.typeDeclarationNodes) {
            var semanticModel = context.Compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            var typeSymbol = semanticModel.GetDeclaredSymbol(declarationSyntax) as INamedTypeSymbol;
            if (typeSymbol == null) {
                continue;
            }
            if (!IsBuildingAssemblyNode(typeSymbol)) {
                continue;
            }
            if (AptUtils.HasUsedForReflectionAttribute(typeSymbol.GetAttributes())) {
                continue;
            }
            try {
                AttributeData linkerBeanAttribute = AptUtils.GetAttribute(typeSymbol.GetAttributes(), CNAME_CODEC_LINKER_BEAN);
                if (linkerBeanAttribute != null) {
                    ProcessLinkerBean(typeSymbol, linkerBeanAttribute);
                    continue;
                }
                AttributeData linkerGroupAttribute = AptUtils.GetAttribute(typeSymbol.GetAttributes(), CNAME_CODEC_LINKER_GROUP);
                if (linkerGroupAttribute != null) {
                    ProcessLinkerGroup(typeSymbol, linkerGroupAttribute);
                    continue;
                }
                AttributeData serializableAttribute = AptUtils.GetAttribute(typeSymbol.GetAttributes(), CNAME_SERIALIZABLE);
                if (serializableAttribute != null) {
                    ProcessDirectType(typeSymbol, serializableAttribute);
                    continue;
                }
            }
            catch (Exception ex) {
                ReportException(ex, typeSymbol);
            }
        }
    }

    private bool IsBuildingAssemblyNode(INamedTypeSymbol typeSymbol) {
        IAssemblySymbol buildingAssembly = compilation.Assembly;
        IAssemblySymbol nodeAssembly = typeSymbol.ContainingAssembly;
        return buildingAssembly.Name == nodeAssembly.Name;
        // return nodeAssembly.Equals(buildingAssembly, SymbolEqualityComparer.Default);
    }

    private class OptionsSyntaxReceiver : ISyntaxReceiver
    {
        public readonly List<TypeDeclarationSyntax> typeDeclarationNodes = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            // 3.8.0 API太原始了...我们把所有有注解的类型都扫描进去，然后在Execute的时候通过语义模型处理
            if (syntaxNode is TypeDeclarationSyntax classDecl && classDecl.AttributeLists.Count > 0) {
                typeDeclarationNodes.Add(classDecl);
            }
        }
    }

    #endregion

    #region process

    /// <summary>
    /// 不是为自己生成，当前类是Codec配置类，为绑定的类型生成
    /// </summary>
    private void ProcessLinkerBean(INamedTypeSymbol linkerBeanType, AttributeData linkerBeanAttribute) {
        // Target是构造函数参数，而Namespace是属性参数
        INamedTypeSymbol targetType = (INamedTypeSymbol)linkerBeanAttribute.ConstructorArguments[0].Value;
        string outNamespace = GetOutputNamespace(linkerBeanType, linkerBeanAttribute);
        AptClassProps aptClassProps = AptClassProps.Parse(linkerBeanAttribute);

        // 创建模拟数据
        Context context = new Context(targetType, linkerBeanType);
        context.outputNamespace = outNamespace;
        context.aptClassProps = aptClassProps;
        context.additionalAnnotations = GetAdditionalAnnotations(aptClassProps);
        CacheFields(context);
        CacheFieldProps(context);
        // 修正字段的Props —— 将LinkerBean上的注解信息转移到目标类
        {
            Context linkerBeanContext = new Context(linkerBeanType, null);
            CacheFields(linkerBeanContext);
            CacheFieldProps(linkerBeanContext);

            // 由于FieldKey包含了声明字段的类型，因此LinkerBean无法直接映射，我们只能按字段的简单名匹配
            foreach (AptFieldInfo fieldInfo in context.allFields) {
                AptFieldProps? fieldProps = linkerBeanContext.FindFieldProps(fieldInfo.Name);
                if (fieldProps != null) {
                    context.fieldPropsMap[fieldInfo] = fieldProps;
                }
            }
        }
        // 绑定CodecProxy
        {
            aptClassProps.codecProxyType = linkerBeanType;
            aptClassProps.codecProxyClassName = AptUtils.ParseType(linkerBeanType);
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
    private void ProcessLinkerGroup(INamedTypeSymbol linkerGroupType, AttributeData linkerGroupAttribute) {
        string outNamespace = GetOutputNamespace(linkerGroupType, linkerGroupAttribute);
        IEnumerable<IFieldSymbol> linkerGroupFields = BeanUtils
            .GetAllMembersWithInherit(linkerGroupType, new List<SymbolKind>() { SymbolKind.Field })
            .Cast<IFieldSymbol>();
        //
        foreach (IFieldSymbol fieldSymbol in linkerGroupFields) {
            // 检查类型合法性
            INamedTypeSymbol targetType = fieldSymbol.Type as INamedTypeSymbol;
            if (targetType == null) continue;
            // 查找字段的配置
            AttributeData linkerAttribute = AptUtils.GetAttribute(fieldSymbol.GetAttributes(), CNAME_CODEC_LINKER);
            AptClassProps aptClassProps = AptClassProps.Parse(linkerAttribute);
            // 泛型字段需要转换为泛型定义类 -- 不能连接到特殊类型
            if (targetType.IsGenericType) {
                targetType = targetType.OriginalDefinition;
            }
            // 创建模拟数据
            Context context = new Context(targetType, fieldSymbol);
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

    private void ProcessDirectType(INamedTypeSymbol typeSymbol, AttributeData serializableAttribute) {
        Context context = new Context(typeSymbol, null);
        CacheFields(context);
        CacheFieldProps(context);
        context.aptClassProps = AptClassProps.Parse(serializableAttribute);
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
        string outputNamespace = context.outputNamespace ?? type.ContainingNamespace.ToDisplayString();
        CsharpFile csharpFile = CsharpFile.NewBuilder(context.typeBuilder.name)
            .AddSpec(new MacroSpec("pragma", "warning disable CS1591"))
            .AddSpec(NamespaceSpec.Of(outputNamespace, context.typeBuilder.Build()))
            .Build();

        _codeWriter.Reset();
        _codeWriter.IndentInsideNamespace = false;
        sourceProductionContext.AddSource(context.typeBuilder.name,
            _codeWriter.Write(csharpFile));
    }

    private void CacheFields(Context context) {
        context.allMembers = BeanUtils.GetAllMembersWithInherit(context.type);
        // 反射字段--第三方程序集字段
        Dictionary<FieldKey, FieldInfo> reflectionFieldDic = new();
        List<MemberInfo> reflectionMembers = GetReflectionMembers(context.type);
        foreach (MemberInfo memberInfo in reflectionMembers) {
            if (memberInfo.MemberType != MemberTypes.Field) continue;
            FieldInfo fieldInfo = (FieldInfo)memberInfo;
            if (fieldInfo.IsStatic) continue;
            // 检查属性 -- 属性类型和字段类型不同的跳过
            PropertyInfo propertyInfo = BeanUtils.FindProperty(fieldInfo.Name, reflectionMembers);
            if (propertyInfo != null && propertyInfo.PropertyType != fieldInfo.FieldType) {
                continue;
            }
            var fieldKey = new FieldKey(Util.GetSimpleName(fieldInfo.DeclaringType!), fieldInfo.Name);
            reflectionFieldDic.Add(fieldKey, fieldInfo);
        }
        // 编译字段--当前程序集字段
        Dictionary<FieldKey, IFieldSymbol> compilationFieldDic = new();
        foreach (ISymbol symbol in context.allMembers) {
            if (symbol.Kind != SymbolKind.Field || symbol.IsStatic) continue;
            IFieldSymbol fieldSymbol = (IFieldSymbol)symbol;
            if (!IsBuildingAssemblyNode(fieldSymbol.ContainingType)) {
                continue;
            }
            // 检查属性 -- 属性类型和字段类型不同的跳过
            IPropertySymbol propertySymbol = BeanUtils.FindProperty(fieldSymbol.Name, context.allMembers);
            if (propertySymbol != null && !fieldSymbol.Type.IsSameType(propertySymbol.Type)) {
                continue;
            }
            FieldKey key = new FieldKey(fieldSymbol.ContainingType.Name, fieldSymbol.Name);
            compilationFieldDic.Add(key, fieldSymbol);
        }
        // 合并信息
        HashSet<FieldKey> fieldKeys = new HashSet<FieldKey>();
        fieldKeys.AddAll(reflectionFieldDic.Keys);
        fieldKeys.AddAll(compilationFieldDic.Keys);

        List<AptFieldInfo> allFields = new List<AptFieldInfo>(fieldKeys.Count);
        foreach (FieldKey key in fieldKeys) {
            reflectionFieldDic.TryGetValue(key, out FieldInfo? fieldInfo);
            compilationFieldDic.TryGetValue(key, out IFieldSymbol? fieldSymbol);
            // props只需要访问public权限的，因此无需特殊处理
            IPropertySymbol propertySymbol = BeanUtils.FindProperty(key.fieldName, context.allMembers);

            AptFieldInfo aptFieldInfo = new AptFieldInfo(fieldInfo, fieldSymbol, propertySymbol);
            if (aptFieldInfo.FieldType != null) {
                aptFieldInfo.typeName = AptUtils.ParseType(aptFieldInfo.FieldType).RemoveAllNullableAttribute();
            }
            allFields.Add(aptFieldInfo);
        }
        context.allFields = allFields;
    }

    private List<MemberInfo> GetReflectionMembers(INamedTypeSymbol typeSymbol) {
        string assemblyName = GetThirdPartyAssemblyName(typeSymbol, out INamedTypeSymbol thirdPartyType);
        if (assemblyName == null) {
            return new List<MemberInfo>();
        }
        string typePath = $"{AptUtils.GetFullMetadataName(thirdPartyType!)}, {assemblyName}";
        Type reflectType = Type.GetType(typePath, false);
        if (reflectType == null) {
            return new List<MemberInfo>();
        }
        return BeanUtils.GetAllMembersWithInherit(reflectType, MemberTypes.Field | MemberTypes.Property)
            .ToList();
    }

    /** 返回Null表示没有依赖的第三方程序集 */
    private string? GetThirdPartyAssemblyName(INamedTypeSymbol typeSymbol,
                                              out INamedTypeSymbol? thirdPartyType) {
        int index = 0;
        List<INamedTypeSymbol> namedTypeSymbols = AptUtils.FlatInherit(typeSymbol);
        for (; index < namedTypeSymbols.Count; index++) {
            INamedTypeSymbol namedTypeSymbol = namedTypeSymbols[index];
            string typeAssemblyName = namedTypeSymbol.ContainingAssembly.Name;
            if (typeAssemblyName != buildingAssemblyName) {
                break;
            }
        }
        if (index < namedTypeSymbols.Count) {
            INamedTypeSymbol namedTypeSymbol = namedTypeSymbols[index];
            thirdPartyType = namedTypeSymbol;
            return namedTypeSymbol.ContainingAssembly.Name;
        }
        thirdPartyType = null;
        return null;
    }

    private void CacheFieldProps(Context context) {
        foreach (AptFieldInfo fieldInfo in context.allFields) {
            if (fieldInfo.FieldType == null) {
                context.fieldPropsMap[fieldInfo] = new AptFieldProps();
                continue;
            }
            // dson-property
            AptFieldProps aptFieldProps = AptFieldProps.Parse(fieldInfo, CNAME_PROPERTY,
                type_NumberStyle, type_StringStyle, type_ObjectStyle, compilation);
            // dson-ignore
            aptFieldProps.ParseIgnore(fieldInfo, CNAME_DSON_IGNORE);
            //
            context.fieldPropsMap[fieldInfo] = aptFieldProps;
        }
    }

    /** 获取输出命名空间 -- 默认为配置类的命名空间 */
    private string GetOutputNamespace(INamedTypeSymbol configType, AttributeData attributeData) {
        // Namespace是属性参数
        if (AptUtils.GetAttributeValue(attributeData, MNAME_OUTPUT, out TypedConstant typedConstant)) {
            return typedConstant.GetValueAsString();
        }
        return configType.ContainingNamespace.ToDisplayString();
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
        foreach (ITypeParameterSymbol typeParameter in type.TypeParameters) {
            context.typeBuilder.AddTypeParameter(AptUtils.CopyTypeParameter(typeParameter));
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
        CheckThirdPartyAssembly(targetType, context.linkerSymbol);

        List<ISymbol> allMembers = context.allMembers;
        foreach (AptFieldInfo fieldInfo in context.allFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (!IsSerializableField(fieldInfo, aptFieldProps!)) {
                continue;
            }
            context.serialFields.Add(fieldInfo);

            if (IsAutoWriteField(fieldInfo, aptClassProps, aptFieldProps)) {
                CheckAutoWriteField(fieldInfo, aptFieldProps, allMembers);
            }
            if (IsAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                CheckAutoReadField(fieldInfo, aptFieldProps, allMembers);
            }
        }
    }

    /** 检查自动读字段 */
    private void CheckAutoReadField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps, List<ISymbol> allMembers) {
        if (!string.IsNullOrWhiteSpace(aptFieldProps.readProxy)) {
            return;
        }
        // 工具读：需要是public或包含public setter
        if (!CanSetDirectly(fieldInfo)
            && string.IsNullOrWhiteSpace(aptFieldProps.setter)
            && !fieldInfo.HasPublicSetter) {
            // 由于可能是超类的字段，symbol可能为null，所以格式化文本中追加字段名
            ReportDiagnostic(DiagnosticSeverity.Error, fieldInfo.fieldSymbol, 1001,
                "auto read field {0} must be public or contains a public setter",
                fieldInfo.Name);
        }
    }

    /** 检查自动写字段 */
    private void CheckAutoWriteField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps, List<ISymbol> allMembers) {
        if (!string.IsNullOrWhiteSpace(aptFieldProps.writeProxy)) {
            return;
        }
        // 工具写：需要是public字段或包含public getter
        if (!CanGetDirectly(fieldInfo)
            && string.IsNullOrWhiteSpace(aptFieldProps.getter)
            && !fieldInfo.HasPublicGetter) {
            // 由于可能是超类的字段，symbol可能为null，所以格式化文本中追加字段名
            ReportDiagnostic(DiagnosticSeverity.Error, fieldInfo.fieldSymbol, 1002,
                "auto write field {0} must be public or contains a public getter",
                fieldInfo.Name);
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
        if (ContainsNoArgsConstructor(typeSymbol)
            || ContainsReaderConstructor(typeSymbol)
            || ContainsNewInstanceMethod(typeSymbol)) {
            return;
        }
        //
        ReportDiagnostic(DiagnosticSeverity.Error, typeSymbol, 1003,
            "SerializableClass must contains public no-args constructor or reader-args constructor!");
    }

    private void CheckThirdPartyAssembly(INamedTypeSymbol typeSymbol, ISymbol? linkerSymbol) {
        string assemblyName = GetThirdPartyAssemblyName(typeSymbol, out INamedTypeSymbol thirdPartyType);
        if (assemblyName == null) {
            return;
        }
        // 其实可以测试一下是否是系统库，但在Unity下可能兼容性不够好
        if (thirdPartyType!.SpecialType != SpecialType.None) {
            return;
        }
        string typePath = $"{AptUtils.GetFullMetadataName(thirdPartyType!)}, {assemblyName}";
        if (Type.GetType(typePath, false) == null) {
            ReportDiagnostic(DiagnosticSeverity.Warning, linkerSymbol, 1004,
                "The assembly '{0}' of '{1}' cannot be loaded, the generated codec maybe partial",
                assemblyName, thirdPartyType!.Name);
        }
    }

    #endregion

    #region 钩子查询

    /** 是否包含无参构造方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsNoArgsConstructor(INamedTypeSymbol typeSymbol) {
        IMethodSymbol constructor = BeanUtils.GetNoArgsConstructor(typeSymbol);
        return constructor != null && constructor.IsPublic();
    }

    /** 是否包含 T(Reader reader) 构造方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsReaderConstructor(INamedTypeSymbol typeSymbol) {
        IMethodSymbol constructor = BeanUtils.GetOneArgsConstructor(typeSymbol, type_DsonReader);
        return constructor != null && constructor.IsPublic();
    }

    /** 是否包含 newInstance(reader) 静态解码方法 -- 只能从当前类型查询 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsNewInstanceMethod(INamedTypeSymbol typeSymbol) {
        IEnumerable<ISymbol> staticMembers = typeSymbol.GetMembers()
            .Where(e => e.IsStatic && e.Kind == SymbolKind.Method);
        return ContainsHookMethod(staticMembers, MNAME_NEW_INSTANCE, type_DsonReader);
    }

    /** 是否包含 readerObject(reader) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsReadObjectMethod(List<ISymbol> allMembers) {
        return ContainsHookMethod(allMembers, MNAME_READ_OBJECT, type_DsonReader);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsWriteObjectMethod(List<ISymbol> allMembers) {
        return ContainsHookMethod(allMembers, MNAME_WRITE_OBJECT, type_DsonWriter);
    }

    /** 是否包含 beforeEncode 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (bool contains, int argCount) ContainsBeforeEncodeMethod(List<ISymbol> allMembers) {
        if (ContainsHookMethod(allMembers, MNAME_BEFORE_ENCODE, type_Options)) {
            return (true, 1);
        }
        if (ContainsNoArgsHookMethod(allMembers, MNAME_BEFORE_ENCODE)) {
            return (true, 0);
        }
        return (false, 0);
    }

    /** 是否包含 afterDecode 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (bool contains, int argCount) ContainsAfterDecodeMethod(List<ISymbol> allMembers) {
        if (ContainsHookMethod(allMembers, MNAME_AFTER_DECODE, type_Options)) {
            return (true, 1);
        }
        if (ContainsNoArgsHookMethod(allMembers, MNAME_AFTER_DECODE)) {
            return (true, 0);
        }
        return (false, 0);
    }

    /** 是否包含指定参数的钩子方法 */
    private bool ContainsHookMethod(IEnumerable<ISymbol> allMembers, string methodName, ITypeSymbol argType) {
        return allMembers.Where(e => e.Kind == SymbolKind.Method)
            .Cast<IMethodSymbol>()
            .Any(symbol => symbol.IsPublic()
                           && symbol.Parameters.Length > 0
                           && symbol.Name == methodName
                           && symbol.Parameters[0].Type.IsSubTypeOf(argType));
    }

    /** 是否包含无参的钩子方法 */
    private bool ContainsNoArgsHookMethod(IEnumerable<ISymbol> allMembers, string methodName) {
        return allMembers.Where(e => e.Kind == SymbolKind.Method)
            .Cast<IMethodSymbol>()
            .Any(symbol => symbol.IsPublic()
                           && symbol.Parameters.Length == 0
                           && symbol.Name == methodName);
    }

    #endregion

    #region 字段检查

    /// <summary>
    /// 测试是否可以直接读取字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接取值，则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanGetDirectly(AptFieldInfo fieldInfo) {
        return fieldInfo.IsPublic;
    }

    /// <summary>
    /// 测试是否可以直接写字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接赋值，则返回true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanSetDirectly(AptFieldInfo fieldInfo) {
        if (fieldInfo.IsReadOnly) {
            return false;
        }
        return fieldInfo.IsPublic;
    }

    /**
     * 是否是可序列化的字段
     * 1.默认只序列化 public 字段
     * 2.默认忽略 <see cref="NonSerializedAttribute"/> 字段
     */
    internal bool IsSerializableField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps) {
        if (fieldInfo.FieldType == null) return false;
        if (fieldInfo.IsStatic) return false;
        // 有注解的情况取决于注解的值，需取反 -- 注解已提前解析
        if (aptFieldProps.ignore.HasValue) {
            return !aptFieldProps.ignore.Value;
        }
        // 无注解的情况下，默认忽略 NonSerialized 字段
        if (fieldInfo.GetAttribute(CNAME_NonSerialize) != null) {
            return false;
        }
        // 判断public和getter/setter
        if (fieldInfo.IsPublic) {
            return true;
        }
        // 我们在Props上缓存了关联的属性
        return fieldInfo.HasPublicSetter && fieldInfo.HasPublicGetter;
    }

    /** 是否是托管写的字段 */
    internal bool IsAutoWriteField(AptFieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.IsSingleton) {
            return false;
        }
        if (IsSkipField(fieldInfo, aptClassProps, aptFieldProps)) {
            return false;
        }
        return true;
    }

    /** 是否是托管读的字段 */
    internal bool IsAutoReadField(AptFieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
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
    private static bool IsSkipField(AptFieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.skipFields.Count == 0) {
            return false;
        }
        if (aptClassProps.skipFields.Contains("*")) {
            return true;
        }
        // 如果是自动属性，则使用属性名
        string fieldName;
        if (fieldInfo.IsAutoPropertyField) {
            fieldName = fieldInfo.propertySymbol!.Name;
        } else {
            fieldName = fieldInfo.Name;
        }
        if (aptClassProps.skipFields.Contains(fieldName)) {
            return true; // 完全匹配
        }
        if (!aptClassProps.clippedSkipFields.Contains(fieldName)) {
            return false; // 简单名不存在
        }
        // 测试类名 -- 不测试FullName，C#的FullName并不易编写
        string declaringTypeName = fieldInfo.FieldKey.ToString();
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