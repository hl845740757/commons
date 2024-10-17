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
        final List<FieldSpec> typesFields = genTypeFields(context.serialFields);
        final List<FieldSpec> factoryFields = genFactoryFields(context.serialFields);
        final List<FieldSpec> namesSpec = genNameFields();
        context.typeBuilder
                .addField(genRawEncoderTypeFiled())
                .addFields(typesFields)
                .addFields(factoryFields)
                .addFields(namesSpec);
    }

    static String rawEncoderTypeFieldName() {
        return "_rawEncoderType";
    }

    static String factoryFieldName(String fieldName) {
        return "factories_" + fieldName;
    }

    static String typeInfoFieldName(String fieldName) {
        return "types_" + fieldName;
    }

    static String nameFileName(String fieldName) {
        return "names_" + fieldName;
    }

    // region typeArgs

    private List<FieldSpec> genFactoryFields(List<VariableElement> allSerialFields) {
        List<FieldSpec> typeFieldList = new ArrayList<>(allSerialFields.size());
        for (VariableElement variableElement : allSerialFields) {
            AptFieldProps fieldProps = context.fieldPropsMap.get(variableElement);
            if (fieldProps.implMirror != null) {
                typeFieldList.add(genFactoryField(variableElement, fieldProps.implMirror));
            }
        }
        return typeFieldList;
    }

    private FieldSpec genFactoryField(VariableElement variableElement, TypeMirror implMirror) {
        // 暂不擦除泛型 -- 我们约定禁止字段出现未定义泛型，如：List<T>
        // public static final Supplier<Map<String, Object>> factories_map = HashMap::new;
        TypeMirror typeMirror = variableElement.asType();
        ParameterizedTypeName fieldTypeName = ParameterizedTypeName.get(AptUtils.CLSNAME_SUPPLIER, TypeName.get(typeMirror));
        String factoryFieldName = factoryFieldName(variableElement.getSimpleName().toString());

        FieldSpec.Builder builder = FieldSpec.builder(fieldTypeName, factoryFieldName, AptUtils.PUBLIC_STATIC_FINAL);
        if (processor.isEnumMap(implMirror)) {
            // EnumMap
            DeclaredType declaredType = (DeclaredType) typeMirror;
            builder.initializer("() -> new EnumMap<>($T.class)",
                    TypeName.get(typeUtils.erasure(declaredType.getTypeArguments().get(0))));
        } else if (processor.isEnumSet(implMirror)) {
            // EnumSet
            DeclaredType declaredType = (DeclaredType) typeMirror;
            builder.initializer("() -> EnumSet.noneOf($T.class)",
                    TypeName.get(typeUtils.erasure(declaredType.getTypeArguments().get(0))));
        } else {
            // 其它可直接New的类型
            builder.initializer("$T::new",
                    TypeName.get(typeUtils.erasure(implMirror)));
        }
        return builder.build();
    }

    private List<FieldSpec> genTypeFields(List<VariableElement> allSerialFields) {
        return allSerialFields.stream()
                .filter(e -> needTypeInfoFields(e.asType()))
                .map(this::genTypeField)
                .toList();
    }

    private boolean needTypeInfoFields(TypeMirror typeMirror) {
        // 有对应读写方法的类型不需要生成TypeInfo
        return !(typeMirror.getKind().isPrimitive()
                || processor.isString(typeMirror)
                || processor.isByteArray(typeMirror));
    }

    private FieldSpec genTypeField(VariableElement variableElement) {
        // public static final TypeInfo types_name = TypeInfo.of();
        TypeMirror typeMirror = variableElement.asType();
        String typeInfoFieldName = typeInfoFieldName(variableElement.getSimpleName().toString());

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
        format.append("$T.of($T.class, ");
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
        format.append(")");
    }

    /** 生成原始类型的TypeInfo字段 */
    private FieldSpec genRawEncoderTypeFiled() {
        // private static final TypeInfo _rawEncoderType = TypeInfo.of();
        FieldSpec.Builder builder = FieldSpec.builder(typeName_TypeInfo, rawEncoderTypeFieldName(), AptUtils.PRIVATE_STATIC_FINAL);
        List<? extends TypeParameterElement> typeParameters = typeElement.getTypeParameters();

        StringBuilder format = new StringBuilder(16);
        List<Object> params = new ArrayList<>(4);
        if (typeParameters.isEmpty()) {
            format.append("$T.of($T.class)");
            params.add(typeName_TypeInfo);
            params.add(TypeName.get(typeUtils.erasure(typeElement.asType())));
        } else {
            format.append("$T.of($T.class, ");
            params.add(typeName_TypeInfo);
            params.add(TypeName.get(typeUtils.erasure(typeElement.asType())));
            // 泛型参数直接擦除即可
            for (int i = 0; i < typeParameters.size(); i++) {
                if (i > 0) format.append(", ");
                format.append("$T.of($T.class)");
                params.add(typeName_TypeInfo);
                params.add(TypeName.get(typeUtils.erasure(typeParameters.get(i).asType())));
            }
            format.append(")");
        }
        builder.initializer(format.toString(), params.toArray());
        return builder.build();
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
            fieldSpecList.add(FieldSpec.builder(AptUtils.CLSNAME_STRING, nameFileName(fieldName), AptUtils.PUBLIC_STATIC_FINAL)
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