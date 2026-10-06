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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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

    private const string CNAME_Binary = "Wjybxx.Dson.Types.Binary";
    private const string CNAME_RefId = "Wjybxx.Dson.Types.RefId";
    private const string CNAME_Timestamp = "Wjybxx.Dson.Types.Timestamp";
    private const string CNAME_Double4 = "Wjybxx.Dson.Types.Double4";
    private const string CNAME_Long4 = "Wjybxx.Dson.Types.Long4";
    private const string CNAME_Fxp64 = "Wjybxx.Dson.Types.Fxp64";
    private const string CNAME_Fxp4 = "Wjybxx.Dson.Types.Fxp4";

    private const string CNAME_TypeInfo = "Wjybxx.Commons.TypeInfo";
    private const string CNAME_TypeName = "Wjybxx.Commons.TypeName";

    private const string CNAME_NON_SERIALIZED = "System.NonSerializedAttribute";
    private const string CNAME_UNITY_SERIALIZE_FIELD = "UnityEngine.SerializeField";
    private const string CNAME_SERIALIZE_REFERENCES = "Wjybxx.Commons.SerializeReference";
    private const string CNAME_UNITY_SERIALIZE_REFERENCES = "UnityEngine.SerializeReference";

    // dson
    private const string CNAME_SERIALIZABLE = "Wjybxx.Dson.Codec.Attributes.DsonSerializableAttribute";
    internal const string CNAME_DSON_PROPERTY = "Wjybxx.Dson.Codec.Attributes.DsonPropertyAttribute";
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
    internal const string MNAME_READ_FIELD = "ReadField";
    internal const string MNAME_SET_FIELD = "SetField";
    internal const string MNAME_AFTER_DECODE = "AfterDecode";

    internal static readonly TypeName typeName_EncodeFeatures = ClassName.Get("Wjybxx.Dson.Codec", "SerializeFeatures");
    internal static readonly TypeName typeName_DecodeFeatures = ClassName.Get("Wjybxx.Dson.Codec", "DeserializeFeatures");

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
    internal INamedTypeSymbol type_Binary;
    internal INamedTypeSymbol type_RefId;
    internal INamedTypeSymbol type_LocalDateTime;
    internal INamedTypeSymbol type_Timestamp;
    internal INamedTypeSymbol type_Double4;
    internal INamedTypeSymbol type_Long4;
    internal INamedTypeSymbol type_Fxp64;
    internal INamedTypeSymbol type_Fxp4;

    internal INamedTypeSymbol type_List;
    internal INamedTypeSymbol type_ICollection;
    internal INamedTypeSymbol type_IReadonlyCollection;
    internal INamedTypeSymbol type_IEnumerable;

    internal INamedTypeSymbol type_Dictionary;
    internal INamedTypeSymbol type_IDictionary;
    internal INamedTypeSymbol type_IReadonlyDictionary;

    private HashSet<string> immutableCollectionTypes = new HashSet<string>();
    private HashSet<string> immutableDictionaryTypes = new HashSet<string>();

    private GeneratorExecutionContext sourceProductionContext;
    private Compilation compilation;
    private string buildingAssemblyName;
    private AttributeSpec processorInfoAnnotation;

    private readonly CodeWriter _codeWriter = new CodeWriter(indent: "    ");
    private readonly Dictionary<string, Assembly> _loadedAssembly = new();

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
        anno_DsonProperty = compilation.GetTypeByMetadataName(CNAME_DSON_PROPERTY);
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
        type_Binary = compilation.GetTypeByMetadataName(CNAME_Binary);
        type_RefId = compilation.GetTypeByMetadataName(CNAME_RefId);
        type_Timestamp = compilation.GetTypeByMetadataName(CNAME_Timestamp);
        type_Double4 = compilation.GetTypeByMetadataName(CNAME_Double4);
        type_Long4 = compilation.GetTypeByMetadataName(CNAME_Long4);
        type_Fxp64 = compilation.GetTypeByMetadataName(CNAME_Fxp64);
        type_Fxp4 = compilation.GetTypeByMetadataName(CNAME_Fxp4);

        // 集合类型
        type_List = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
        type_ICollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T);
        type_IReadonlyCollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyCollection_T);
        type_IEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

        type_IDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");
        type_Dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
        type_IReadonlyDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");

        //
        immutableCollectionTypes.Add("ImmutableList`1");
        immutableCollectionTypes.Add("ImmutableSet`1");
        immutableCollectionTypes.Add("ImmutableHashSet`1");
        immutableDictionaryTypes.Add("ImmutableDictionary`2");
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
            context.linkerContext = linkerBeanContext;
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
        CsharpFile.Builder csharpFile = CsharpFile.NewBuilder(context.typeBuilder.name)
            .AddSpec(new MacroSpec("pragma", "warning disable CS1591"));
        // 导入命名空间别名，解决类型名冲突
        foreach (KeyValuePair<string, string> pair in context.aptClassProps.namespaceAliases) {
            csharpFile.AddSpec(new ImportSpec(pair.Key, pair.Value));
        }
        csharpFile.AddSpec(NamespaceSpec.Of(outputNamespace, context.typeBuilder.Build()));

        _codeWriter.Reset();
        _codeWriter.IndentInsideNamespace = false;
        sourceProductionContext.AddSource(context.typeBuilder.name,
            _codeWriter.Write(csharpFile.Build()));
    }

    private void CacheFields(Context context) {
        context.allMembers = BeanUtils.GetAllMembersWithInherit(context.type);
        // 反射字段--第三方程序集字段
        Dictionary<FieldKey, FieldInfo> reflectionFieldDic = new();
        List<MemberInfo> reflectionMembers = GetReflectionMembers(context.type, context.linkerSymbol);
        foreach (MemberInfo memberInfo in reflectionMembers) {
            if (memberInfo.MemberType != MemberTypes.Field) continue;
            FieldInfo fieldInfo = (FieldInfo)memberInfo;
            if (fieldInfo.IsStatic) continue;
            var fieldKey = new FieldKey(Util.GetSimpleName(fieldInfo.DeclaringType!), fieldInfo.Name);
            reflectionFieldDic.Add(fieldKey, fieldInfo);
        }
        // 编译字段--当前程序集字段
        Dictionary<FieldKey, IFieldSymbol> compilationFieldDic = new();
        foreach (ISymbol symbol in context.allMembers) {
            if (symbol.Kind != SymbolKind.Field || symbol.IsStatic) continue;
            IFieldSymbol fieldSymbol = (IFieldSymbol)symbol;
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
            // 字段类型和属性类型不同时忽略属性
            IPropertySymbol propertySymbol = BeanUtils.FindProperty(key.fieldName, context.allMembers);
            propertySymbol = CheckProperty(propertySymbol, fieldSymbol);
            // 只能扫描到public权限的属性，因此无法实现期望的优先通过属性反射的需求
            AptFieldInfo aptFieldInfo = new AptFieldInfo(fieldInfo, fieldSymbol, propertySymbol);
            if (aptFieldInfo.FieldType != null) {
                aptFieldInfo.typeName = AptUtils.ParseType(aptFieldInfo.FieldType).RemoveAllNullableAttribute();
            }
            allFields.Add(aptFieldInfo);
        }
        context.allFields = allFields;
    }

    // 无法准确处理第三方程序集的属性和字段验证，由用户自行处理字段类型和属性名不一致的情况（读写代理）
    private IPropertySymbol? CheckProperty(IPropertySymbol? propertySymbol, IFieldSymbol? fieldSymbol) {
        if (propertySymbol != null && fieldSymbol != null) {
            return propertySymbol.Type.IsSameType(fieldSymbol.Type) ? propertySymbol : null;
        }
        return propertySymbol;
    }

    private List<MemberInfo> GetReflectionMembers(INamedTypeSymbol typeSymbol, ISymbol linkerSymbol) {
        INamedTypeSymbol thirdPartyType = GetThirdPartyType(typeSymbol);
        if (thirdPartyType == null) {
            return new List<MemberInfo>();
        }
        string typeFullName = AptUtils.GetFullMetadataName(thirdPartyType!);
        string typePath = $"{typeFullName}, {thirdPartyType.ContainingAssembly.Name}";
        Type reflectType = Type.GetType(typePath, false);
        if (reflectType == null && (reflectType = TryLoadThirdPartyType(thirdPartyType, typeFullName)) == null) {
            ReportDiagnostic(DiagnosticSeverity.Warning, linkerSymbol, 1004,
                "The assembly '{0}' of '{1}' cannot be loaded, the generated codec maybe partial",
                thirdPartyType.ContainingAssembly.Name, thirdPartyType!.Name);
            return new List<MemberInfo>();
        }
        return BeanUtils.GetAllMembersWithInherit(reflectType, MemberTypes.Field | MemberTypes.Property)
            .ToList();
    }

    private Type TryLoadThirdPartyType(ITypeSymbol thirdPartyType, string typeFullName) {
        IAssemblySymbol assemblySymbol = thirdPartyType.ContainingAssembly;
        if (_loadedAssembly.TryGetValue(assemblySymbol.Name, out Assembly? assembly)) {
            return assembly?.GetType(typeFullName, false);
        }
        assembly = AptUtils.TryLoadAssembly(compilation, assemblySymbol);
        _loadedAssembly[assemblySymbol.Name] = assembly; // 避免总是尝试加载
        return assembly?.GetType(typeFullName, false);
    }

    /** 返回Null表示没有依赖的第三方程序集 */
    private INamedTypeSymbol? GetThirdPartyType(INamedTypeSymbol typeSymbol) {
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
            return namedTypeSymbols[index];
        }
        return null;
    }

    private void CacheFieldProps(Context context) {
        foreach (AptFieldInfo fieldInfo in context.allFields) {
            if (fieldInfo.FieldType == null) {
                context.fieldPropsMap[fieldInfo] = new AptFieldProps();
                continue;
            }
            // dson-property
            AptFieldProps aptFieldProps = AptFieldProps.Parse(fieldInfo, CNAME_DSON_PROPERTY, compilation);
            // dson-ignore
            aptFieldProps.ParseIgnore(fieldInfo, CNAME_DSON_IGNORE);
            // serialize-reference
            aptFieldProps.ParseSerializeReference(fieldInfo, CNAME_SERIALIZE_REFERENCES);
            if (aptFieldProps.serializeReference == null) {
                aptFieldProps.ParseSerializeReference(fieldInfo, CNAME_UNITY_SERIALIZE_REFERENCES);
            }
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
        CheckConstructor(context);

        foreach (AptFieldInfo fieldInfo in context.allFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (!IsSerializableField(fieldInfo, aptFieldProps!)) {
                continue;
            }
            context.serialFields.Add(fieldInfo);
            CheckSerializeField(context, fieldInfo);
        }
    }

    // 新版本不再校验字段访问权限，不可正常读写时通过反射操作 - 但值类型不可以包含需要反射读写的字段
    // 值类型的List/Dictionary使用序列化引用时，只能使用原始的List和Dictionary，不能产生延迟转换需求
    private void CheckSerializeField(Context context, AptFieldInfo fieldInfo) {
        if (context.type.IsValueType) {
            bool genGetter = !CanGetDirectly(fieldInfo) && !fieldInfo.HasPublicGetter;
            bool genSetter = !CanSetDirectly(fieldInfo) && !fieldInfo.HasPublicSetter;
            if (genGetter || genSetter) {
                ISymbol symbol = fieldInfo.fieldSymbol ?? (ISymbol)context.type;
                ReportDiagnostic(DiagnosticSeverity.Error, symbol, 1002, "值类型不可以包含需要反射操作的字段!");
            }
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void CheckConstructor(Context context) {
        INamedTypeSymbol typeSymbol = context.type;
        if (typeSymbol.IsAbstract || typeSymbol.IsValueType) {
            return;
        }
        // 静态代理包含NewInstance方法
        if (context.linkerContext != null
            && context.linkerContext.ContainsHookMethod(MNAME_NEW_INSTANCE)) {
            return;
        }
        if (ContainsNoArgsConstructor(typeSymbol)
            || ContainsReaderConstructor(typeSymbol)
            || ContainsNewInstanceMethod(typeSymbol)) {
            return;
        }
        //
        ReportDiagnostic(DiagnosticSeverity.Error, typeSymbol, 1003, "要序列化的类型必须包含无参构造函数或解码构造函数");
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

    /** 是否包含 ReadFields(reader) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsReadFieldsMethod(List<ISymbol> allMembers) {
        return ContainsHookMethod(allMembers, MNAME_READ_FIELDS, type_DsonReader);
    }

    /** 是否包含 ReadField(reader, name) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsReadFieldMethod(List<ISymbol> allMembers) {
        return ContainsHookMethod(allMembers, MNAME_READ_FIELD, type_DsonReader);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsWriteObjectMethod(List<ISymbol> allMembers) {
        return ContainsHookMethod(allMembers, MNAME_WRITE_OBJECT, type_DsonWriter);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ContainsWriteFieldsMethod(List<ISymbol> allMembers) {
        return ContainsHookMethod(allMembers, MNAME_WRITE_FIELDS, type_DsonWriter);
    }

    /** 是否包含 beforeEncode 实例方法 - 返回值表示方法参数个数，-1表示不包含钩子 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int ContainsBeforeEncodeMethod(List<ISymbol> allMembers) {
        if (ContainsHookMethod(allMembers, MNAME_BEFORE_ENCODE, type_Options)) {
            return 1;
        }
        if (ContainsNoArgsHookMethod(allMembers, MNAME_BEFORE_ENCODE)) {
            return 0;
        }
        return -1;
    }

    /** 是否包含 afterDecode 实例方法 - 返回值表示方法参数个数，-1表示不包含钩子 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int ContainsAfterDecodeMethod(List<ISymbol> allMembers) {
        if (ContainsHookMethod(allMembers, MNAME_AFTER_DECODE, type_Options)) {
            return 1;
        }
        if (ContainsNoArgsHookMethod(allMembers, MNAME_AFTER_DECODE)) {
            return 0;
        }
        return -1;
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
        if (fieldInfo.GetAttribute(CNAME_NON_SERIALIZED) != null) {
            return false;
        }
        // 有DsonProperty注解也视作需要序列化, Unity项目的话还需要包括SerializeField
        if (fieldInfo.GetAttribute(CNAME_DSON_PROPERTY) != null
            || fieldInfo.GetAttribute(CNAME_UNITY_SERIALIZE_FIELD) != null) {
            return true;
        }
        // 判断public和getter/setter
        if (fieldInfo.IsPublic) {
            return true;
        }
        // 我们在FieldInfo上缓存了关联的属性
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
            return true;
        }
        // 测试类名 -- 不测试FullName，C#的FullName并不易编写
        string declaringTypeName = fieldInfo.FieldKey.ToString();
        if (aptClassProps.skipFields.Contains(declaringTypeName + "." + fieldName)) {
            return true;
        }
        return false;
    }

    internal bool IsCollection(ITypeSymbol type) {
        type = type.OriginalDefinition;
        return type.IsSameType(type_List) || type.IsSubTypeOf(type_ICollection)
                                          || type.IsSubTypeOf(type_IReadonlyCollection);
    }

    internal bool IsDictionary(ITypeSymbol type) {
        type = type.OriginalDefinition;
        return type.IsSameType(type_Dictionary) || type.IsSubTypeOf(type_IDictionary)
                                                || type.IsSubTypeOf(type_IReadonlyDictionary);
    }

    internal bool IsImmutableCollection(ITypeSymbol type) {
        type = type.OriginalDefinition;
        return immutableCollectionTypes.Contains(type.MetadataName);
    }

    internal bool IsImmutableDictionary(ITypeSymbol type) {
        type = type.OriginalDefinition;
        return immutableDictionaryTypes.Contains(type.MetadataName);
    }

    #endregion

    #region overring util

    public MethodSpec NewGetEncoderTypeMethod(INamedTypeSymbol superDeclaredType, TypeName encoderTypeName) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_GET_ENCODER_TYPE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        // 需要处理泛型
        return AptUtils.Overriding(methodInfo)
            .Code(CodeBlock.Of("typeof($T)", encoderTypeName).WithExpressionStyle())
            .Build();
    }

    public MethodSpec.Builder NewNewInstanceMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_NEW_INSTANCE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewReadObjectMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_READ_OBJECT);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewReadFieldsMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_READ_FIELDS);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewSetFieldMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_SET_FIELD);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    internal INamedTypeSymbol GetTypeSymbol(string metadataName) {
        return compilation.GetTypeByMetadataName(metadataName)
               ?? throw new InvalidOperationException($"找不到类型：{metadataName}");
    }

    public MethodSpec.Builder NewReadFieldMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_READ_FIELD);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewAfterDecodeMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_AFTER_DECODE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewBeforeEncodeMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_BEFORE_ENCODE);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewWriteObjectMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_WRITE_OBJECT);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewWriteFieldsMethodBuilder(INamedTypeSymbol superDeclaredType) {
        IMethodSymbol? methodInfo = GetFirstVirtualMethod(superDeclaredType, MNAME_WRITE_FIELDS);
        if (methodInfo == null) {
            throw new InvalidOperationException();
        }
        return AptUtils.Overriding(methodInfo);
    }

    private static IMethodSymbol GetFirstVirtualMethod(ITypeSymbol typeSymbol, string name) {
        foreach (ISymbol member in typeSymbol.GetMembers()) {
            if (member.Kind == SymbolKind.Method && (member.IsVirtual || member.IsAbstract)
                                                 && member.Name == name) {
                return (IMethodSymbol?)member;
            }
        }
        return null;
    }

    #endregion
}
}