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
    public static final String CNAME_WireType = "cn.wjybxx.dson.WireType";
    public static final String CNAME_NumberStyle = "cn.wjybxx.dson.text.NumberStyle";
    public static final String CNAME_StringStyle = "cn.wjybxx.dson.text.StringStyle";
    public static final String CNAME_ObjectStyle = "cn.wjybxx.dson.text.ObjectStyle";
    public static final String CNAME_TypeInfo = "cn.wjybxx.dsoncodec.TypeInfo";
    public static final String CNAME_Options = "cn.wjybxx.dsoncodec.ConverterOptions";

    public static final String CNAME_ObjectPtr = "cn.wjybxx.dson.types.ObjectPtr";
    public static final String CNAME_ObjectLitePtr = "cn.wjybxx.dson.types.ObjectLitePtr";

    // Dson
    private static final String CNAME_SERIALIZABLE = "cn.wjybxx.dsoncodec.annotations.DsonSerializable";
    public static final String CNAME_PROPERTY = "cn.wjybxx.dsoncodec.annotations.DsonProperty";
    private static final String CNAME_DSON_IGNORE = "cn.wjybxx.dsoncodec.annotations.DsonIgnore";
    private static final String CNAME_DSON_READER = "cn.wjybxx.dsoncodec.DsonObjectReader";
    private static final String CNAME_DSON_WRITER = "cn.wjybxx.dsoncodec.DsonObjectWriter";
    private static final String CNAME_DSON_SCAN_IGNORE = "cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore";
    // Linker
    private static final String CNAME_CODEC_LINKER_GROUP = "cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerGroup";
    private static final String CNAME_CODEC_LINKER = "cn.wjybxx.dsoncodec.annotations.DsonCodecLinker";
    private static final String CNAME_CODEC_LINKER_BEAN = "cn.wjybxx.dsoncodec.annotations.DsonCodecLinkerBean";
    private static final String MNAME_OUTPUT = "outputPackage";
    private static final String MNAME_CLASS_PROPS = "props";

    // Codec
    public static final String CNAME_CODEC = "cn.wjybxx.dsoncodec.DsonCodec";
    public static final String MNAME_READ_OBJECT = "readObject";
    public static final String MNAME_WRITE_OBJECT = "writeObject";
    // AbstractCodec
    private static final String CNAME_ABSTRACT_CODEC = "cn.wjybxx.dsoncodec.AbstractDsonCodec";
    public static final String MNAME_GET_ENCODER_CLASS = "getEncoderClass";
    public static final String MNAME_BEFORE_ENCODE = "beforeEncode";
    public static final String MNAME_WRITE_FIELDS = "writeFields";
    public static final String MNAME_NEW_INSTANCE = "newInstance";
    public static final String MNAME_READ_FIELDS = "readFields";
    public static final String MNAME_AFTER_DECODE = "afterDecode";
    // EnumCode
    private static final String CNAME_ENUM_CODEC = "cn.wjybxx.dsoncodec.codecs.EnumCodec";
    public static final String CNAME_ENUM_LITE = "cn.wjybxx.base.EnumLite";
    public static final String MNAME_FOR_NUMBER = "forNumber";
    public static final String MNAME_GET_NUMBER = "getNumber";

    //endregion

    // region 字段
    public ClassName typeName_TypeInfo;
    public ClassName typeName_WireType;
    public ClassName typeName_NumberStyle;
    public ClassName typeName_StringStyle;
    public ClassName typeName_ObjectStyle;
    public TypeMirror type_Options;

    // Dson
    public TypeElement anno_DsonSerializable;
    public TypeMirror anno_DsonProperty;
    public TypeMirror anno_DsonIgnore;
    public TypeMirror typeMirror_DsonReader;
    public TypeMirror typeMirror_dsonWriter;

    // linker
    public TypeElement anno_CodecLinkerGroup;
    public TypeElement anno_CodecLinker;
    public TypeElement anno_CodecLinkerBean;

    // abstractCodec
    public TypeElement abstractCodecTypeElement;
    public ExecutableElement getEncoderClassMethod;
    public ExecutableElement newInstanceMethod;
    public ExecutableElement readFieldsMethod;
    public ExecutableElement afterDecodeMethod;
    public ExecutableElement beforeEncodeMethod;
    public ExecutableElement writeFieldsMethod;

    // enumCodec
    public TypeElement type_EnumCodec;

    // 特殊类型依赖
    // 基础类型
    public TypeMirror type_String;
    public TypeMirror type_Object;
    public TypeMirror type_EnumLite;
    public TypeMirror type_Ptr;
    public TypeMirror type_LitePtr;
    public TypeMirror type_LocalDateTime;

    // 集合类型
    public TypeMirror type_Map;
    public TypeMirror type_Collection;
    public TypeMirror type_Set;
    public TypeMirror type_EnumSet;
    public TypeMirror type_EnumMap;
    public TypeMirror type_LinkedHashMap;
    public TypeMirror type_LinkedHashSet;
    public TypeMirror type_ArrayList;

    // endregion

    public CodecProcessor() {
    }

    @Override
    public Set<String> getSupportedAnnotationTypes() {
        return Set.of(CNAME_SERIALIZABLE, CNAME_CODEC_LINKER_GROUP, CNAME_CODEC_LINKER_BEAN);
    }

    @Override
    protected void ensureInited() {
        if (typeName_WireType != null) return;
        // common
        typeName_TypeInfo = ClassName.get(elementUtils.getTypeElement(CNAME_TypeInfo));
        typeName_WireType = AptUtils.classNameOfCanonicalName(CNAME_WireType);
        typeName_NumberStyle = AptUtils.classNameOfCanonicalName(CNAME_NumberStyle);
        typeName_StringStyle = AptUtils.classNameOfCanonicalName(CNAME_StringStyle);
        typeName_ObjectStyle = AptUtils.classNameOfCanonicalName(CNAME_ObjectStyle);
        type_Options = elementUtils.getTypeElement(CNAME_Options).asType();

        // dson
        anno_DsonSerializable = elementUtils.getTypeElement(CNAME_SERIALIZABLE);
        anno_DsonProperty = elementUtils.getTypeElement(CNAME_PROPERTY).asType();
        anno_DsonIgnore = elementUtils.getTypeElement(CNAME_DSON_IGNORE).asType();
        typeMirror_DsonReader = elementUtils.getTypeElement(CNAME_DSON_READER).asType();
        typeMirror_dsonWriter = elementUtils.getTypeElement(CNAME_DSON_WRITER).asType();
        // linker
        anno_CodecLinkerGroup = elementUtils.getTypeElement(CNAME_CODEC_LINKER_GROUP);
        anno_CodecLinker = elementUtils.getTypeElement(CNAME_CODEC_LINKER);
        anno_CodecLinkerBean = elementUtils.getTypeElement(CNAME_CODEC_LINKER_BEAN);

        // Codec
        TypeElement codecTypeElement = elementUtils.getTypeElement(CNAME_CODEC);
        getEncoderClassMethod = AptUtils.findMethodByName(codecTypeElement, MNAME_GET_ENCODER_CLASS);
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
        // enumLiteCodec
        type_EnumCodec = elementUtils.getTypeElement(CNAME_ENUM_CODEC);

        // 特殊类型依赖
        // 基础类型
        type_String = elementUtils.getTypeElement(String.class.getCanonicalName()).asType();
        type_Object = elementUtils.getTypeElement(Object.class.getCanonicalName()).asType();
        type_EnumLite = elementUtils.getTypeElement(CNAME_ENUM_LITE).asType();
        type_Ptr = elementUtils.getTypeElement(CNAME_ObjectPtr).asType();
        type_LitePtr = elementUtils.getTypeElement(CNAME_ObjectLitePtr).asType();
        type_LocalDateTime = elementUtils.getTypeElement(LocalDateTime.class.getCanonicalName()).asType();

        // 集合
        type_Map = elementUtils.getTypeElement(Map.class.getCanonicalName()).asType();
        type_Collection = elementUtils.getTypeElement(Collection.class.getCanonicalName()).asType();
        type_Set = elementUtils.getTypeElement(Set.class.getCanonicalName()).asType();
        type_EnumSet = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumSet.class));
        type_EnumMap = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, EnumMap.class));
        type_LinkedHashMap = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, LinkedHashMap.class));
        type_LinkedHashSet = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, LinkedHashSet.class));
        type_ArrayList = typeUtils.erasure(AptUtils.getTypeMirrorOfClass(elementUtils, ArrayList.class));
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
                Context context = createContext(typeElement);
                // 判断是哪类注解 -- LinkerBean外部代理优先级最高
                if (context.linkerBeanAnnoMirror != null) {
                    // 不是为自己生成，当前类是Codec配置类
                    processLinkerBean(context);
                } else if (context.linkerGroupAnnoMirror != null) {
                    // 不是为自己生成，而是为字段类型生成
                    processLinkerGroup(context);
                } else {
                    assert context.dsonSerialAnnoMirror != null;
                    processDirectType(context);
                }
            } catch (Throwable e) {
                messager.printMessage(Diagnostic.Kind.ERROR, AptUtils.getStackTrace(e), typeElement);
            }
        }
        return true;
    }

    private Context createContext(TypeElement typeElement) {
        Context context = new Context(typeElement);
        context.dsonSerialAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_DsonSerializable.asType());
        if (context.dsonSerialAnnoMirror != null) {
            return context;
        }
        context.linkerBeanAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_CodecLinkerBean.asType());
        if (context.linkerBeanAnnoMirror != null) {
            return context;
        }
        context.linkerGroupAnnoMirror = AptUtils.findAnnotation(typeUtils, typeElement, anno_CodecLinkerGroup.asType());
        return context;
    }

    // region process

    private void processLinkerBean(Context linkerBeanContext) {
        final AnnotationMirror linkerBeanAnnoMirror = linkerBeanContext.linkerBeanAnnoMirror;
        final String outPackage = getOutputPackage(linkerBeanContext.typeElement, linkerBeanAnnoMirror);
        // 真实需要生成Codec的类型
        DeclaredType targetType;
        {
            AnnotationValue annotationValue = AptUtils.getAnnotationValue(linkerBeanAnnoMirror, "value");
            Objects.requireNonNull(annotationValue, "classProps");
            targetType = AptUtils.findDeclaredType(AptUtils.getAnnotationValueTypeMirror(annotationValue));
            Objects.requireNonNull(targetType);
        }
        AnnotationValue classPropsAnnoValue = AptUtils.getAnnotationValue(linkerBeanAnnoMirror, MNAME_CLASS_PROPS);
        AptClassProps aptClassProps = classPropsAnnoValue == null ?
                new AptClassProps() : AptClassProps.parse((AnnotationMirror) classPropsAnnoValue.getValue());

        // 创建模拟数据
        TypeElement targetTypeElement = (TypeElement) targetType.asElement();
        Context context = new Context(targetTypeElement);
        context.linkerBeanAnnoMirror = linkerBeanAnnoMirror;
        context.outPackage = outPackage;

        context.aptClassProps = aptClassProps;
        context.additionalAnnotations = getAdditionalAnnotations(aptClassProps);
        cacheFields(context);
        // 修正字段的Props注解 —— 将LinkerBean上的注解信息转移到目标类
        {
            cacheFields(linkerBeanContext);
            cacheFieldProps(linkerBeanContext);

            // 按name缓存，提高效率
            Map<String, AptFieldProps> fieldName2FieldPropsMap = HashMap.newHashMap(linkerBeanContext.fieldPropsMap.size());
            for (Map.Entry<VariableElement, AptFieldProps> entry : linkerBeanContext.fieldPropsMap.entrySet()) {
                fieldName2FieldPropsMap.put(entry.getKey().getSimpleName().toString(), entry.getValue());
            }
            for (VariableElement field : context.allFields) {
                AptFieldProps aptFieldProps = fieldName2FieldPropsMap.get(field.getSimpleName().toString());
                if (aptFieldProps == null) aptFieldProps = new AptFieldProps();
                context.fieldPropsMap.put(field, aptFieldProps);
            }
        }
        // 绑定CodecProxy
        {
            TypeMirror linkerBeanTypeMirror = linkerBeanContext.typeElement.asType();
            aptClassProps.codecProxyTypeElement = linkerBeanContext.typeElement;
            aptClassProps.codecProxyClassName = TypeName.get(typeUtils.erasure(linkerBeanTypeMirror));
            aptClassProps.codecProxyEnclosedElements = linkerBeanContext.typeElement.getEnclosedElements();
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

    private void processLinkerGroup(Context groupContext) {
        final String outPackage = getOutputPackage(groupContext.typeElement, groupContext.linkerGroupAnnoMirror);

        cacheFields(groupContext);
        for (VariableElement variableElement : groupContext.allFields) {
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
            Context context = new Context(typeElement);
            context.linkerGroupAnnoMirror = groupContext.linkerGroupAnnoMirror;
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

    private void processDirectType(Context context) {
        cacheFields(context);
        cacheFieldProps(context);
        context.aptClassProps = AptClassProps.parse(context.dsonSerialAnnoMirror);
        context.additionalAnnotations = getAdditionalAnnotations(context.aptClassProps);
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
        if (isEnumLite(typeElement.asType())) {
            DeclaredType superDeclaredType = typeUtils.getDeclaredType(type_EnumCodec, typeUtils.erasure(typeElement.asType()));
            initTypeBuilder(context, typeElement, superDeclaredType);
            // 生成枚举Codec
            new EnumCodecGenerator(this, typeElement, context).execute();
        } else {
            DeclaredType superDeclaredType = typeUtils.getDeclaredType(abstractCodecTypeElement, typeUtils.erasure(typeElement.asType()));
            initTypeBuilder(context, typeElement, superDeclaredType);
            // 先生成常量字段
            SchemaGenerator schemaGenerator = new SchemaGenerator(this, context);
            schemaGenerator.execute();
            // 再生成PojoCodec
            new PojoCodecGenerator(this, context).execute();
        }
        // 写入文件
        if (context.outPackage != null) {
            AptUtils.writeToFile(typeElement, context.typeBuilder, context.outPackage, messager, filer);
        } else {
            AptUtils.writeToFile(typeElement, context.typeBuilder, elementUtils, messager, filer);
        }
    }

    private void cacheFields(Context context) {
        context.allFieldsAndMethodWithInherit = BeanUtils.getAllFieldsAndMethodsWithInherit(context.typeElement);
        context.allFields = context.allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.FIELD && !e.getModifiers().contains(Modifier.STATIC))
                .map(e -> (VariableElement) e)
                .toList();
    }

    private void cacheFieldProps(Context context) {
        for (VariableElement variableElement : context.allFields) {
            AptFieldProps aptFieldProps = AptFieldProps.parse(typeUtils, variableElement, anno_DsonProperty);
            // dsonIgnore
            aptFieldProps.parseIgnore(typeUtils, variableElement, anno_DsonIgnore);

            context.fieldPropsMap.put(variableElement, aptFieldProps);
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

    private void checkTypeElement(Context context) {
        TypeElement typeElement = context.typeElement;
        if (!isClassOrEnum(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR, "unsupported type", typeElement);
            return;
        }
        if (typeElement.getKind() == ElementKind.ENUM) {
            checkEnum(typeElement);
        } else {
            checkNormalClass(context);
        }
    }

    // region 枚举检查

    /**
     * 检查枚举 - 要自动序列化的枚举，必须实现EnumLite接口且提供forNumber方法。
     */
    private void checkEnum(TypeElement typeElement) {
        if (!isEnumLite(typeElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum must implement EnumLite",
                    typeElement);
            return;
        }
        if (!containNotPrivateStaticForNumberMethod(typeElement)) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "serializable enum must contains a not private 'static T forNumber(int)' method!",
                    typeElement);
        }
    }

    /**
     * 是否包含静态的非private的forNumber方法
     */
    private boolean containNotPrivateStaticForNumberMethod(TypeElement typeElement) {
        return typeElement.getEnclosedElements().stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .anyMatch(e -> e.getModifiers().contains(Modifier.PUBLIC)
                        && e.getModifiers().contains(Modifier.STATIC)
                        && e.getParameters().size() == 1
                        && e.getSimpleName().toString().equals(MNAME_FOR_NUMBER)
                        && e.getParameters().get(0).asType().getKind() == TypeKind.INT);
    }
    // endregion

    // region 普通类检查

    private void checkNormalClass(Context context) {
        final AptClassProps aptClassProps = context.aptClassProps;
        if (aptClassProps.isSingleton()) {
            return;
        }
        TypeElement typeElement = context.typeElement;
        checkConstructor(typeElement);

        final List<? extends Element> allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
        final List<? extends Element> instMethodList = context.allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD && !e.getModifiers().contains(Modifier.STATIC))
                .toList();

        for (VariableElement variableElement : context.allFields) {
            final AptFieldProps aptFieldProps = context.fieldPropsMap.get(variableElement);
            if (!isSerializableField(variableElement, instMethodList, aptFieldProps)) {
                continue;
            }
            context.serialFields.add(variableElement);

            if (isAutoWriteField(variableElement, aptClassProps, aptFieldProps)) {
                if (!AptUtils.isBlank(aptFieldProps.writeProxy)) {
                    continue;
                }
                // 工具写：需要提供可直接取值或包含非private的getter方法
                if (AptUtils.isBlank(aptFieldProps.getter)
                        && !canGetDirectly(variableElement)
                        && findPublicGetter(variableElement, allFieldsAndMethodWithInherit) == null) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("auto write field (%s) must be public or contains a public getter", variableElement.getSimpleName()),
                            typeElement); // 可能无法定位到超类字段，因此打印到Type
                    continue;
                }
            }
            if (isAutoReadField(variableElement, aptClassProps, aptFieldProps)) {
                if (!AptUtils.isBlank(aptFieldProps.readProxy)) {
                    continue;
                }
                // 工具读：需要提供可直接赋值或非private的setter方法
                if (AptUtils.isBlank(aptFieldProps.setter)
                        && !canSetDirectly(variableElement)
                        && findPublicSetter(variableElement, allFieldsAndMethodWithInherit) == null) {
                    messager.printMessage(Diagnostic.Kind.ERROR,
                            String.format("auto read field (%s) must be public or contains a public getter", variableElement.getSimpleName()),
                            typeElement); // 可能无法定位到超类字段，因此打印到Type
                    continue;
                }
            }
        }
    }

    /** 检查是否包含无参构造方法或解析构造方法 */
    private void checkConstructor(TypeElement typeElement) {
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {
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
    public boolean containsReadObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return containsHookMethod(allFieldsAndMethodWithInherit, MNAME_READ_OBJECT, typeMirror_DsonReader);
    }

    /** 是否包含 writeObject(writer) 实例方法 */
    public boolean containsWriteObjectMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return containsHookMethod(allFieldsAndMethodWithInherit, MNAME_WRITE_OBJECT, typeMirror_dsonWriter);
    }

    /** 是否包含 beforeEncode 实例方法 */
    public boolean containsBeforeEncodeMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return containsHookMethod(allFieldsAndMethodWithInherit, MNAME_BEFORE_ENCODE, type_Options);
    }

    /** 是否包含 afterDecode 实例方法 */
    public boolean containsAfterDecodeMethod(List<? extends Element> allFieldsAndMethodWithInherit) {
        return containsHookMethod(allFieldsAndMethodWithInherit, MNAME_AFTER_DECODE, type_Options);
    }

    private boolean containsHookMethod(List<? extends Element> allFieldsAndMethodWithInherit, String methodName, TypeMirror argTypeMirror) {
        return allFieldsAndMethodWithInherit.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD)
                .map(e -> (ExecutableElement) e)
                .anyMatch(e -> e.getModifiers().contains(Modifier.PUBLIC)
                        && e.getSimpleName().toString().equals(methodName)
                        && e.getParameters().size() > 0 // 有时可能需要多个参数
                        && AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, e.getParameters().get(0).asType(), argTypeMirror));
    }

    // endregion

    // region 字段检查

    /**
     * 测试是否可以直接读取字段。
     *
     * @param variableElement 类字段，可能是继承的字段
     * @return 如果可直接取值，则返回true
     */
    public boolean canGetDirectly(final VariableElement variableElement) {
        return variableElement.getModifiers().contains(Modifier.PUBLIC);
    }

    /**
     * 测试是否可以直接写字段。
     *
     * @param variableElement 类字段，可能是继承的字段
     * @return 如果可直接赋值，则返回true
     */
    public boolean canSetDirectly(final VariableElement variableElement) {
        if (variableElement.getModifiers().contains(Modifier.FINAL)) {
            return false;
        }
        return variableElement.getModifiers().contains(Modifier.PUBLIC);
    }

    /** 字段是否是类的成员或同包类的成员 -- 兼容性不好，生成的Codec可能在其它包 */
    @Deprecated
    private boolean isMemberOrPackageMember(VariableElement variableElement, TypeElement typeElement) {
        final TypeElement enclosingElement = (TypeElement) variableElement.getEnclosingElement();
        if (enclosingElement.equals(typeElement)) {
            return true;
        }
        return elementUtils.getPackageOf(enclosingElement).equals(elementUtils.getPackageOf(typeElement));
    }

    /**
     * 查找非private的getter方法
     *
     * @param allMethodWithInherit 所有的字段和方法，可能在父类中
     */
    public ExecutableElement findPublicGetter(final VariableElement variableElement, final List<? extends Element> allMethodWithInherit) {
        return BeanUtils.findPublicGetter(typeUtils, variableElement, allMethodWithInherit);
    }

    /**
     * 查找非private的setter方法
     *
     * @param allMethodWithInherit 所有的字段和方法，可能在父类中
     */
    public ExecutableElement findPublicSetter(final VariableElement variableElement, final List<? extends Element> allMethodWithInherit) {
        return BeanUtils.findPublicSetter(typeUtils, variableElement, allMethodWithInherit);
    }

    /**
     * 是否是可序列化的字段
     * 1.默认只序列化 public 字段
     * 2.默认忽略 transient 字段
     */
    private boolean isSerializableField(VariableElement variableElement, List<? extends Element> instMethodList, AptFieldProps aptFieldProps) {
        if (variableElement.getModifiers().contains(Modifier.STATIC)) {
            return false;
        }
        // 有注解的情况下，取决于注解的值 -- 取反。。。
        Boolean ignore = aptFieldProps.dsonIgnore;
        if (ignore != null) return !ignore;
        // 无注解的情况下，默认忽略 transient 字段
        if (variableElement.getModifiers().contains(Modifier.TRANSIENT)) {
            return false;
        }
        // 判断public和getter/setter
        if (variableElement.getModifiers().contains(Modifier.PUBLIC)) {
            return true;
        }
        // setter更容易失败
        return BeanUtils.containsPublicSetter(typeUtils, variableElement, instMethodList)
                && BeanUtils.containsPublicGetter(typeUtils, variableElement, instMethodList);
    }

    /** 是否是托管写的字段 */
    boolean isAutoWriteField(VariableElement variableElement, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.isSingleton()) {
            return false;
        }
        // 优先判断skip属性
        if (isSkipFields(variableElement, aptClassProps)) {
            return false;
        }
        return true;
    }

    /** 是否是托管读的字段 */
    boolean isAutoReadField(VariableElement variableElement, AptClassProps aptClassProps, AptFieldProps aptFieldProps) {
        if (aptClassProps.isSingleton()) {
            return false;
        }
        // final必定或构造方法读
        if (variableElement.getModifiers().contains(Modifier.FINAL)) {
            return false;
        }
        // 优先判断skip属性
        if (isSkipFields(variableElement, aptClassProps)) {
            return false;
        }
        return true;
    }

    private boolean isSkipFields(VariableElement variableElement, AptClassProps aptClassProps) {
        if (aptClassProps.skipFields.isEmpty()) {
            return false;
        }
        String fieldName = variableElement.getSimpleName().toString();
        if (aptClassProps.skipFields.contains(fieldName)) {
            return true; // 完全匹配
        }
        if (!aptClassProps.clippedSkipFields.contains(fieldName)) {
            return false; // 简单名不存在
        }
        // 测试SimpleClassName和FullClassName
        TypeElement declaredTypeElement = (TypeElement) variableElement.getEnclosingElement();
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
    protected boolean isClassOrEnum(TypeElement typeElement) {
        return typeElement.getKind() == ElementKind.CLASS
                || typeElement.getKind() == ElementKind.ENUM;
    }

    protected boolean isString(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_String);
    }

    protected boolean isObjectPtr(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_Ptr);
    }

    protected boolean isObjectLitePtr(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_LitePtr);
    }

    protected boolean isLocalDateTime(TypeMirror typeMirror) {
        return typeUtils.isSameType(typeMirror, type_LocalDateTime);
    }

    protected boolean isByteArray(TypeMirror typeMirror) {
        return AptUtils.isByteArray(typeMirror);
    }

    protected boolean isEnumLite(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, type_EnumLite);
    }

    protected boolean isMap(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, type_Map);
    }

    protected boolean isCollection(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, type_Collection);
    }

    protected boolean isSet(TypeMirror typeMirror) {
        return AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, typeMirror, type_Set);
    }

    protected boolean isEnumSet(TypeMirror typeMirror) {
        return typeMirror == type_EnumSet || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, type_EnumSet);
    }

    protected boolean isEnumMap(TypeMirror typeMirror) {
        return typeMirror == type_EnumMap || AptUtils.isSameTypeIgnoreTypeParameter(typeUtils, typeMirror, type_EnumMap);
    }
    // endregion

    // region overriding util

    public MethodSpec newGetEncoderClassMethod(DeclaredType superDeclaredType, TypeName encoderTypeName) {
        return MethodSpec.overriding(getEncoderClassMethod, superDeclaredType, typeUtils)
                .addStatement("return $T.class", encoderTypeName)
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