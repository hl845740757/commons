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
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;
using static System.Reflection.BindingFlags;
using ClassName = Wjybxx.Commons.Poet.ClassName;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.Dson.Apt2
{
/// <summary>
/// <see cref="DsonSerializableAttribute"/>注解处理器
///
/// 1.最终序列化的都是字段，自动属性只是定义字段的快捷方法，自动属性字段的编码名默认为属性名。
///
/// 该处理器主要用途为第三方程序集生成Codec，因此通常处理的是当前程序集力定义的<see cref="DsonCodecLinkerBeanAttribute"/>
/// 和<see cref="DsonCodecLinkerGroupAttribute"/>注解，因此由用户显式指定类型，而不是反射整个程序集，以避免重复生成。
/// 建议将配置类放在特定的命名空间下，扫描特定的命名空间即可。
/// </summary>
public class CodecProcessor
{
    #region 常量

    internal const string MNAME_READ_OBJECT = "ReadObject";
    internal const string MNAME_WRITE_OBJECT = "WriteObject";

    internal const string MNAME_GET_ENCODER_TYPE = "GetEncoderType";
    internal const string MNAME_BEFORE_ENCODE = "BeforeEncode";
    internal const string MNAME_WRITE_FIELDS = "WriteFields";
    internal const string MNAME_NEW_INSTANCE = "NewInstance";
    internal const string MNAME_READ_FIELDS = "ReadFields";
    internal const string MNAME_AFTER_DECODE = "AfterDecode";

    internal static readonly ClassName typeName_CollectionUtil = ClassName.Get(typeof(CollectionUtil));
    internal static readonly ClassName typeName_WireType = ClassName.Get(typeof(WireType));
    internal static readonly ClassName typeName_NumberStyle = ClassName.Get(typeof(NumberStyle));
    internal static readonly ClassName typeName_StringStyle = ClassName.Get(typeof(StringStyle));
    internal static readonly ClassName typeName_ObjectStyle = ClassName.Get(typeof(ObjectStyle));
    internal static readonly ClassName typeName_NumberStyles = ClassName.Get(typeof(NumberStyles));

    #endregion

    #region 字段

#nullable disable

    /// <summary>
    /// 要处理的类型
    /// </summary>
    public readonly List<Type> assemblyTypes;
    /// <summary>
    /// 生成的c#文件的输出目录
    /// </summary>
    public readonly string csharpFileOutDir;
    /// <summary>
    /// 文件头
    /// </summary>
    public readonly List<ISpecification> fileHeader;

    // region 字段
    // Dson
    internal Type anno_DsonSerializable;
    internal Type anno_DsonProperty;
    internal Type anno_DsonIgnore;
    internal Type type_DsonReader;
    internal Type type_DsonWriter;
    internal Type type_Options; // ConverterOptions

    // linker
    internal Type anno_CodecLinkerGroup;
    internal Type anno_CodecLinker;
    internal Type anno_CodecLinkerBean;

    // abstractCodec
    internal Type type_DsonCodec;
    internal Type type_AbstractCodec;

    private readonly CodeWriter _codeWriter = new CodeWriter(indent: "    ");
    private readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false);

    /** 每个程序集初始化一次 */
    private readonly AttributeSpec processorInfoAnnotation = AptUtils.NewProcessorInfoAnnotation(typeof(CodecProcessor));

#nullable enable

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyTypes">要处理的类型</param>
    /// <param name="csharpFileOutDir">CS文件输出目录</param>
    /// <param name="fileHeader">文件头</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CodecProcessor(List<Type> assemblyTypes,
                          string csharpFileOutDir,
                          List<ISpecification>? fileHeader = null) {
        this.assemblyTypes = assemblyTypes ?? throw new ArgumentNullException(nameof(assemblyTypes));
        this.csharpFileOutDir = csharpFileOutDir ?? throw new ArgumentNullException(nameof(csharpFileOutDir));
        this.fileHeader = fileHeader ?? new List<ISpecification>();
    }

    #region Init

    private void Init() {
        // dson
        anno_DsonSerializable = typeof(DsonSerializableAttribute);
        anno_DsonProperty = typeof(DsonPropertyAttribute);
        anno_DsonIgnore = typeof(DsonIgnoreAttribute);
        type_DsonReader = typeof(IDsonObjectReader);
        type_DsonWriter = typeof(IDsonObjectWriter);
        type_Options = typeof(ConverterOptions);

        // linker
        anno_CodecLinkerGroup = typeof(DsonCodecLinkerGroupAttribute);
        anno_CodecLinker = typeof(DsonCodecLinkerAttribute);
        anno_CodecLinkerBean = typeof(DsonCodecLinkerBeanAttribute);

        // Codec
        type_DsonCodec = typeof(IDsonCodec<>);
        type_AbstractCodec = typeof(AbstractDsonCodec<>);
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
        // 自动加载一下它们的内部类--需要去重
        LinkedHashSet<Type> types = new LinkedHashSet<Type>(assemblyTypes);
        foreach (Type type in assemblyTypes) {
            types.AddAll(type.GetNestedTypes());
        }
        foreach (Type type in types) {
            DsonCodecLinkerBeanAttribute? linkerBeanAttribute = type.GetCustomAttribute<DsonCodecLinkerBeanAttribute>();
            if (linkerBeanAttribute != null) {
                ProcessLinkerBean(type, linkerBeanAttribute);
                continue;
            }
            DsonCodecLinkerGroupAttribute? linkerGroupAttribute = type.GetCustomAttribute<DsonCodecLinkerGroupAttribute>();
            if (linkerGroupAttribute != null) {
                ProcessLinkerGroup(type, linkerGroupAttribute);
                continue;
            }
            DsonSerializableAttribute? serializableAttribute = type.GetCustomAttribute<DsonSerializableAttribute>();
            if (serializableAttribute != null) {
                ProcessDirectType(type, serializableAttribute);
            }
        }
    }

    #region process

    private void ProcessLinkerBean(Type linkerBeanType, DsonCodecLinkerBeanAttribute linkerBeanAttribute) {
        // Target是构造函数参数，而Namespace是属性参数
        Type targetType = linkerBeanAttribute.Target;
        string outNamespace = GetOutputNamespace(linkerBeanType, linkerBeanAttribute.OutputNamespace);
        AptClassProps aptClassProps = AptClassProps.Parse(linkerBeanAttribute);

        // 创建模拟数据
        Context context = new Context(targetType);
        context.outputNamespace = outNamespace;
        context.aptClassProps = aptClassProps;
        context.additionalAnnotations = GetAdditionalAnnotations(aptClassProps);
        CacheFields(context);
        CacheFieldProps(context);
        // 修正字段的Props —— 将LinkerBean上的注解信息转移到目标类
        {
            Context linkerBeanContext = new Context(linkerBeanType);
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
            aptClassProps.codecProxyClassName = ClassName.Get(linkerBeanType);
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

    private void ProcessLinkerGroup(Type linkerGroupType, DsonCodecLinkerGroupAttribute linkerGroupAttribute) {
        string outNamespace = GetOutputNamespace(linkerGroupType, linkerGroupAttribute.OutputNamespace);
        // 扫描LinkerGroup的字段
        List<FieldInfo> linkerGroupFields = AptUtils.GetAllMembersWithInherit(linkerGroupType, MemberTypes.Field)
            .Cast<FieldInfo>()
            .ToList();
        foreach (FieldInfo fieldInfo in linkerGroupFields) {
            // 查找字段的配置
            DsonCodecLinkerAttribute? linkerAttribute = fieldInfo.GetCustomAttribute<DsonCodecLinkerAttribute>();
            AptClassProps aptClassProps = AptClassProps.Parse(linkerAttribute);

            // 泛型字段需要转换为泛型定义类 -- 不能连接到特殊类型
            Type targetType = fieldInfo.FieldType;
            if (targetType.IsGenericType) {
                targetType = targetType.GetGenericTypeDefinition();
            }
            // 创建模拟数据
            Context context = new Context(targetType);
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

    private void ProcessDirectType(Type typeElement, DsonSerializableAttribute serializableAttribute) {
        Context context = new Context(typeElement);
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
        _codeWriter.IndentInsideNamespace = false;
        File.WriteAllText(csharpFileOutDir + "/" + csharpFile.name + ".cs",
            _codeWriter.Write(csharpFile),
            _utf8Encoding);
    }

    private void CacheFields(Context context) {
        context.allMembers = AptUtils.GetAllMembersWithInherit(context.type);
        //
        List<AptFieldInfo> allFields = new List<AptFieldInfo>(context.allMembers.Count / 2);
        foreach (MemberInfo memberInfo in context.allMembers) {
            if (memberInfo.MemberType != MemberTypes.Field) {
                continue;
            }
            FieldInfo fieldInfo = (FieldInfo)memberInfo;
            PropertyInfo? propertyInfo = AptUtils.FindProperty(fieldInfo, context.allMembers);

            AptFieldInfo aptFieldInfo = new AptFieldInfo(fieldInfo, propertyInfo);
            aptFieldInfo.typeName = TypeName.Get(fieldInfo.FieldType);
            allFields.Add(aptFieldInfo);
        }
        context.allFields = allFields;
    }

    private void CacheFieldProps(Context context) {
        foreach (AptFieldInfo fieldInfo in context.allFields) {
            // dson-property
            AptFieldProps aptFieldProps = AptFieldProps.Parse(fieldInfo);
            // dson-ignore
            aptFieldProps.ParseIgnore(fieldInfo);
            //
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
        List<Type> attributes = aptClassProps.additionalAnnotations;
        List<AttributeSpec> result = new List<AttributeSpec>(attributes.Count);
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
            .AddBaseClass(ClassName.Get(superDeclaredType));

        // 拷贝泛型参数 -- Codec泛型参数和原始类型泛型参数相同
        foreach (Type typeParameter in type.GetGenericArguments()) {
            context.typeBuilder.AddTypeParameter(TypeParameterSpec.Get(typeParameter));
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
        if (aptClassProps.IsSingleton) {
            return;
        }
        Type targetType = context.type;
        CheckConstructor(targetType, aptClassProps);

        List<MemberInfo> allMembers = context.allMembers;
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
    private void CheckAutoReadField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps, List<MemberInfo> allMembers) {
        if (!string.IsNullOrWhiteSpace(aptFieldProps.readProxy)) {
            return;
        }
        // 工具读：需要是public或包含public setter
        if (!CanSetDirectly(fieldInfo)
            && string.IsNullOrWhiteSpace(aptFieldProps.setter)
            && !fieldInfo.HasPublicSetter) {
            // 由于可能是超类的字段，symbol可能为null，所以格式化文本中追加字段名
            throw new Exception($"auto read field {fieldInfo.Name} must be public or contains a public getter");
        }
    }

    /** 检查自动写字段 */
    private void CheckAutoWriteField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps, List<MemberInfo> allMembers) {
        if (!string.IsNullOrWhiteSpace(aptFieldProps.writeProxy)) {
            return;
        }
        // 工具写：需要是public字段或包含public getter
        if (!CanGetDirectly(fieldInfo)
            && string.IsNullOrWhiteSpace(aptFieldProps.getter)
            && !fieldInfo.HasPublicGetter) {
            // 由于可能是超类的字段，symbol可能为null，所以格式化文本中追加字段名
            throw new Exception($"auto write field {fieldInfo.Name} must be public or contains a public setter");
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void CheckConstructor(Type typeElement, AptClassProps aptClassProps) {
        if (typeElement.IsAbstract || typeElement.IsValueType) {
            return;
        }
        // 静态代理包含NewInstance方法
        if (aptClassProps.ContainsHookMethod(MNAME_NEW_INSTANCE)) {
            return;
        }
        if (ContainsNoArgsConstructor(typeElement)
            || ContainsReaderConstructor(typeElement)
            || ContainsNewInstanceMethod(typeElement)) {
            return;
        }
        throw new Exception($"SerializableClass {typeElement} must contains no-args constructor or reader-args constructor!");
    }

    #endregion

    #region 钩子查询

    /** 是否包含无参构造函数 */
    internal bool ContainsNoArgsConstructor(Type typeElement) {
        var constructor = AptUtils.GetNoArgsConstructor(typeElement);
        return constructor != null && constructor.IsPublic;
    }

    /** 是否包含 T(Reader reader) 构造方法 */
    internal bool ContainsReaderConstructor(Type typeElement) {
        var constructor = AptUtils.GetOneArgsConstructor(typeElement, type_DsonReader);
        return constructor != null && constructor.IsPublic;
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
    internal bool CanGetDirectly(AptFieldInfo fieldInfo) {
        return fieldInfo.IsPublic;
    }

    /// <summary>
    /// 测试是否可以直接写字段。
    /// </summary>
    /// <param name="fieldInfo">类字段，可能是继承的字段</param>
    /// <returns>如果可直接赋值，则返回true</returns>
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
        if (fieldInfo.IsStatic) return false;
        // 有注解的情况取决于注解的值，需取反 -- 注解已提前解析
        if (aptFieldProps.ignore.HasValue) {
            return !aptFieldProps.ignore.Value;
        }
        // 无注解的情况下，默认忽略 NonSerialized 字段
        if (fieldInfo.GetAttribute<NonSerializedAttribute>() != null) {
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
    private bool IsSkipField(AptFieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.skipFields.Count == 0) {
            return false;
        }
        if (aptClassProps.skipFields.Contains("*")) {
            return true;
        }
        // 如果是自动属性，则使用属性名
        string fieldName;
        if (fieldInfo.IsAutoPropertyField) {
            fieldName = fieldInfo.propertyInfo!.Name;
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

    internal MethodSpec NewGetEncoderTypeMethod(Type superDeclaredType, TypeName encoderTypeName) {
        // 需要处理泛型
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_GET_ENCODER_TYPE);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo)
            .Code(CodeBlock.Of("typeof($T)", encoderTypeName).WithExpressionStyle())
            .Build();
    }

    internal MethodSpec.Builder NewNewInstanceMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_NEW_INSTANCE, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    internal MethodSpec.Builder NewReadFieldsMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_READ_FIELDS, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    internal MethodSpec.Builder NewAfterDecodeMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_AFTER_DECODE, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    internal MethodSpec.Builder NewBeforeEncodeMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_BEFORE_ENCODE, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    internal MethodSpec.Builder NewWriteFieldsMethodBuilder(Type superDeclaredType) {
        MethodInfo? methodInfo = superDeclaredType.GetMethod(MNAME_WRITE_FIELDS, Public | NonPublic | Instance);
        if (methodInfo == null) {
            throw new AssertionError();
        }
        return MethodSpec.Overriding(methodInfo);
    }

    #endregion
}
}