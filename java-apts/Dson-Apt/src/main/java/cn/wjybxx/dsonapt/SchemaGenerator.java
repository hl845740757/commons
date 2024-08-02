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
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.*;
import javax.tools.Diagnostic;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

/**
 * 方法对象
 *
 * @author wjybxx
 * date - 2023/12/10
 */
class SchemaGenerator extends AbstractGenerator<CodecProcessor> {

    private final Context context;
    private final ClassName typeInfoRawTypeName;

    public SchemaGenerator(CodecProcessor processor, Context context) {
        super(processor, context.typeElement);
        this.context = context;
        this.typeInfoRawTypeName = processor.typeName_TypeInfo;
    }

    @Override
    public void execute() {
        final List<VariableElement> allSerialFields = context.serialFields;
        final List<AptTypeInfo> allAptTypeInfos = new ArrayList<>(allSerialFields.size());
        for (VariableElement variableElement : allSerialFields) {
            AptTypeInfo typeInfo = parseTypeArgMirrors(variableElement);
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
        context.typeBuilder.addFields(typesFields)
                .addFields(factoryFields)
                .addFields(namesSpec);
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

    private List<FieldSpec> genTypeFields(List<VariableElement> allSerialFields, List<AptTypeInfo> allAptTypeInfos) {
        List<FieldSpec> typeFieldList = new ArrayList<>(allSerialFields.size() * 2);
        for (int i = 0; i < allSerialFields.size(); i++) {
            VariableElement variableElement = allSerialFields.get(i);
            AptTypeInfo aptTypeInfo = allAptTypeInfos.get(i);
            typeFieldList.add(genTypeField(variableElement, aptTypeInfo));
        }
        return typeFieldList;
    }

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
        ParameterizedTypeName fieldTypeName = ParameterizedTypeName.get(AptUtils.CLSNAME_SUPPLIER,
                TypeName.get(variableElement.asType()));
        String factoryFieldName = getFactoryFieldName(variableElement.getSimpleName().toString());
        FieldSpec.Builder builder = FieldSpec.builder(fieldTypeName, factoryFieldName, AptUtils.PUBLIC_STATIC_FINAL);

        if (aptTypeInfo.type == AptTypeInfo.TYPE_MAP) {
            if (processor.isEnumMap(aptTypeInfo.impl)) {
                builder.initializer("() -> new EnumMap<>($T.class)",
                        TypeName.get(typeUtils.erasure(aptTypeInfo.typeArgs.get(0))));
            } else {
                builder.initializer("$T::new",
                        TypeName.get(typeUtils.erasure(aptTypeInfo.impl)));
            }
        } else if (aptTypeInfo.type == AptTypeInfo.TYPE_COLLECTION) {
            if (processor.isEnumSet(aptTypeInfo.impl)) {
                builder.initializer("() -> EnumSet.noneOf($T.class)",
                        TypeName.get(typeUtils.erasure(aptTypeInfo.typeArgs.get(0))));
            } else {
                builder.initializer("$T::new",
                        TypeName.get(typeUtils.erasure(aptTypeInfo.impl)));
            }
        } else {
            // 其它类型字段
            builder.initializer("$T::new",
                    TypeName.get(typeUtils.erasure(aptTypeInfo.impl)));
        }
        return builder.build();
    }

    private FieldSpec genTypeField(VariableElement variableElement, AptTypeInfo aptTypeInfo) {
        ParameterizedTypeName fieldTypeName;
        if (variableElement.asType().getKind().isPrimitive()) {
            // 基础类型不能做泛型参数...
            fieldTypeName = ParameterizedTypeName.get(typeInfoRawTypeName,
                    TypeName.get(variableElement.asType()).box());
        } else {
            // TypeInfo的泛型T最好不带泛型参数，兼容性很差
            fieldTypeName = ParameterizedTypeName.get(typeInfoRawTypeName,
                    TypeName.get(typeUtils.erasure(variableElement.asType())));
        }
        String typeInfoFieldName = getTypeInfoFieldName(variableElement.getSimpleName().toString());
        FieldSpec.Builder builder = FieldSpec.builder(fieldTypeName, typeInfoFieldName, AptUtils.PUBLIC_STATIC_FINAL);
        switch (aptTypeInfo.typeArgs.size()) {
            case 0: {
                builder.initializer("$T.of($T.class)",
                        typeInfoRawTypeName,
                        TypeName.get(typeUtils.erasure(aptTypeInfo.declared)));
                break;
            }
            case 1: {
                builder.initializer("$T.of($T.class, $T.class)",
                        typeInfoRawTypeName,
                        TypeName.get(typeUtils.erasure(aptTypeInfo.declared)),
                        TypeName.get(typeUtils.erasure(aptTypeInfo.typeArgs.get(0))));
                break;
            }
            case 2: {
                builder.initializer("$T.of($T.class, $T.class, $T.class)",
                        typeInfoRawTypeName,
                        TypeName.get(typeUtils.erasure(aptTypeInfo.declared)),
                        TypeName.get(typeUtils.erasure(aptTypeInfo.typeArgs.get(0))),
                        TypeName.get(typeUtils.erasure(aptTypeInfo.typeArgs.get(1))));
                break;
            }
            default: {
                // 超过2个泛型参数时，使用List.of
                StringBuilder format = new StringBuilder("$T.of($T.class, List.Of(");
                List<Object> params = new ArrayList<>(aptTypeInfo.typeArgs.size() + 2);
                params.add(typeInfoRawTypeName);
                params.add(TypeName.get(typeUtils.erasure(aptTypeInfo.declared)));
                for (int i = 0; i < aptTypeInfo.typeArgs.size(); i++) {
                    if (i > 0) {
                        format.append(", ");
                    }
                    TypeMirror typeArg = aptTypeInfo.typeArgs.get(i);
                    format.append("$T.class");
                    params.add(typeUtils.erasure(typeArg));
                }
                format.append(")");
                builder.initializer(format.toString(), params.toArray());
                break;
            }
        }
        return builder.build();
    }

    private AptTypeInfo parseTypeArgMirrors(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        // 普通类型字段 -- 也需要解析泛型参数
        if (typeMirror.getKind().isPrimitive()) {
            return AptTypeInfo.of(typeMirror, null);
        }
        // 数组支持impl
        AptFieldProps properties = context.fieldPropsMap.get(variableElement);
        if (typeMirror.getKind() == TypeKind.ARRAY) {
            return AptTypeInfo.of(variableElement.asType(), properties.implMirror);
        }
        // 统一检查impl的合法性
        if (properties.implMirror != null
                && !AptUtils.isSubTypeIgnoreTypeParameter(typeUtils, properties.implMirror, variableElement.asType())) {
            messager.printMessage(Diagnostic.Kind.ERROR, "The implementation type must be a subtype of the declared type", variableElement);
        }
        if (processor.isMap(typeMirror)) {
            return parseMapTypeInfo(variableElement, properties);
        }
        if (processor.isCollection(typeMirror)) {
            return parseCollectionTypeInfo(variableElement, properties);
        }
        final DeclaredType declaredType = (DeclaredType) typeMirror;
        final List<? extends TypeMirror> typeArguments = erasureTypeArguments(declaredType.getTypeArguments());
        return AptTypeInfo.of(declaredType, typeArguments, properties.implMirror);
    }

    /** 解析map的类型信息 */
    private AptTypeInfo parseMapTypeInfo(VariableElement variableElement, AptFieldProps properties) {
        // 查找传递给Map接口的KV泛型参数
//        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, variableElement.asType(), processor.mapTypeMirror);
//        List<? extends TypeMirror> typeArguments = erasureTypeArguments(superTypeMirror.getTypeArguments());
//        if (typeArguments.size() != 2) {
//            messager.printMessage(Diagnostic.Kind.ERROR, "Can't find key or value type of map", variableElement);
//        }
        final DeclaredType declaredType = (DeclaredType) variableElement.asType();
        final List<? extends TypeMirror> typeArguments = erasureTypeArguments(declaredType.getTypeArguments());
        final TypeMirror realImplMirror = parseMapVarImpl(variableElement, properties);
        return AptTypeInfo.ofMap(variableElement.asType(), typeArguments, realImplMirror);
    }

    /** 解析Collection的类型信息 */
    private AptTypeInfo parseCollectionTypeInfo(VariableElement variableElement, AptFieldProps properties) {
        // 查找传递给Collection接口的E泛型参数
//        final DeclaredType superTypeMirror = AptUtils.upwardToSuperTypeMirror(typeUtils, variableElement.asType(), processor.collectionTypeMirror);
//        final List<? extends TypeMirror> typeArguments = erasureTypeArguments(superTypeMirror.getTypeArguments());
//        if (typeArguments.size() != 1) {
//            messager.printMessage(Diagnostic.Kind.ERROR, "Can't find element type of collection", variableElement);
//        }
        final DeclaredType declaredType = (DeclaredType) variableElement.asType();
        final List<? extends TypeMirror> typeArguments = erasureTypeArguments(declaredType.getTypeArguments());
        TypeMirror realImplMirror = parseCollectionVarImpl(variableElement, properties);
        return AptTypeInfo.ofCollection(variableElement.asType(), typeArguments, realImplMirror);
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
        if (processor.isEnumMap(typeMirror)) {
            return processor.type_EnumMap; // EnumMap不需要解析
        }

        final DeclaredType declaredType = AptUtils.findDeclaredType(variableElement.asType());
        assert declaredType != null;
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
        if (processor.isEnumSet(typeMirror)) {
            return processor.type_EnumSet; // EnumSet不需要解析
        }

        final DeclaredType declaredType = AptUtils.findDeclaredType(variableElement.asType());
        assert declaredType != null;
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

    /** 擦除泛型参数 */
    private List<? extends TypeMirror> erasureTypeArguments(List<? extends TypeMirror> typeArguments) {
        List<TypeMirror> result = new ArrayList<>(typeArguments.size());
        for (TypeMirror typeArgument : typeArguments) {
            if (typeArgument.getKind() == TypeKind.WILDCARD) {
                // 通配符 —— ? extends XXX，其上界就可以看做声明类型
                WildcardType wildcardType = (WildcardType) typeArgument;
                if (wildcardType.getExtendsBound() != null) {
                    result.add(typeUtils.erasure(wildcardType.getExtendsBound()));
                } else {
                    result.add(processor.type_Object);
                }
            } else if (typeArgument.getKind() == TypeKind.TYPEVAR) {
                // 泛型变量 —— T extends XXX，其上界就可以看做声明类型
                TypeVariable typeVariable = (TypeVariable) typeArgument;
                result.add(typeUtils.erasure(typeVariable.getUpperBound()));
            } else {
                result.add(typeUtils.erasure(typeArgument));
            }
        }
        return result;
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

}