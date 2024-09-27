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

import cn.wjybxx.apt.AbstractGenerator;
import cn.wjybxx.apt.AptUtils;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.FieldSpec;
import com.squareup.javapoet.ParameterizedTypeName;
import com.squareup.javapoet.TypeName;

import javax.lang.model.element.Modifier;
import javax.lang.model.element.TypeElement;
import javax.lang.model.element.TypeParameterElement;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.*;
import javax.tools.Diagnostic;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * 方法对象
 * 1.要序列化的泛型类，其外部类不可以是泛型 -- 否则其泛型信息难以解析。
 * 2.
 *
 * @author wjybxx
 * date - 2023/12/10
 */
class SchemaGenerator extends AbstractGenerator<CodecProcessor> {

    private final Context context;
    private final ClassName typeName_TypeInfo;

    public SchemaGenerator(CodecProcessor processor, Context context) {
        super(processor, context.typeElement);
        this.context = context;
        this.typeName_TypeInfo = processor.typeName_TypeInfo;
    }

    @Override
    public void execute() {
        final List<VariableElement> allSerialFields = context.serialFields;
        final List<AptTypeInfo> allAptTypeInfos = new ArrayList<>(allSerialFields.size());
        for (VariableElement variableElement : allSerialFields) {
            AptTypeInfo typeInfo = parseTypeInfo(variableElement);
            allAptTypeInfos.add(typeInfo);
            // 修正impl属性
            AptFieldProps aptFieldProps = context.fieldPropsMap.get(variableElement);
            if (aptFieldProps != null) {
                aptFieldProps.implMirror = typeInfo.impl;
            }
        }
        final List<FieldSpec> typesFields = genTypeFields(allSerialFields, allAptTypeInfos);
        final List<FieldSpec> factoryFields = genFactoryFields(allSerialFields, allAptTypeInfos);
        final List<FieldSpec> namesSpec = genNameFields();
        context.typeBuilder
                .addField(genRawTypeInfoFiled())
                .addFields(typesFields)
                .addFields(factoryFields)
                .addFields(namesSpec);
    }

    static String getTypeInfoFieldName() {
        return "rawTypeInfo";
    }

    static String getFactoryFieldName(String fieldName) {
        return "factories_" + fieldName;
    }

    static String getTypeInfoFieldName(String fieldName) {
        return "types_" + fieldName;
    }

    static String getNameFileName(String fieldName) {
        return "names_" + fieldName;
    }

    // region typeArgs

    private List<FieldSpec> genFactoryFields(List<VariableElement> allSerialFields, List<AptTypeInfo> allAptTypeInfos) {
        List<FieldSpec> typeFieldList = new ArrayList<>(allSerialFields.size() * 2);
        for (int i = 0; i < allSerialFields.size(); i++) {
            VariableElement variableElement = allSerialFields.get(i);
            AptTypeInfo aptTypeInfo = allAptTypeInfos.get(i);
            if (aptTypeInfo.impl != null) {
                typeFieldList.add(genFactoryField(variableElement, aptTypeInfo));
            }
        }
        return typeFieldList;
    }

    private FieldSpec genFactoryField(VariableElement variableElement, AptTypeInfo aptTypeInfo) {
        // 暂不擦除泛型 -- 我们约定禁止字段出现未定义泛型，如：List<T>
        // public static final Supplier<Map<String, Object>> factories_map = HashMap::new;
        TypeMirror typeMirror = variableElement.asType();
        ParameterizedTypeName fieldTypeName = ParameterizedTypeName.get(AptUtils.CLSNAME_SUPPLIER, TypeName.get(typeMirror));
        String factoryFieldName = getFactoryFieldName(variableElement.getSimpleName().toString());

        FieldSpec.Builder builder = FieldSpec.builder(fieldTypeName, factoryFieldName, AptUtils.PUBLIC_STATIC_FINAL);
        if (aptTypeInfo.impl == processor.type_EnumMap) {
            // EnumMap
            DeclaredType declaredType = (DeclaredType) typeMirror;
            builder.initializer("() -> new EnumMap<>($T.class)",
                    TypeName.get(typeUtils.erasure(declaredType.getTypeArguments().get(0))));
        } else if (aptTypeInfo.impl == processor.type_EnumSet) {
            // EnumSet
            DeclaredType declaredType = (DeclaredType) typeMirror;
            builder.initializer("() -> EnumSet.noneOf($T.class)",
                    TypeName.get(typeUtils.erasure(declaredType.getTypeArguments().get(0))));
        } else {
            // 其它可直接New的类型
            builder.initializer("$T::new",
                    TypeName.get(typeUtils.erasure(aptTypeInfo.impl)));
        }
        return builder.build();
    }

    private List<FieldSpec> genTypeFields(List<VariableElement> allSerialFields, List<AptTypeInfo> allAptTypeInfos) {
        List<FieldSpec> typeFieldList = new ArrayList<>(allSerialFields.size() * 2);
        for (int i = 0; i < allSerialFields.size(); i++) {
            VariableElement variableElement = allSerialFields.get(i);
            AptTypeInfo aptTypeInfo = allAptTypeInfos.get(i);
            typeFieldList.add(genTypeField(variableElement, aptTypeInfo));
        }
        return typeFieldList;
    }

    private FieldSpec genTypeField(VariableElement variableElement, AptTypeInfo aptTypeInfo) {
        // public static final TypeInfo types_name = TypeInfo.of();
        TypeMirror typeMirror = variableElement.asType();
        String typeInfoFieldName = getTypeInfoFieldName(variableElement.getSimpleName().toString());

        FieldSpec.Builder builder = FieldSpec.builder(typeName_TypeInfo, typeInfoFieldName, AptUtils.PUBLIC_STATIC_FINAL);
        // 需要递归构建
        StringBuilder format = new StringBuilder(16);
        List<Object> params = new ArrayList<>(4);
        appendTypeInfo(typeMirror, format, params);
        builder.initializer(format.toString(), params.toArray());
        return builder.build();
    }

    private void appendTypeInfo(TypeMirror typeMirror, StringBuilder format, List<Object> params) {
        List<? extends TypeMirror> typeArguments = List.of();
        switch (typeMirror.getKind()) {
            case ARRAY -> {
                ArrayType arrayType = (ArrayType) typeMirror;
                typeArguments = getArrayTypeArguments(arrayType);
            }
            case DECLARED -> {
                DeclaredType declaredType = (DeclaredType) typeMirror;
                typeArguments = declaredType.getTypeArguments();
            }
        }
        if (typeArguments.isEmpty()) {
            format.append("$T.of($T.class)");
            params.add(typeName_TypeInfo);
            params.add(TypeName.get(typeUtils.erasure(typeMirror)));
            return;
        }
        boolean nested = format.length() > 0;
        if (nested) { // 递归时换行，否则生成代码太乱
            format.append("\n$>");
        }
        format.append("$T.of($T.class, List.of("); // List已import
        params.add(typeName_TypeInfo);
        params.add(TypeName.get(typeUtils.erasure(typeMirror)));
        // 泛型参数递归解析
        for (int i = 0; i < typeArguments.size(); i++) {
            if (i > 0) format.append(", ");
            TypeMirror constructedType = toConstructedType(typeArguments.get(i));
            appendTypeInfo(constructedType, format, params);
        }
        if (nested) {
            format.append("$<");
        }
        format.append("))");
    }

    /** 生成原始类型的TypeInfo字段 */
    private FieldSpec genRawTypeInfoFiled() {
        // private static final TypeInfo rawTypeInfo = TypeInfo.of();
        FieldSpec.Builder builder = FieldSpec.builder(typeName_TypeInfo, getTypeInfoFieldName(), AptUtils.PRIVATE_STATIC_FINAL);
        List<? extends TypeParameterElement> typeParameters = typeElement.getTypeParameters();

        StringBuilder format = new StringBuilder(16);
        List<Object> params = new ArrayList<>(4);
        if (typeParameters.isEmpty()) {
            format.append("$T.of($T.class, $T.of())"); // List需要import
            params.add(typeName_TypeInfo);
            params.add(TypeName.get(typeUtils.erasure(typeElement.asType())));
            params.add(AptUtils.CLSNAME_LIST);
        } else {
            format.append("$T.of($T.class, $T.of("); // List需要import
            params.add(typeName_TypeInfo);
            params.add(TypeName.get(typeUtils.erasure(typeElement.asType())));
            params.add(AptUtils.CLSNAME_LIST);
            // 泛型参数直接擦除即可
            for (int i = 0; i < typeParameters.size(); i++) {
                if (i > 0) format.append(", ");
                format.append("$T.of($T.class)");
                params.add(typeName_TypeInfo);
                params.add(TypeName.get(typeUtils.erasure(typeParameters.get(i).asType())));
            }
            format.append("))");
        }
        builder.initializer(format.toString(), params.toArray());
        return builder.build();
    }

    private AptTypeInfo parseTypeInfo(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        // 普通类型字段 -- 也需要解析泛型参数
        if (typeMirror.getKind().isPrimitive()) {
            return AptTypeInfo.of(typeMirror, null);
        }
        // 统一检查impl的合法性
        AptFieldProps properties = context.fieldPropsMap.get(variableElement);
        if (properties.implMirror != null
                && !AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, properties.implMirror, variableElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR, "The implementation type must be a subtype of the declared type", variableElement);
        }
        switch (typeMirror.getKind()) {
            case ARRAY -> {
                ArrayType constructedArrayType = (ArrayType) toConstructedType(typeMirror);
                return AptTypeInfo.of(constructedArrayType, properties.implMirror); // 数组也可指定impl
            }
            case DECLARED -> {
                if (processor.isMap(typeMirror)) {
                    return parseMapTypeInfo(variableElement, properties);
                }
                if (processor.isCollection(typeMirror)) {
                    return parseCollectionTypeInfo(variableElement, properties);
                }
                return AptTypeInfo.of(typeMirror, properties.implMirror);
            }
            default -> {
                // 可能是泛型T
                return AptTypeInfo.of(typeUtils.erasure(typeMirror), properties.implMirror);
            }
        }
    }

    /** 解析map的类型信息 */
    private AptTypeInfo parseMapTypeInfo(VariableElement variableElement, AptFieldProps properties) {
        final TypeMirror realImplMirror = parseMapVarImpl(variableElement, properties);
        return AptTypeInfo.ofMap(variableElement.asType(), realImplMirror);
    }

    /** 解析Collection的类型信息 */
    private AptTypeInfo parseCollectionTypeInfo(VariableElement variableElement, AptFieldProps properties) {
        final TypeMirror realImplMirror = parseCollectionVarImpl(variableElement, properties);
        return AptTypeInfo.ofCollection(variableElement.asType(), realImplMirror);
    }

    /** 解析map的实现类 */
    private TypeMirror parseMapVarImpl(VariableElement variableElement, AptFieldProps properties) {
        if (!AptUtils.isBlank(properties.readProxy)) {
            return null; // 有读代理，不需要解析
        }
        TypeMirror typeMirror = variableElement.asType();
        if (properties.implMirror != null) { // 具体类和抽象类都可以指定实现类，且优先级最高
            return properties.implMirror;
        }
        if (processor.isEnumMap(typeMirror)) { // EnumMap不需要解析
            return processor.type_EnumMap;
        }

        final DeclaredType declaredType = (DeclaredType) variableElement.asType();
        // 是具体类型-可直接构造
        if (!declaredType.asElement().getModifiers().contains(Modifier.ABSTRACT)) {
            return declaredType;
        }
        // 如果是抽象的，并且不是LinkedHashMap的超类，则抛出异常
        checkDefaultImpl(variableElement, processor.type_LinkedHashMap);
        return null;
    }

    /** 解析collection的实现类 */
    private TypeMirror parseCollectionVarImpl(VariableElement variableElement, AptFieldProps properties) {
        if (!AptUtils.isBlank(properties.readProxy)) {
            return null; // 有读代理，不需要解析
        }
        TypeMirror typeMirror = variableElement.asType();
        if (properties.implMirror != null) { // 具体类和抽象类都可以指定实现类，且优先级最高
            return properties.implMirror;
        }
        if (processor.isEnumSet(typeMirror)) { // EnumSet不需要解析
            return processor.type_EnumSet;
        }

        final DeclaredType declaredType = (DeclaredType) variableElement.asType();
        // 是具体类型-可直接构造
        if (!declaredType.asElement().getModifiers().contains(Modifier.ABSTRACT)) {
            return declaredType;
        }
        // 如果是抽象的，并且不是ArrayList/LinkedHashSet的超类，则抛出异常
        if (processor.isSet(typeMirror)) {
            checkDefaultImpl(variableElement, processor.type_LinkedHashSet);
        } else {
            checkDefaultImpl(variableElement, processor.type_ArrayList);
        }
        return null;
    }

    /** 检查字段是否是默认实现类型的超类 */
    private void checkDefaultImpl(VariableElement variableElement, TypeMirror defImpl) {
        if (!AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, defImpl, variableElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR,
                    "Unknown abstract Map or Collection must contains impl annotation " + CodecProcessor.CNAME_PROPERTY,
                    variableElement);
        }
    }

    // endregion

    // region names

    private List<FieldSpec> genNameFields() {
        final List<VariableElement> serialFields = context.serialFields;
        final Set<String> dsonNameSet = new HashSet<>((int) (serialFields.size() * 1.35f));
        final List<FieldSpec> fieldSpecList = new ArrayList<>(serialFields.size());

        for (VariableElement variableElement : serialFields) {
            AptFieldProps properties = context.fieldPropsMap.get(variableElement);
            String fieldName = variableElement.getSimpleName().toString();
            String dsonName;
            if (!AptUtils.isBlank(properties.name)) {
                dsonName = properties.name.trim();
            } else {
                dsonName = fieldName;
            }
            if (!dsonNameSet.add(dsonName)) {
                messager.printMessage(Diagnostic.Kind.ERROR,
                        String.format("dsonName is duplicate, dsonName %s", dsonName),
                        variableElement);
                continue;
            }
            fieldSpecList.add(FieldSpec.builder(AptUtils.CLSNAME_STRING, getNameFileName(fieldName), AptUtils.PUBLIC_STATIC_FINAL)
                    .initializer("$S", dsonName)
                    .build()
            );
        }
        return fieldSpecList;
    }

    // endregion

    // region util

    /** 转换为已构造类型 -- 不确定的类型的信息回被擦除；泛型信息回尽可能保留 */
    private TypeMirror toConstructedType(TypeMirror typeMirror) {
        if (typeMirror.getKind().isPrimitive()) {
            return typeMirror; // 基本类型数组可能走到这里
        }
        switch (typeMirror.getKind()) {
            case WILDCARD -> {
                // 通配符 —— ? extends XXX，其上界就可以看做声明类型
                WildcardType wildcardType = (WildcardType) typeMirror;
                if (wildcardType.getExtendsBound() != null) {
                    return typeUtils.erasure(wildcardType.getExtendsBound());
                } else {
                    return processor.type_Object;
                }
            }
            case TYPEVAR -> {
                // 泛型变量 —— T extends XXX，其上界就可以看做声明类型
                TypeVariable typeVariable = (TypeVariable) typeMirror;
                return typeUtils.erasure(typeVariable.getUpperBound());
            }
            case DECLARED -> {
                // Class或接口
                DeclaredType declaredType = (DeclaredType) typeMirror;
                if (isConstructedType(declaredType)) {
                    return declaredType;
                }
                // 递归擦除 -- 擦除所有泛型参数，再构造新的DeclaredType
                List<? extends TypeMirror> typeArguments = declaredType.getTypeArguments();
                TypeMirror[] copiedTypeArgs = new TypeMirror[typeArguments.size()];
                for (int i = 0; i < typeArguments.size(); i++) {
                    TypeMirror nestTypeMirror = typeArguments.get(i);
                    copiedTypeArgs[i] = toConstructedType(nestTypeMirror);
                }
                TypeElement element = (TypeElement) declaredType.asElement();
                if (declaredType.getEnclosingType() instanceof DeclaredType enclosingType) {
                    return typeUtils.getDeclaredType(enclosingType, element, copiedTypeArgs);
                } else {
                    return typeUtils.getDeclaredType(element, copiedTypeArgs);
                }
            }
            case ARRAY -> {
                ArrayType arrayType = (ArrayType) typeMirror;
                if (isConstructedType(arrayType)) {
                    return arrayType;
                }
                // 擦除rootComponent的泛型信息，然后再构造 -- 可能是T[]
                TypeMirror rootComponentType = AptUtils.getRootComponentType(arrayType);
                TypeMirror rootComponentType2 = toConstructedType(rootComponentType);

                int arrayRank = AptUtils.getArrayRank(arrayType);
                ArrayType copiedArrayType = typeUtils.getArrayType(rootComponentType2);
                for (int i = 1; i < arrayRank; i++) {
                    copiedArrayType = typeUtils.getArrayType(copiedArrayType);
                }
                return copiedArrayType;
            }
            case null, default -> {
                return typeUtils.erasure(typeMirror);
            }
        }
    }

    /** 是否是已构造类型 -- 泛型参数都已确定，不包含通配符 */
    private boolean isConstructedType(TypeMirror typeMirror) {
        if (typeMirror.getKind().isPrimitive()) {
            return true;
        }
        switch (typeMirror.getKind()) {
            case DECLARED -> {
                DeclaredType declaredType = (DeclaredType) typeMirror;
                List<? extends TypeMirror> typeArguments = declaredType.getTypeArguments();
                if (typeArguments.isEmpty()) {
                    return true;
                }
                for (int i = 0; i < typeArguments.size(); i++) {
                    TypeMirror nestTypeMirror = typeArguments.get(i);
                    if (!isConstructedType(nestTypeMirror)) {
                        return false;
                    }
                }
                return true;
            }
            case ARRAY -> {
                ArrayType arrayType = (ArrayType) typeMirror;
                TypeMirror rootComponentType = AptUtils.getRootComponentType(arrayType);
                return isConstructedType(rootComponentType);
            }
            case VOID, NONE, NULL -> {
                return true;
            }
            default -> {
                return false;
            }
        }
    }

    private static List<? extends TypeMirror> getArrayTypeArguments(ArrayType arrayType) {
        TypeMirror rootComponentType = AptUtils.getRootComponentType(arrayType); // ROOT可能是TypeVar
        if (rootComponentType instanceof DeclaredType declaredType) {
            return declaredType.getTypeArguments();
        }
        return List.of();
    }

    // endregion

}