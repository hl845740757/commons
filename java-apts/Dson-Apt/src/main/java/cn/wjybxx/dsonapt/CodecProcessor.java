/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dsonapt;

import cn.wjybxx.apt.AptUtils;
import cn.wjybxx.apt.BeanUtils;
import cn.wjybxx.apt.MyAbstractProcessor;
import com.google.auto.service.AutoService;
import com.squareup.javapoet.*;

import javax.annotation.processing.Processor;
import javax.annotation.processing.RoundEnvironment;
import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import javax.tools.Diagnostic;
import java.time.LocalDateTime;
import java.util.*;

/**
 * @author wjybxx
 * date 2023/4/13
 */
@AutoService(Processor.class)
public class CodecProcessor extends MyAbstractProcessor {

    // region 常量
    private static final String CNAME_WireType = "cn.wjybxx.dson.WireType";
    private static final String CNAME_NumberStyle = "cn.wjybxx.dson.text.NumberStyle";
    private static final String CNAME_StringStyle = "cn.wjybxx.dson.text.StringStyle";
    private static final String CNAME_ObjectStyle = "cn.wjybxx.dson.text.ObjectStyle";
    private static final String CNAME_ContextType = "cn.wjybxx.dson.DsonContextType";
    private static final String CNAME_DsonType = "cn.wjybxx.dson.DsonType";

    private static final String CNAME_Binary = "cn.wjybxx.dson.types.Binary";
    private static final String CNAME_ObjectPtr = "cn.wjybxx.dson.types.ObjectPtr";
    private static final String CNAME_Timestamp = "cn.wjybxx.dson.types.Timestamp";
    // commons
    private static final String CNAME_TypeInfo = "cn.wjybxx.base.TypeInfo";

    // Dson
    private static final String CNAME_SERIALIZABLE = "cn.wjybxx.dsoncodec.annotations.DsonSerializable";
    private static final String CNAME_PROPERTY = "cn.wjybxx.dsoncodec.annotations.DsonProperty";
    private static final String CNAME_DSON_IGNORE = "cn.wjybxx.dsoncodec.annotations.DsonIgnore";
    private static final String CNAME_DSON_READER = "cn.wjybxx.dsoncodec.DsonObjectReader";
    private static final String CNAME_DSON_WRITER = "cn.wjybxx.dsoncodec.DsonObjectWriter";
    private static final String CNAME_OPTIONS = "cn.wjybxx.dsoncodec.ConverterOptions";
    // Linker
    private static final String CNAME_CODEC_LINKER_GROUP = "cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerGroup";
    private static final String CNAME_CODEC_LINKER = "cn.wjybxx.dsoncodec.annotations.DsonCodecLinker";
    private static final String CNAME_CODEC_LINKER_BEAN = "cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerBean";
    private static final String MNAME_OUTPUT = "outputPackage";
    private static final String MNAME_CLASS_PROPS = "props"; // Java的注解是组合式
    private static final String MNAME_VALUE = "value"; // 链接的目标

    // Codec
    public static final String CNAME_CODEC = "cn.wjybxx.dsoncodec.DsonCodec";
    public static final String MNAME_READ_OBJECT = "readObject";
    public static final String MNAME_WRITE_OBJECT = "writeObject";
    // AbstractCodec
    private static final String CNAME_ABSTRACT_CODEC = "cn.wjybxx.dsoncodec.AbstractDsonCodec";
    public static final String MNAME_GET_ENCODER_TYPE = "getEncoderType";
    public static final String MNAME_BEFORE_ENCODE = "beforeEncode";
    public static final String MNAME_WRITE_FIELDS = "writeFields";
    public static final String MNAME_NEW_INSTANCE = "newInstance";
    public static final String MNAME_READ_FIELDS = "readFields";
    public static final String MNAME_AFTER_DECODE = "afterDecode";

    public static final ClassName typeName_TypeInfo = AptUtils.classNameOfCanonicalName(CNAME_TypeInfo);
    public static final ClassName typeName_WireType = AptUtils.classNameOfCanonicalName(CNAME_WireType);
    public static final ClassName typeName_NumberStyle = AptUtils.classNameOfCanonicalName(CNAME_NumberStyle);
    public static final ClassName typeName_StringStyle = AptUtils.classNameOfCanonicalName(CNAME_StringStyle);
    public static final ClassName typeName_ObjectStyle = AptUtils.classNameOfCanonicalName(CNAME_ObjectStyle);
    public static final ClassName typeName_ContextType = AptUtils.classNameOfCanonicalName(CNAME_ContextType);
    public static final ClassName typeName_DsonType = AptUtils.classNameOfCanonicalName(CNAME_DsonType);

    //endregion

    // region 字段

    // Dson
    public TypeElement anno_DsonSerializable;
    public TypeMirror anno_DsonProperty;
    public TypeMirror anno_DsonIgnore;
    public TypeMirror typeMirror_DsonReader;
    public TypeMirror typeMirror_dsonWriter;
    public TypeMirror typeMirror_Options;

    // linker
    public TypeElement anno_CodecLinkerGroup;
    public TypeElement anno_CodecLinker;
    public TypeElement anno_CodecLinkerBean;

    // abstractCodec
    public TypeElement abstractCodecTypeElement;
    public ExecutableElement getEncoderTypeMethod;
    public ExecutableElement newInstanceMethod;
    public ExecutableElement readFieldsMethod;
    public ExecutableElement afterDecodeMethod;
    public ExecutableElement beforeEncodeMethod;
    public ExecutableElement writeFieldsMethod;

    // 特殊类型依赖
    // 基础类型
    public TypeMirror type_String;
    public TypeMirror type_Object;
    public TypeMirror type_LocalDateTime;
    public TypeMirror type_Binary;
    public TypeMirror type_Ptr;
    public TypeMirror type_Timestamp;

    // 集合类型
    public TypeMirror type_EnumSet;
    public TypeMirror type_EnumMap;

    // endregion

    public CodecProcessor() {
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_SERIALIZABLE, CNAME_CODEC_LINKER_GROUP, CNAME_CODEC_LINKER_BEAN);
    }

    @Override
    protected void ensureInited() {
        if (anno_DsonSerializable != null) return;

        // dson
        anno_DsonSerializable = elementUtils.getTypeElement(CNAME_SERIALIZABLE);
        anno_DsonProperty = elementUtils.getTypeElement(CNAME_PROPERTY).asType();
        anno_DsonIgnore = elementUtils.getTypeElement(CNAME_DSON_IGNORE).asType();
        typeMirror_DsonReader = elementUtils.getTypeElement(CNAME_DSON_READER).asType();
        typeMirror_dsonWriter = elementUtils.getTypeElement(CNAME_DSON_WRITER).asType();
        typeMirror_Options = elementUtils.getTypeElement(CNAME_OPTIONS).asType();
        // linker
        anno_CodecLinkerGroup = elementUtils.getTypeElement(CNAME_CODEC_LINKER_GROUP);
        anno_CodecLinker = elementUtils.getTypeElement(CNAME_CODEC_LINKER);
        anno_CodecLinkerBean = elementUtils.getTypeElement(CNAME_CODEC_LINKER_BEAN);

        // Codec
        TypeElement codecTypeElement = elementUtils.getTypeElement(CNAME_CODEC);
        getEncoderTypeMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_ENCODER_TYPE);
        // abstractCodec
        abstractCodecTypeElement = elementUtils.getTypeElement(CNAME_ABSTRACT_CODEC);
        {
            List<ExecutableElement> allMethodsWithInherit = BeanUtils.getAllMethodsWithInherit(abstractCodecTypeElement);
            // dson
            newInstanceMethod = findCodecMethod(allMethodsWithInherit, MNAME_NEW_INSTANCE, typeMirror_DsonReader);
            readFieldsMethod = findCodecMethod(allMethodsWithInherit, MNAME_READ_FIELDS, typeMirror_DsonReader);
            afterDecodeMethod = findCodecMethod(allMethodsWithInherit, MNAME_AFTER_DECODE, typeMirror_DsonReader);
            beforeEncodeMethod = findCodecMethod(allMethodsWithInherit, MNAME_BEFORE_ENCODE, typeMirror_dsonWriter);
            writeFieldsMethod = findCodecMethod(allMethodsWithInherit, MNAME_WRITE_FIELDS, typeMirror_dsonWriter);
        }

        // 特殊类型依赖
        // 基础类型
        type_String = elementUtils.getTypeElement(String.class.getCanonicalName()).asType();
        type_Object = elementUtils.getTypeElement(Object.class.getCanonicalName()).asType();
        type_LocalDateTime = elementUtils.getTypeElement(LocalDateTime.class.getCanonicalName()).asType();
        type_Binary = elementUtils.getTypeElement(CNAME_Binary).asType();
        type_Ptr = elementUtils.getTypeElement(CNAME_ObjectPtr).asType();
        type_Timestamp = elementUtils.getTypeElement(CNAME_Timestamp).asType();
        // 特殊集合
        type_EnumSet = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumSet.class));
        type_EnumMap = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumMap.class));
    }

    private ExecutableElement findCodecMethod(List<ExecutableElement> allMethodsWithInherit,
                                              String methodName, TypeMirror readerWriterType) {
        return allMethodsWithInherit.stream()
                .filter(e -> e.getSimpleName().toString().equals(methodName))
                .filter(e -> e.getParameters().size() > 0
                        && AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), readerWriterType)
                )
                .findFirst()
                .orElseThrow(() -> new RuntimeException("method is absent, methodName: " + methodName));
    }

    @Override
    protected boolean doProcess(Set<? extends TypeElement> annotations, RoundEnvironment roundEnv) {
        final Set<TypeElement> allTypeElements = AptUtils.selectSourceFileAny(roundEnv, elementUtils,
                anno_DsonSerializable, anno_CodecLinkerGroup, anno_CodecLinkerBean);
        for (TypeElement typeElement : allTypeElements) {
            try {
                AnnotationMirror dsonSerialAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_DsonSerializable.asType());
                if (dsonSerialAnnoMirror != null) {
                    processDirectType(typeElement, dsonSerialAnnoMirror);
                    continue;
                }
                AnnotationMirror linkerBeanAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_CodecLinkerBean.asType());
                if (linkerBeanAnnoMirror != null) {
                    processLinkerBean(typeElement, linkerBeanAnnoMirror);
                    continue;
                }
                AnnotationMirror linkerGroupAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_CodecLinkerGroup.asType());
                if (linkerGroupAnnoMirror != null) {
                    processLinkerGroup(typeElement, linkerGroupAnnoMirror);
                    continue;
                }
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), typeElement);
            }
        }
        return true;
    }

    // region process

    private void processLinkerBean(TypeElement linkerBeanType, AnnotationMirror linkerBeanAnnoMirror) {
        final String outPackage = getOutputPackage(linkerBeanType, linkerBeanAnnoMirror);
        // 真实需要生成Codec的类型
        DeclaredType targetType;
        {
            AnnotationValue annotationValue = AptUtils.getAnnotationValue(linkerBeanAnnoMirror, MNAME_VALUE);
            Objects.requireNonNull(annotationValue, "targetType");
            targetType = AptUtils.findDeclaredType(AptUtils.getAnnotationValueTypeMirror(annotationValue));
            Objects.requireNonNull(targetType, "targetType");
        }
        AnnotationValue classPropsAnnoValue = AptUtils.getAnnotationValue(linkerBeanAnnoMirror, MNAME_CLASS_PROPS);
        AptClassProps aptClassProps = classPropsAnnoValue == null ?
                new AptClassProps() : AptClassProps.parse((AnnotationMirror) classPropsAnnoValue.getValue());

        // 创建模拟数据
        TypeElement targetTypeElement = (TypeElement) targetType.asElement();
        Context context = new Context(targetTypeElement, linkerBeanType);
        context.outPackage = outPackage;
        context.aptClassProps = aptClassProps;
        context.additionalAnnotations = getAdditionalAnnotations(aptClassProps);
        cacheFields(context);
        cacheFieldProps(context);
        // 修正字段的Props注解 —— 将LinkerBean上的注解信息转移到目标类
        {
            Context linkerBeanContext = new Context(linkerBeanType, null);
            cacheFields(linkerBeanContext);
            cacheFieldProps(linkerBeanContext);

            // 由于FieldKey包含了声明字段的类型，因此LinkerBean无法直接映射，我们只能按字段的简单名匹配
            for (AptFieldInfo fieldInfo : context.allFields) {
                AptFieldProps fieldProps = linkerBeanContext.findFieldProps(fieldInfo.name);
                if (fieldProps != null) {
                    context.fieldPropsMap.put(fieldInfo, fieldProps);
                }
            }
            context.linkerContext = linkerBeanContext;
        }
        // 检查数据
        {
            checkTypeElement(context);
        }
        // 生成Codec
        {
            generateCodec(context);
        }
    }

    private void processLinkerGroup(TypeElement groupTypeElement, AnnotationMirror linkerGroupAnnoMirror) {
        final String outPackage = getOutputPackage(groupTypeElement, linkerGroupAnnoMirror);
        for (VariableElement variableElement : BeanUtils.getAllFieldsWithInherit(groupTypeElement)) {
            DeclaredType targetType = AptUtils.findDeclaredType(variableElement.asType());
            if (targetType == null) {
                messager.printMessage(Diagnostic.Kind.ERROR, "Bad Linker Target", variableElement);
                continue;
            }

            AnnotationMirror linkerAnnoMirror = AptUtils.findAnnotation(typeUtils, variableElement, anno_CodecLinker.asType());
            AnnotationValue classPropsAnnoValue = linkerAnnoMirror != null ? AptUtils.getAnnotationValue(linkerAnnoMirror, MNAME_CLASS_PROPS) : null;
            AptClassProps aptClassProps = classPropsAnnoValue != null ? AptClassProps.parse((AnnotationMirror) classPropsAnnoValue.getValue())
                    : new AptClassProps();

            // 创建模拟数据
            TypeElement typeElement = (TypeElement) targetType.asElement();
            Context context = new Context(typeElement, variableElement);
            context.outPackage = outPackage;
            context.aptClassProps = aptClassProps;
            context.additionalAnnotations = getAdditionalAnnotations(aptClassProps);
            cacheFields(context);
            cacheFieldProps(context);
            // 检查数据
            {
                checkTypeElement(context);
            }
            // 生成Codec
            {
                generateCodec(context);
            }
        }
    }

    private void processDirectType(TypeElement typeElement, AnnotationMirror dsonSerialAnnoMirror) {
        Context context = new Context(typeElement, null);
        context.aptClassProps = AptClassProps.parse(dsonSerialAnnoMirror);
        context.additionalAnnotations = getAdditionalAnnotations(context.aptClassProps);
        cacheFields(context);
        cacheFieldProps(context);
        // 检查数据
        {
            checkTypeElement(context);
        }
        // 生成Codec
        {
            generateCodec(context);
        }
    }

    private void generateCodec(Context context) {
        TypeElement typeElement = context.typeElement;
        if (typeElement.getKind() != ElementKind.CLASS) {
            return; // Enum
        }
        DeclaredType superDeclaredType = typeUtils.getDeclaredType(abstractCodecTypeElement, typeUtils.erasure(typeElement.asType()));
        initTypeBuilder(context, typeElement, superDeclaredType);
        // 先生成常量字段
        SchemaGenerator schemaGenerator = new SchemaGenerator(this, context);
        schemaGenerator.execute();
        // 再生成PojoCodec
        new PojoCodecGenerator(this, context).execute();

        // 写入文件
        if (context.outPackage != null) {
            AptUtils.writeToFile(typeElement, context.typeBuilder, context.outPackage, messager, filer);
        } else {
            AptUtils.writeToFile(typeElement, context.typeBuilder, elementUtils, messager, filer);
        }
    }

    private void cacheFields(Context context) {
        context.allMembers = BeanUtils.getAllFieldsAndMethodsWithInherit(context.typeElement);
        List<AptFieldInfo> allFields = new ArrayList<>();
        for (Element member : context.allMembers) {
            if (member.getKind() != ElementKind.FIELD || member.getModifiers().contains(Modifier.STATIC)) {
                continue;
            }
            VariableElement variableElement = (VariableElement) member;
            ExecutableElement publicGetter = BeanUtils.findPublicGetter(typeUtils, variableElement, context.allMembers);
            ExecutableElement publicSetter = BeanUtils.findPublicSetter(typeUtils, variableElement, context.allMembers);
            TypeName typeName = TypeName.get(variableElement.asType());

            allFields.add(new AptFieldInfo(variableElement, publicGetter, publicSetter, typeName));
        }
        context.allFields = allFields;
    }

    private void cacheFieldProps(Context context) {
        for (AptFieldInfo fieldInfo : context.allFields) {
            AptFieldProps aptFieldProps = AptFieldProps.parse(typeUtils, fieldInfo.element, anno_DsonProperty);
            // dsonIgnore
            aptFieldProps.parseIgnore(typeUtils, fieldInfo.element, anno_DsonIgnore);
            // 修正一下EnumSet和EnumMap，可以减少不必要的麻烦 -- 其实也可以测试是否是final类型
            TypeMirror fieldTypeMirror = fieldInfo.element.asType();
            if (aptFieldProps.implMirror == null
                    && fieldTypeMirror.getKind() == TypeKind.DECLARED
                    && (isEnumSet(fieldTypeMirror) || isEnumMap(fieldTypeMirror))) {
                aptFieldProps.implMirror = fieldTypeMirror;
            }
            context.fieldPropsMap.put(fieldInfo, aptFieldProps);
        }
    }

    /** 获取输出目录 -- 默认为配置类的路径 */
    private String getOutputPackage(TypeElement typeElement, AnnotationMirror annotationMirror) {
        String outPackage = AptUtils.getAnnotationValueValue(annotationMirror, MNAME_OUTPUT);
        if (AptUtils.isBlank(outPackage)) {
            return elementUtils.getPackageOf(typeElement).getQualifiedName().toString();
        }
        return outPackage;
    }

    /** 获取为生成的Codec附加的注解 */
    private List<AnnotationSpec> getAdditionalAnnotations(AptClassProps aptClassProps) {
        if (aptClassProps.additionalAnnotations.isEmpty()) {
            return List.of();
        }
        List<AnnotationSpec> result = new ArrayList<>(aptClassProps.additionalAnnotations.size());
        for (TypeMirror typeMirror : aptClassProps.additionalAnnotations) {
            final ClassName className = (ClassName) ClassName.get(typeMirror);
            result.add(AnnotationSpec.builder(className)
                    .build());
        }
        return result;
    }

    private void initTypeBuilder(Context context, TypeElement typeElement, DeclaredType superDeclaredType) {
        context.superDeclaredType = superDeclaredType;
        context.typeBuilder = TypeSpec.classBuilder(getCodecName(typeElement))
                .addModifiers(Modifier.PUBLIC, Modifier.FINAL)
                .addAnnotation(AptUtils.SUPPRESS_UNCHECKED_RAWTYPES)
                .addAnnotation(processorInfoAnnotation)
                .superclass(TypeName.get(superDeclaredType));
    }

    private String getCodecName(TypeElement typeElement) {
        return AptUtils.getProxyClassName(elementUtils, typeElement, "Codec");
    }

    // endregion

    // region check
    private void checkTypeElement(Context context) {
        TypeElement typeElement = context.typeElement;
        if (typeElement.getKind() != ElementKind.CLASS) {
            return; // Enum
        }
        final AptClassProps aptClassProps = context.aptClassProps;
        if (aptClassProps.isSingleton()) {
            return;
        }
        checkConstructor(context);

        final List<? extends Element> allMembers = context.allMembers;
        for (AptFieldInfo fieldInfo : context.allFields) {
            final AptFieldProps aptFieldProps = context.fieldPropsMap.get(fieldInfo);
            if (!isSerializableField(fieldInfo, aptFieldProps)) {
                continue;
            }
            context.serialFields.add(fieldInfo);

            if (isAutoWriteField(fieldInfo, aptClassProps, aptFieldProps)) {
                checkAutoWriteField(fieldInfo, aptFieldProps, allMembers, typeElement);
            }
            if (isAutoReadField(fieldInfo, aptClassProps, aptFieldProps)) {
                checkAutoReadField(fieldInfo, aptFieldProps, allMembers, typeElement);
            }
        }
    }

    private void checkAutoReadField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps,
                                    List<? extends Element> allMembers, TypeElement typeElement) {
        if (!AptUtils.isBlank(aptFieldProps.readProxy)) {
            return;
        }
        // 工具读：需要提供可直接赋值或非private的setter方法
        if (AptUtils.isBlank(aptFieldProps.setter)
                && !canSetDirectly(fieldInfo)
                && !fieldInfo.hasPublicSetter()) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    String.format("auto read field (%s) must be public or contains a public setter", fieldInfo.name),
                    typeElement); // 可能无法定位到超类字段，因此打印到Type
        }
    }

    private void checkAutoWriteField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps,
                                     List<? extends Element> allMembers, TypeElement typeElement) {
        if (!AptUtils.isBlank(aptFieldProps.writeProxy)) {
            return;
        }
        // 工具写：需要提供可直接取值或包含非private的getter方法
        if (AptUtils.isBlank(aptFieldProps.getter)
                && !canGetDirectly(fieldInfo)
                && !fieldInfo.hasPublicGetter()) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    String.format("auto write field (%s) must be public or contains a public getter", fieldInfo.name),
                    typeElement); // 可能无法定位到超类字段，因此打印到Type
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void checkConstructor(Context context) {
        TypeElement typeElement = context.typeElement;
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {
            return;
        }
        // 静态类包含nesInstance代理方法
        if (context.linkerContext != null
                && context.linkerContext.containsHookMethod(CodecProcessor.MNAME_NEW_INSTANCE)) {
            return;
        }
        if (BeanUtils.containsNoArgsConstructor(typeElement)
                || containsReaderConstructor(typeElement)
                || containsNewInstanceMethod(typeElement)) {
            return;
        }
        messager.printMessage(Diagnostic.Kind.ERROR,
                "SerializableClass %s must contains no-args constructor or reader-args constructor!",
                typeElement);
    }

    // region 钩子方法检查

    /** 是否包含 T(Reader reader, TypeInfo typeInfo) 构造方法 */
    public boolean containsReaderConstructor(TypeElement typeElement) {
        return typeElement.getEnclosedElements().stream()
                .filter(e -> e.getKind() == ElementKind.CONSTRUCTOR)
                .map(e -> (ExecutableElement) e)
                .filter(e -> e.getParameters().size() > 0)
                .anyMatch(e -> AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), typeMirror_DsonReader));
//        return BeanUtils.containsOneArgsConstructor(typeUtils, typeElement, dsonReaderTypeMirror);
    }

    /** 是否包含 newInstance(reader) 静态解码方法 -- 只能从当前类型查询 */
    public boolean containsNewInstanceMethod(TypeElement typeElement) {
        List<? extends Element> staticMembers = typeElement.getEnclosedElements().stream()
                .filter(e -> e.getModifiers().contains(Modifier.STATIC) && e.getModifiers().contains(Modifier.PUBLIC))
                .toList();
        return containsHookMethod(staticMembers, MNAME_NEW_INSTANCE, typeMirror_DsonReader);
    }

    /** 是否包含 readerObject(reader) 实例方法 */
    public boolean containsReadObjectMethod(List<? extends Element> allMembers) {
        return containsHookMethod(allMembers, MNAME_READ_OBJECT, typeMirror_DsonReader);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    public boolean containsWriteObjectMethod(List<? extends Element> allMembers) {
        return containsHookMethod(allMembers, MNAME_WRITE_OBJECT, typeMirror_dsonWriter);
    }

    /** 是否包含 beforeEncode 实例方法 */
    public Map.Entry<Boolean, Integer> containsBeforeEncodeMethod(List<? extends Element> allMembers) {
        if (containsHookMethod(allMembers, MNAME_BEFORE_ENCODE, typeMirror_Options)) {
            return Map.entry(true, 1);
        }
        if (containsNoArgHookMethod(allMembers, MNAME_BEFORE_ENCODE)) {
            return Map.entry(true, 0);
        }
        return Map.entry(false, 0);
    }

    /** 是否包含 afterDecode 实例方法 */
    public Map.Entry<Boolean, Integer> containsAfterDecodeMethod(List<? extends Element> allMembers) {
        if (containsHookMethod(allMembers, MNAME_AFTER_DECODE, typeMirror_Options)) {
            return Map.entry(true, 1);
        }
        if (containsNoArgHookMethod(allMembers, MNAME_AFTER_DECODE)) {
            return Map.entry(true, 0);
        }
        return Map.entry(false, 0);
    }

    private boolean containsHookMethod(List<? extends Element> allMembers, String methodName, TypeMirror argTypeMirror) {
        return allMembers.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .anyMatch(e -> e.getModifiers().contains(Modifier.PUBLIC)
                        && e.getParameters().size() > 0 // 有时可能需要多个参数
                        && e.getSimpleName().toString().equals(methodName)
                        && AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), argTypeMirror));
    }

    private boolean containsNoArgHookMethod(List<? extends Element> allMembers, String methodName) {
        return allMembers.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .anyMatch(e -> e.getModifiers().contains(Modifier.PUBLIC)
                        && e.getParameters().size() == 0 // 有时可能需要多个参数
                        && e.getSimpleName().toString().equals(methodName));
    }

    // endregion

    // region 字段检查

    /**
     * 测试是否可以直接读取字段。
     *
     * @param fieldInfo 类字段，可能是继承的字段
     * @return 如果可直接取值，则返回true
     */
    public boolean canGetDirectly(final AptFieldInfo fieldInfo) {
        return fieldInfo.getModifiers().contains(Modifier.PUBLIC);
    }

    /**
     * 测试是否可以直接写字段。
     *
     * @param fieldInfo 类字段，可能是继承的字段
     * @return 如果可直接赋值，则返回true
     */
    public boolean canSetDirectly(final AptFieldInfo fieldInfo) {
        if (fieldInfo.getModifiers().contains(Modifier.FINAL)) {
            return false;
        }
        return fieldInfo.getModifiers().contains(Modifier.PUBLIC);
    }

    /**
     * 是否是可序列化的字段
     * 1.默认只序列化 public 字段
     * 2.默认忽略 transient 字段
     */
    private boolean isSerializableField(AptFieldInfo fieldInfo, AptFieldProps aptFieldProps) {
        if (fieldInfo.getModifiers().contains(Modifier.STATIC)) {
            return false;
        }
        // 有注解的情况下，取决于注解的值 -- 取反。。。
        Boolean ignore = aptFieldProps.ignore;
        if (ignore != null) return !ignore;
        // 无注解的情况下，默认忽略 transient 字段
        if (fieldInfo.getModifiers().contains(Modifier.TRANSIENT)) {
            return false;
        }
        // 判断public和getter/setter
        if (fieldInfo.getModifiers().contains(Modifier.PUBLIC)) {
            return true;
        }
        // setter更容易失败
        return fieldInfo.hasPublicGetter() && fieldInfo.hasPublicSetter();
    }

    /** 是否是托管写的字段 */
    boolean isAutoWriteField(AptFieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.isSingleton()) {
            return false;
        }
        // 优先判断skip属性
        if (isSkipFields(fieldInfo, aptClassProps)) {
            return false;
        }
        return true;
    }

    /** 是否是托管读的字段 */
    boolean isAutoReadField(AptFieldInfo fieldInfo, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.isSingleton()) {
            return false;
        }
        // final必定或构造方法读
        if (fieldInfo.getModifiers().contains(Modifier.FINAL)) {
            return false;
        }
        // 优先判断skip属性
        if (isSkipFields(fieldInfo, aptClassProps)) {
            return false;
        }
        return true;
    }

    private boolean isSkipFields(AptFieldInfo fieldInfo, AptClassProps aptClassProps) {
        if (aptClassProps.skipFields.isEmpty()) {
            return false;
        }
        if (aptClassProps.skipFields.contains("*")) {
            return true;
        }
        String fieldName = fieldInfo.name;
        if (aptClassProps.skipFields.contains(fieldName)) {
            return true; // 完全匹配
        }
        if (!aptClassProps.clippedSkipFields.contains(fieldName)) {
            return false; // 简单名不存在
        }
        // 测试SimpleClassName和FullClassName
        TypeElement declaredTypeElement = (TypeElement) fieldInfo.element.getEnclosingElement();
        String simpleClassName = declaredTypeElement.getSimpleName().toString();
        if (aptClassProps.skipFields.contains(simpleClassName + "." + fieldName)) {
            return true;
        }
        String fullClassName = declaredTypeElement.getQualifiedName().toString();
        if (aptClassProps.skipFields.contains(fullClassName + "." + fieldName)) {
            return true;
        }
        return false;
    }
    // endregion

    // endregion

    // region 类型测试

    protected boolean isString(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_String);
    }

    protected boolean isBinary(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_Binary);
    }

    protected boolean isObjectPtr(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_Ptr);
    }

    protected boolean isLocalDateTime(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_LocalDateTime);
    }

    protected boolean isTimestamp(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_Timestamp);
    }

    protected boolean isByteArray(TypeMirror typeMirror) {
        return AptUtils.isByteArray(typeMirror);
    }

    protected boolean isEnumSet(TypeMirror typeMirror) {
        return typeMirror == type_EnumSet || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, type_EnumSet);
    }

    protected boolean isEnumMap(TypeMirror typeMirror) {
        return typeMirror == type_EnumMap || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, type_EnumMap);
    }
    // endregion

    // region overriding util

    public MethodSpec newGetEncoderTypeMethod(DeclaredType superDeclaredType, ClassName rawTypeName) {
        return MethodSpec.overriding(getEncoderTypeMethod, superDeclaredType, typeUtils)
                .addStatement("return encoderType") // final字段
                .addAnnotation(AptUtils.ANNOTATION_NONNULL)
                .build();
    }

    public MethodSpec.Builder newNewInstanceMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(newInstanceMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newReadFieldsMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(readFieldsMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newAfterDecodeMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(afterDecodeMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newBeforeEncodeMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(beforeEncodeMethod, superDeclaredType, typeUtils);
    }

    public MethodSpec.Builder newWriteFieldsMethodBuilder(DeclaredType superDeclaredType) {
        return MethodSpec.overriding(writeFieldsMethod, superDeclaredType, typeUtils);
    }

    // endregion

}