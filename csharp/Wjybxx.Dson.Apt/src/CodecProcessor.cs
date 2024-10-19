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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using static System.Reflection.BindingFlags;
using ClassName = Wjybxx.Commons.Poet.ClassName;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// <see cref="DsonSerializableAttribute"/>注解处理器
///
/// 最终序列化的都是字段，自动属性只是定义字段的快捷方法，自动属性字段的编码名默认为属性名。
/// </summary>
public class CodecProcessor
{
    internal const string MNAME_READ_OBJECT = "ReadObject";
    internal const string MNAME_WRITE_OBJECT = "WriteObject";

    internal const string MNAME_GET_ENCODER_TYPE = "GetEncoderType";
    internal const string MNAME_BEFORE_ENCODE = "BeforeEncode";
    internal const string MNAME_WRITE_FIELDS = "WriteFields";
    internal const string MNAME_NEW_INSTANCE = "NewInstance";
    internal const string MNAME_READ_FIELDS = "ReadFields";
    internal const string MNAME_AFTER_DECODE = "AfterDecode";

    /// <summary>
    /// 要处理的所有类型
    /// (至于是扫描程序集还是怎么样，由用户自行处理)
    /// </summary>
    public readonly IList<Type> assemblyTypes;
    /// <summary>
    /// 生成的c#文件的输出目录
    /// </summary>
    public readonly string csharpFileOutDir;
    /// <summary>
    /// 文件头
    /// </summary>
    public readonly IList<ISpecification> fileHeader;
    /// <summary>
    /// 导出导出Codec信息的类名
    /// </summary>
    public ClassName? codecExporterClassName;
#nullable disable

    #region cache

    private readonly AttributeSpec processorInfoAnnotation = AptUtils.NewProcessorInfoAnnotation(typeof(CodecProcessor));

    // region 字段
    internal ClassName typeName_WireType;
    internal ClassName typeName_NumberStyle;
    internal ClassName typeName_StringStyle;
    internal ClassName typeName_ObjectStyle;
    internal Type type_Options; // ConverterOptions

    // Dson
    internal Type anno_DsonSerializable;
    internal Type anno_DsonProperty;
    internal Type anno_DsonIgnore;
    internal Type type_DsonReader;
    internal Type type_DsonWriter;

    // linker
    internal Type anno_CodecLinkerGroup;
    internal Type anno_CodecLinker;
    internal Type anno_CodecLinkerBean;

    // abstractCodec
    internal Type type_DsonCodec;
    internal Type type_AbstractCodec;

    private readonly CodeWriter _codeWriter = new CodeWriter();
    private readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false);
    /** 用于生成GenericCodecConfig */
    private readonly Dictionary<ClassName, ClassName> _type2CodecNames = new Dictionary<ClassName, ClassName>();

    #endregion

#nullable enable

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyTypes">要处理的类型</param>
    /// <param name="csharpFileOutDir">CS文件输出牡蛎</param>
    /// <param name="fileHeader">文件头</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CodecProcessor(IList<Type> assemblyTypes,
                          string csharpFileOutDir,
                          IList<ISpecification>? fileHeader = null) {
        if (assemblyTypes == null) throw new ArgumentNullException(nameof(assemblyTypes));
        this.csharpFileOutDir = csharpFileOutDir ?? throw new ArgumentNullException(nameof(csharpFileOutDir));
        this.assemblyTypes = assemblyTypes.ToImmutableList2();
        this.fileHeader = fileHeader ?? ImmutableList<ISpecification>.Empty;
    }

    public bool EnableAutoImport {
        get => _codeWriter.EnableAutoImport;
        set => _codeWriter.EnableAutoImport = value;
    }

    public bool EnableFileScopedNamespace {
        get => _codeWriter.EnableFileScopedNamespace;
        set => _codeWriter.EnableFileScopedNamespace = value;
    }

    public bool IndentInsideNamespace {
        get => _codeWriter.IndentInsideNamespace;
        set => _codeWriter.IndentInsideNamespace = value;
    }

    #region Init

    private void Init() {
        // common
        typeName_WireType = ClassName.Get(typeof(WireType));
        typeName_NumberStyle = ClassName.Get(typeof(NumberStyles));
        typeName_StringStyle = ClassName.Get(typeof(StringStyle));
        typeName_ObjectStyle = ClassName.Get(typeof(ObjectStyle));
        type_Options = typeof(ConverterOptions);

        // dson
        anno_DsonSerializable = typeof(DsonSerializableAttribute);
        anno_DsonProperty = typeof(DsonPropertyAttribute);
        anno_DsonIgnore = typeof(DsonIgnoreAttribute);
        type_DsonReader = typeof(IDsonObjectReader);
        type_DsonWriter = typeof(IDsonObjectWriter);

        // linker
        anno_CodecLinkerGroup = typeof(DsonCodecLinkerGroupAttribute);
        anno_CodecLinker = typeof(DsonCodecLinkerAttribute);
        anno_CodecLinkerBean = typeof(DsonCodecLinkerBeanAttribute);

        // Codec
        type_DsonCodec = typeof(IDsonCodec<>);
        type_AbstractCodec = typeof(AbstractDsonCodec<>);
    }

    private MethodInfo FindCodecMethod(List<MethodInfo> allMethodsWithInherit, string methodName, Type readerWriterType) {
        foreach (MethodInfo methodInfo in allMethodsWithInherit) {
            if (methodInfo.Name != methodName) {
                continue;
            }
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length > 0 && parameterInfos[0].ParameterType == readerWriterType) {
                return methodInfo;
            }
        }
        throw new IllegalStateException($"method {methodName} is absent");
    }

    #endregion

    /// <summary>
    /// 执行处理
    /// </summary>
    public void Process() {
        if (!Directory.Exists(csharpFileOutDir)) {
            throw new IllegalStateException($"out dir: {csharpFileOutDir} is absent");
        }
        Init();
        foreach (Type type in assemblyTypes) {
            Debug.Assert(!type.IsConstructedGenericType);
            try {
                Context? context = TryCreateContext(type);
                if (context == null) {
                    continue;
                }
                // 判断是哪类注解 -- LinkerBean外部代理优先级最高
                if (context.linkerBeanAttribute != null) {
                    // 不是为自己生成，当前类是Codec配置类
                    ProcessLinkerBean(context);
                } else if (context.linkerGroupAttribute != null) {
                    // 不是为自己生成，而是为字段类型生成
                    ProcessLinkerGroup(context);
                } else {
                    ProcessDirectType(context);
                }

                context.aptClassProps = new AptClassProps();
                context.aptClassProps.attribute = context.dsonSerilAttribute;
            }
            catch (Exception e) {
                throw new Exception($"type: {type}", e);
            }
        }
        if (codecExporterClassName != null) {
            GenCodecExporter();
        }
    }

    private Context? TryCreateContext(Type type) {
        IEnumerable<Attribute> attributes = type.GetCustomAttributes();
        foreach (Attribute attribute in attributes) {
            if (attribute is DsonCodecLinkerBeanAttribute linkerBeanAttribute) {
                Context context = new Context(type);
                context.linkerBeanAttribute = linkerBeanAttribute;
                return context;
            }
            if (attribute is DsonCodecLinkerGroupAttribute linkerGroupAttribute) {
                Context context = new Context(type);
                context.linkerGroupAttribute = linkerGroupAttribute;
                return context;
            }
            // 由于使用了继承，DsonSerializableAttribute要放最后
            if (attribute is DsonSerializableAttribute serializableAttribute) {
                Context context = new Context(type);
                context.dsonSerilAttribute = serializableAttribute;
                return context;
            }
        }
        return null;
    }

    #region process

    private void ProcessLinkerBean(Context linkerBeanContext) {
        DsonCodecLinkerBeanAttribute linkerBeanAttribute = linkerBeanContext.linkerBeanAttribute;
        string outNamespace = GetOutputNamespace(linkerBeanContext.type, linkerBeanAttribute.OutputNamespace);

        // 真实需要生成Codec的类型
        Type targetType = linkerBeanAttribute.Target;
        AptClassProps aptClassProps = AptClassProps.Parse(linkerBeanAttribute);

        // 创建模拟数据
        Context context = new Context(targetType);
        context.linkerBeanAttribute = linkerBeanAttribute;
        context.outputNamespace = outNamespace;

        context.aptClassProps = aptClassProps;
        context.additionalAnnotations = GetAdditionalAnnotations(aptClassProps);
        CacheFields(context);
        // 修正字段的Props —— 将LinkerBean上的注解信息转移到目标类
        {
            CacheFields(linkerBeanContext);
            CacheFieldProps(linkerBeanContext);

            // 按name缓存，提高效率
            Dictionary<string, AptFieldProps> fieldName2FieldPropsMap = new Dictionary<string, AptFieldProps>(linkerBeanContext.fieldPropsMap.Count);
            foreach (KeyValuePair<FieldInfo, AptFieldProps> pair in linkerBeanContext.fieldPropsMap) {
                fieldName2FieldPropsMap[pair.Key.Name] = pair.Value;
            }
            foreach (FieldInfo fieldInfo in context.allFields) {
                if (fieldName2FieldPropsMap.TryGetValue(fieldInfo.Name, out AptFieldProps? aptFieldProps)) {
                    context.fieldPropsMap[fieldInfo] = aptFieldProps;
                } else {
                    context.fieldPropsMap[fieldInfo] = new AptFieldProps();
                }
            }
        }
        // 绑定CodecProxy
        {
            aptClassProps.codecProxyType = linkerBeanContext.type;
            aptClassProps.codecProxyClassName = TypeName.Get(linkerBeanContext.type);
            aptClassProps.codecProxyEnclosedElements = BeanUtils.GetAllMemberWithInherit(linkerBeanContext.type);
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

    private void ProcessLinkerGroup(Context groupContext) {
        DsonCodecLinkerGroupAttribute groupAttribute = groupContext.linkerGroupAttribute;
        string outNamespace = GetOutputNamespace(groupContext.type, groupAttribute.OutputNamespace);

        CacheFields(groupContext);
        foreach (FieldInfo fieldInfo in groupContext.allFields) {
            DsonCodecLinkerAttribute? linkerAttribute = fieldInfo.GetCustomAttributes()
                .FirstOrDefault(e => e is DsonCodecLinkerAttribute) as DsonCodecLinkerAttribute;
            if (linkerAttribute == null) {
                linkerAttribute = new DsonCodecLinkerAttribute();
            }
            // 泛型字段需要转换为泛型定义类
            Type targetType = fieldInfo.FieldType.IsGenericType
                ? fieldInfo.FieldType.GetGenericTypeDefinition()
                : fieldInfo.FieldType;

            AptClassProps aptClassProps = AptClassProps.Parse(linkerAttribute);
            // 创建模拟数据
            Context context = new Context(targetType);
            context.linkerGroupAttribute = groupAttribute;
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

    private void ProcessDirectType(Context context) {
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

    private void GenericCodec(Context context) {
        Type type = context.type; // C#不需要处理Enum
        Type superDeclaredType = type_AbstractCodec.MakeGenericType(type);
        InitTypeBuilder(context, type, superDeclaredType);

        SchemaGenerator schemaGenerator = new SchemaGenerator(this, context);
        schemaGenerator.Execute();

        PojoCodecGenerator codecGenerator = new PojoCodecGenerator(this, context);
        codecGenerator.Execute();

        // 写入文件
        string outputNamespace = GetOutputNamespace(type, context.outputNamespace);
        CsharpFile csharpFile = CsharpFile.NewBuilder(context.typeBuilder.name)
            .AddSpecs(fileHeader)
            .AddSpec(new MacroSpec("pragma", "warning disable CS1591"))
            .AddSpec(NamespaceSpec.Of(outputNamespace, context.typeBuilder.Build()))
            .Build();

        _codeWriter.Reset();
        File.WriteAllText(csharpFileOutDir + "/" + csharpFile.name + ".cs",
            _codeWriter.Write(csharpFile),
            _utf8Encoding);
    }

    private void CacheFields(Context context) {
        context.allFieldsAndMethodWithInherit =
            BeanUtils.GetAllMemberWithInherit(context.type);
        // 包含自动属性字段
        context.allFields = BeanUtils.GetAllFieldsWithInherit(context.type)
            .Where(e => !BeanUtils.IsStaticMember(e))
            .ToList();
    }

    private void CacheFieldProps(Context context) {
        foreach (FieldInfo fieldInfo in context.allFields) {
            // 最终序列化的都是字段，自动属性是定义字段的快捷方法
            MemberInfo attributeHolder;
            if (BeanUtils.IsAutoPropertyField(fieldInfo)) {
                attributeHolder = BeanUtils.FindProperty(fieldInfo, context.allFieldsAndMethodWithInherit)!;
            } else {
                attributeHolder = fieldInfo;
            }
            AptFieldProps aptFieldProps = AptFieldProps.Parse(attributeHolder);
            aptFieldProps.ParseIgnore(attributeHolder);

            aptFieldProps.autoProperty = attributeHolder as PropertyInfo;
            context.fieldPropsMap[fieldInfo] = aptFieldProps;
        }
    }

    /** 获取输出命名空间 -- 默认为配置类的命名空间 */
    private string GetOutputNamespace(Type type, string? outNamespace) {
        if (string.IsNullOrWhiteSpace(outNamespace)) {
            return type.Namespace ?? throw new Exception();
        }
        return outNamespace;
    }

    /** 获取为生成的Codec附加的注解 */
    private List<AttributeSpec> GetAdditionalAnnotations(AptClassProps aptClassProps) {
        Type[] attributes = aptClassProps.attribute.Attributes;
        List<AttributeSpec> result = new List<AttributeSpec>(attributes.Length);
        foreach (Type attribute in attributes) {
            ClassName className = ClassName.Get(attribute);
            result.Add(AttributeSpec.NewBuilder(className)
                .Build());
        }
        return result;
    }

    private void InitTypeBuilder(Context context, Type type, Type superDeclaredType) {
        context.superDeclaredType = superDeclaredType;
        context.typeBuilder = TypeSpec.NewClassBuilder(GetCodecName(type))
            .AddModifiers(Modifiers.Public | Modifiers.Sealed) // 禁止手写类重写生成类
            .AddAttribute(processorInfoAnnotation)
            .AddBaseClass(superDeclaredType);
        // 拷贝泛型参数 -- Codec泛型参数和原始类型泛型参数相同
        ClassName srcClassName = ClassName.Get(type);
        List<TypeName> emptyTypeVars = new List<TypeName>();
        foreach (TypeName typeArgument in srcClassName.typeArguments) {
            context.typeBuilder.AddTypeVariable((TypeVariableName)typeArgument);
            emptyTypeVars.Add(emptyTypeVariableName);
        }
        // 保存类型映射
        {
            ClassName srcGenericDefine = srcClassName.WithTypeVariables(emptyTypeVars.ToArray());
            ClassName codecGenericDefine = ClassName.Get(GetOutputNamespace(type, context.outputNamespace), context.typeBuilder.name, emptyTypeVars);
            _type2CodecNames[srcGenericDefine] = codecGenericDefine;
        }
    }

    private string GetCodecName(Type type) {
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
        if (aptClassProps.IsSingleton()) {
            return;
        }
        Type targetType = context.type;
        CheckConstructor(targetType);

        List<MemberInfo> allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
        List<MemberInfo> instMethodWithInherit = allFieldsAndMethodWithInherit
            .Where(e => (e.MemberType & MemberTypes.Method) != 0 || (e.MemberType & MemberTypes.Property) != 0)
            .Where(e => !BeanUtils.IsStaticMember(e))
            .ToList();

        foreach (FieldInfo fieldInfo in context.allFields) {
            AptFieldProps aptFieldProps = context.fieldPropsMap[fieldInfo];
            if (!IsSerializableField(fieldInfo, instMethodWithInherit, aptFieldProps!)) {
                continue;
            }
            context.serialFields.Add(fieldInfo);

            if (IsAutoWriteField(fieldInfo, aptClassProps, aptFieldProps)) {
                if (!string.IsNullOrWhiteSpace(aptFieldProps.attribute.WriteProxy)) {
                    continue;
                }
                // 工具写：需要是public字段或包含public getter
                if (!CanGetDirectly(fieldInfo)
                    && string.IsNullOrWhiteSpace(aptFieldProps.attribute.Getter)
                    && FindPublicGetter(fieldInfo, allFieldsAndMethodWithInherit, aptFieldProps) == null) {
                    throw new Exception($"auto write field {fieldInfo} must be public or contains a public getter");
                }
            }
            if (IsAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                if (!string.IsNullOrWhiteSpace(aptFieldProps.attribute.ReadProxy)) {
                    continue;
                }
                // 工具读：需要是public或包含public setter
                if (!CanSetDirectly(fieldInfo)
                    && string.IsNullOrWhiteSpace(aptFieldProps.attribute.Setter)
                    && FindPublicSetter(fieldInfo, allFieldsAndMethodWithInherit, aptFieldProps) == null) {
                    throw new Exception($"auto read field {fieldInfo} must be public or contains a public setter");
                }
            }
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void CheckConstructor(Type typeElement) {
        if (typeElement.IsAbstract) {
            return;
        }
        if (BeanUtils.ContainsNoArgsConstructor(typeElement)
            || ContainsReaderConstructor(typeElement)
            || ContainsNewInstanceMethod(typeElement)) {
            return;
        }
        throw new Exception($"SerializableClass {typeElement} must contains no-args constructor or reader-args constructor!");
    }

    #endregion

    #region 钩子查询

    /** 是否包含 T(Reader reader) 构造方法 */
    internal bool ContainsReaderConstructor(Type typeElement) {
        return BeanUtils.ContainsOneArgsConstructor(typeElement, type_DsonReader);
    }

    /** 是否包含 newInstance(reader) 静态解码方法 -- 只能从当前类型查询 */
    internal bool ContainsNewInstanceMethod(Type typeElement) {
        MemberInfo[] staticMembers = typeElement.GetMembers(Static | Public);
        return ContainsHookMethod(staticMembers, MNAME_NEW_INSTANCE, type_DsonReader);
    }

    /** 是否包含 readerObject(reader) 实例方法 */
    internal bool ContainsReadObjectMethod(List<MemberInfo> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_READ_OBJECT, type_DsonReader);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    internal bool ContainsWriteObjectMethod(List<MemberInfo> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_WRITE_OBJECT, type_DsonWriter);
    }

    /** 是否包含 beforeEncode 实例方法 */
    internal bool ContainsBeforeEncodeMethod(List<MemberInfo> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_BEFORE_ENCODE, type_Options);
    }

    /** 是否包含 afterDecode 实例方法 */
    internal bool ContainsAfterDecodeMethod(List<MemberInfo> allFieldsAndMethodWithInherit) {
        return ContainsHookMethod(allFieldsAndMethodWithInherit, MNAME_AFTER_DECODE, type_Options);
    }

    /** 是否包含指定参数的钩子方法 */
    private bool ContainsHookMethod(IEnumerable<MemberInfo> allFieldsAndMethodWithInherit, string methodName, Type argType) {
        return allFieldsAndMethodWithInherit
            .Where(e => e.MemberType == MemberTypes.Method)
            .Select(e => (MethodInfo)e)
            .Any(e => {
                if (!e.IsPublic || e.Name != methodName) {
                    return false;
                }
                ParameterInfo[] parameterInfos = e.GetParameters();
                if (parameterInfos.Length == 0) {
                    return false;
                }
                return parameterInfos[0].ParameterType == argType;
            });
    }

    #endregion

    #region 字段检查

    /// <summary>
    /// 测试是否可以直接读取字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接取值，则返回true</returns>
    internal bool CanGetDirectly(FieldInfo fieldInfo) {
        return fieldInfo.IsPublic;
    }

    /// <summary>
    /// 测试是否可以直接写字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接赋值，则返回true</returns>
    internal bool CanSetDirectly(FieldInfo fieldInfo) {
        if (fieldInfo.IsInitOnly) {
            return false;
        }
        return fieldInfo.IsPublic;
    }

    /**
     * 查找非private的getter方法
     *
     * @param allMethodWithInherit 所有的字段和方法，可能在父类中
     */
    internal PropertyInfo? FindPublicGetter(FieldInfo variableElement, List<MemberInfo> allMethodWithInherit, AptFieldProps aptFieldProps) {
        PropertyInfo? autoProperty = aptFieldProps.autoProperty;
        if (autoProperty != null) {
            MethodInfo? getMethod = autoProperty.GetMethod;
            return (getMethod != null && getMethod.IsPublic) ? autoProperty : null;
        }
        return BeanUtils.FindPublicGetter(variableElement, allMethodWithInherit);
    }

    /**
     * 查找非private的setter方法
     *
     * @param allMethodWithInherit 所有的字段和方法，可能在父类中
     */
    internal PropertyInfo? FindPublicSetter(FieldInfo variableElement, List<MemberInfo> allMethodWithInherit, AptFieldProps aptFieldProps) {
        PropertyInfo? autoProperty = aptFieldProps.autoProperty;
        if (autoProperty != null) {
            MethodInfo? setMethod = autoProperty.SetMethod;
            return (setMethod != null && setMethod.IsPublic) ? autoProperty : null;
        }
        return BeanUtils.FindPublicSetter(variableElement, allMethodWithInherit);
    }

    /**
     * 是否是可序列化的字段
     * 1.默认只序列化 public 字段
     * 2.默认忽略 <see cref="NonSerializedAttribute"/> 字段
     */
    internal bool IsSerializableField(FieldInfo fieldInfo, List<MemberInfo> allMethodWithInherit, AptFieldProps aptFieldProps) {
        if (fieldInfo.IsStatic) {
            return false;
        }
        // 有注解的情况取决于注解的值，含NonSerializedAttribute注解处理
        if (aptFieldProps.ignore.HasValue) {
            return !aptFieldProps.ignore.Value;
        }
        // 判断public和getter/setter
        if (fieldInfo.IsPublic) {
            return true;
        }
        // 自动属性优化
        if (aptFieldProps.autoProperty != null) {
            MethodInfo? getMethod = aptFieldProps.autoProperty.GetMethod;
            MethodInfo? setMethod = aptFieldProps.autoProperty.SetMethod;
            return (getMethod != null && getMethod.IsPublic)
                   && (setMethod != null && setMethod.IsPublic);
        }
        // setter更容易失败
        return BeanUtils.ContainsPublicSetter(fieldInfo, allMethodWithInherit)
               && BeanUtils.ContainsPublicGetter(fieldInfo, allMethodWithInherit);
    }

    /** 是否是托管写的字段 */
    internal bool IsAutoWriteField(FieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.IsSingleton()) {
            return false;
        }
        if (IsSkipField(fieldInfo, aptClassProps, aptFieldProps)) {
            return false;
        }
        return true;
    }

    /** 是否是托管读的字段 */
    internal bool IsAutoReadField(FieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.IsSingleton()) {
            return false;
        }
        // readonly或无setter的字段只能构造方法读
        if (fieldInfo.IsInitOnly) {
            return false;
        }
        if (IsSkipField(fieldInfo, aptClassProps, aptFieldProps)) {
            return false;
        }
        return true;
    }

    /** skip仅仅代表不自动读 */
    private bool IsSkipField(FieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
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
        string declaringTypeName = fieldInfo.DeclaringType!.Name;
        if (aptClassProps.skipFields.Contains(declaringTypeName + "." + fieldName)) {
            return true;
        }
        return false;
    }

    #endregion

    #region overring util

    public MethodSpec NewGetEncoderTypeMethod(Type superDeclaredType, TypeName encoderTypeName) {
        // 需要处理泛型
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_GET_ENCODER_TYPE);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo)
            .Code(CodeBlock.Of("typeof($T)", encoderTypeName).WithExpressionStyle())
            .Build();
    }

    public MethodSpec.Builder NewNewInstanceMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_NEW_INSTANCE, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewReadFieldsMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_READ_FIELDS, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewAfterDecodeMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_AFTER_DECODE, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewBeforeEncodeMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_BEFORE_ENCODE, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    public MethodSpec.Builder NewWriteFieldsMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_WRITE_FIELDS, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    #endregion

    #region export

    private static readonly TypeVariableName emptyTypeVariableName = TypeVariableName.Get("");

    private void GenCodecExporter() {
        TypeSpec.Builder typeBuilder = TypeSpec.NewClassBuilder(codecExporterClassName!.simpleName)
            .AddModifiers(Modifiers.Public | Modifiers.Static)
            .AddAttribute(processorInfoAnnotation);
        // 生成导出方法
        {
            ClassName typeName_Type2TypeDictionary = ClassName.Get(typeof(Dictionary<Type, Type>));
            var methodBuilder = MethodSpec.NewMethodBuilder("ExportCodecs")
                .AddModifiers(Modifiers.Public | Modifiers.Static)
                .Returns(typeName_Type2TypeDictionary);
            methodBuilder.codeBuilder.AddStatement("var dic = new $T($L)", typeName_Type2TypeDictionary, _type2CodecNames.Count);
            foreach (KeyValuePair<ClassName, ClassName> pair in _type2CodecNames) {
                methodBuilder.codeBuilder.AddStatement("dic[typeof($T)] = typeof($T)", pair.Key, pair.Value);
            }
            methodBuilder.codeBuilder.AddStatement("return dic");
            typeBuilder.AddMethod(methodBuilder.Build());
        }

        // 写入文件
        CsharpFile csharpFile = CsharpFile.NewBuilder(codecExporterClassName.simpleName)
            .AddSpecs(fileHeader)
            .AddSpec(new MacroSpec("pragma", "warning disable CS1591"))
            .AddSpec(NamespaceSpec.Of(codecExporterClassName.ns, typeBuilder.Build()))
            .Build();

        _codeWriter.Reset();
        File.WriteAllText(csharpFileOutDir + "/" + csharpFile.name + ".cs",
            _codeWriter.Write(csharpFile),
            _utf8Encoding);
    }

    #endregion
}
}