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
import cn.wjybxx.apt.BeanUtils;
import com.squareup.javapoet.ClassName;
import com.squareup.javapoet.FieldSpec;
import com.squareup.javapoet.MethodSpec;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.*;
import javax.lang.model.type.DeclaredType;
import javax.lang.model.type.TypeKind;
import javax.lang.model.type.TypeMirror;
import java.util.EnumMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/13
 */
class PojoCodecGenerator extends AbstractGenerator<CodecProcessor> {

    private final Context context;
    private final TypeSpec.Builder typeBuilder;
    private final List<? extends Element> allFieldsAndMethodWithInherit;

    private ClassName rawTypeName;
    private boolean containsReaderConstructor;
    private boolean containsNewInstanceMethod;
    private boolean containsReadObjectMethod;
    private boolean containsWriteObjectMethod;
    private boolean containsBeforeEncodeMethod;
    private boolean containsAfterDecodeMethod;

    private MethodSpec.Builder newInstanceMethodBuilder;
    private MethodSpec.Builder readFieldsMethodBuilder;
    private MethodSpec.Builder afterDecodeMethodBuilder;
    private MethodSpec.Builder beforeEncodeMethodBuilder;
    private MethodSpec.Builder writeFieldsMethodBuilder;

    public PojoCodecGenerator(CodecProcessor processor, Context context) {
        super(processor, context.typeElement);
        this.context = context;
        this.typeBuilder = context.typeBuilder;
        this.allFieldsAndMethodWithInherit = context.allFieldsAndMethodWithInherit;
    }

    // region codec

    @Override
    public void execute() {
        init();
        gen();
    }

    /** 子类需要初始化 fieldsClassName */
    protected void init() {
        rawTypeName = ClassName.get(typeElement);
        containsReaderConstructor = processor.containsReaderConstructor(typeElement);
        containsNewInstanceMethod = processor.containsNewInstanceMethod(typeElement);
        containsReadObjectMethod = processor.containsReadObjectMethod(allFieldsAndMethodWithInherit);
        containsWriteObjectMethod = processor.containsWriteObjectMethod(allFieldsAndMethodWithInherit);
        containsBeforeEncodeMethod = processor.containsBeforeEncodeMethod(allFieldsAndMethodWithInherit);
        containsAfterDecodeMethod = processor.containsAfterDecodeMethod(allFieldsAndMethodWithInherit);

        // 需要先初始化superDeclaredType
        DeclaredType superDeclaredType = context.superDeclaredType;
        newInstanceMethodBuilder = processor.newNewInstanceMethodBuilder(superDeclaredType);
        readFieldsMethodBuilder = processor.newReadFieldsMethodBuilder(superDeclaredType);
        afterDecodeMethodBuilder = processor.newAfterDecodeMethodBuilder(superDeclaredType);
        beforeEncodeMethodBuilder = processor.newBeforeEncodeMethodBuilder(superDeclaredType);
        writeFieldsMethodBuilder = processor.newWriteFieldsMethodBuilder(superDeclaredType);
    }

    protected void gen() {
        // newInstance
        AptClassProps aptClassProps = context.aptClassProps;
        genNewInstanceMethod(aptClassProps);
        if (!aptClassProps.isSingleton()) {
            genWriteObjectMethod(aptClassProps);
            genReadObjectMethod(aptClassProps);
            // 普通字段读写
            for (VariableElement variableElement : context.serialFields) {
                final AptFieldProps aptFieldProps = context.fieldPropsMap.get(variableElement);
                if (processor.isAutoWriteField(variableElement, aptClassProps, aptFieldProps)) {
                    addWriteStatement(variableElement, aptFieldProps, aptClassProps);
                }
                if (processor.isAutoReadField(variableElement, aptClassProps, aptFieldProps)) {
                    addReadStatement(variableElement, aptFieldProps, aptClassProps);
                }
            }
        }

        // 控制方法生成顺序
        // typeInfo 字段
        typeBuilder.addField(FieldSpec.builder(processor.typeName_TypeInfo, "encoderType", Modifier.PRIVATE, Modifier.FINAL).build());
        // 生成默认构造函数，使用全局默认TypeInfo
        typeBuilder.addMethod(MethodSpec.constructorBuilder()
                .addModifiers(Modifier.PUBLIC)
                .addStatement("this.encoderType = " + SchemaGenerator.rawEncoderTypeFieldName())
                .build());
        // 再生成一个指定TypeInfo的工作函数 -- 非泛型类的抽象类也可能需要
        typeBuilder.addMethod(MethodSpec.constructorBuilder()
                .addModifiers(Modifier.PUBLIC)
                .addParameter(processor.typeName_TypeInfo, "encoderType")
                .addStatement("this.encoderType = encoderType")
                .build());

        // getEncoderType
        typeBuilder.addMethod(processor.newGetEncoderTypeMethod(context.superDeclaredType, rawTypeName));

        // beforeEncode回调
        if (genBeforeEncodeMethod(aptClassProps)) {
            typeBuilder.addMethod(beforeEncodeMethodBuilder.build());
        }
        typeBuilder.addMethod(writeFieldsMethodBuilder.build());
        typeBuilder.addMethod(newInstanceMethodBuilder.build())
                .addMethod(readFieldsMethodBuilder.build());
        // afterDecode回调
        if (genAfterDecodeMethod(aptClassProps)) {
            typeBuilder.addMethod(afterDecodeMethodBuilder.build());
        }

        // 额外注解
        if (context.additionalAnnotations != null) {
            typeBuilder.addAnnotations(context.additionalAnnotations);
        }
    }

    // region hook

    private static boolean containsHookMethod(AptClassProps aptClassProps, String methodName) {
        return aptClassProps.codecProxyEnclosedElements.stream()
                .filter(e -> e.getKind() == ElementKind.METHOD && e.getModifiers().contains(Modifier.STATIC))
                .anyMatch(e -> e.getSimpleName().toString().equals(methodName));
    }

    /** 调用用户的readObject方法 */
    private boolean genReadObjectMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyTypeElement != null) {
            if (containsHookMethod(aptClassProps, CodecProcessor.MNAME_READ_OBJECT)) {
                // CodecProxy.readObject(inst, reader);
                readFieldsMethodBuilder.addStatement("$T.$L(inst, reader)",
                        aptClassProps.codecProxyClassName, CodecProcessor.MNAME_READ_OBJECT);
                return true;
            }
        } else {
            if (containsReadObjectMethod) {
                // inst.readObject(reader);
                readFieldsMethodBuilder.addStatement("inst.$L(reader)",
                        CodecProcessor.MNAME_READ_OBJECT);
                return true;
            }
        }
        return false;
    }

    /** 调用用户的writeObject方法 */
    private boolean genWriteObjectMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyTypeElement != null) {
            if (containsHookMethod(aptClassProps, CodecProcessor.MNAME_WRITE_OBJECT)) {
                // CodecProxy.writeObject(inst, writer);
                writeFieldsMethodBuilder.addStatement("$T.$L(inst, writer)",
                        aptClassProps.codecProxyClassName, CodecProcessor.MNAME_WRITE_OBJECT);
                return true;
            }
        } else {
            if (containsWriteObjectMethod) {
                // inst.writeObject(writer);
                writeFieldsMethodBuilder.addStatement("inst.$L(writer)",
                        CodecProcessor.MNAME_WRITE_OBJECT);
                return true;
            }
        }
        return false;
    }

    /** 调用用户beforeEncode钩子方法 -- 需要支持codecProxy来处理 */
    private boolean genBeforeEncodeMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyTypeElement != null) {
            if (containsHookMethod(aptClassProps, CodecProcessor.MNAME_BEFORE_ENCODE)) {
                // CodecProxy.beforeEncode(inst, writer.options());
                beforeEncodeMethodBuilder.addStatement("$T.$L(inst, writer.options())",
                        aptClassProps.codecProxyClassName, CodecProcessor.MNAME_BEFORE_ENCODE);
                return true;
            }
        } else {
            if (containsBeforeEncodeMethod) {
                // inst.beforeEncode(writer.options());
                beforeEncodeMethodBuilder.addStatement("inst.$L(writer.options())",
                        CodecProcessor.MNAME_BEFORE_ENCODE);
                return true;
            }
        }
        return false;
    }

    /** 调用用户afterDecode钩子方法 -- 需要支持CodecProxy来处理 */
    private boolean genAfterDecodeMethod(AptClassProps aptClassProps) {
        if (aptClassProps.codecProxyTypeElement != null) {
            if (containsHookMethod(aptClassProps, CodecProcessor.MNAME_AFTER_DECODE)) {
                // CodecProxy.afterDecode(inst, reader.options());
                afterDecodeMethodBuilder.addStatement("$T.$L(inst, reader.options())",
                        aptClassProps.codecProxyClassName, CodecProcessor.MNAME_AFTER_DECODE);
                return true;
            }
        } else {
            if (containsAfterDecodeMethod) {
                // inst.afterDecode(reader.options());
                afterDecodeMethodBuilder.addStatement("inst.$L(reader.options())",
                        CodecProcessor.MNAME_AFTER_DECODE);
                return true;
            }
        }
        return false;
    }

    /** 调用用户的newInstance方法 */
    private void genNewInstanceMethod(AptClassProps aptClassProps) {
        if (aptClassProps.isSingleton()) {
            // 有CodecProxy的情况下，单例也交由CodecProxy实现 -- 方法名是CodecProxy指定的，因此应当存在，不做校验
            if (aptClassProps.codecProxyTypeElement != null) {
                newInstanceMethodBuilder.addStatement("return $T.$L()",
                        aptClassProps.codecProxyClassName, aptClassProps.singleton);
            } else {
                newInstanceMethodBuilder.addStatement("return $T.$L()",
                        rawTypeName, aptClassProps.singleton);
            }
            return;
        }
        // 理论上，如果当前类是泛型类，需要<>表示泛型，避免不必要的警告
        if (typeElement.getModifiers().contains(Modifier.ABSTRACT)) {// 抽象类
            newInstanceMethodBuilder.addStatement("throw new $T()", UnsupportedOperationException.class);
            return;
        }

        if (aptClassProps.codecProxyTypeElement != null) {
            if (containsHookMethod(aptClassProps, CodecProcessor.MNAME_NEW_INSTANCE)) {
                // CodecProxy.newInstance(reader, getEncoderType());
                newInstanceMethodBuilder.addStatement("return $T.$L(reader, $L())",
                        aptClassProps.codecProxyClassName, CodecProcessor.MNAME_NEW_INSTANCE, CodecProcessor.MNAME_GET_ENCODER_TYPE);
                return;
            }
        }
        if (containsNewInstanceMethod) { // 静态解析方法，优先级更高
            // MyBean.NewInstance(reader, getEncoderType());
            newInstanceMethodBuilder.addStatement("return $T.$L(reader, $L())", rawTypeName, CodecProcessor.MNAME_NEW_INSTANCE, CodecProcessor.MNAME_GET_ENCODER_TYPE);
        } else if (containsReaderConstructor) { // 解析构造方法
            // return new MyBean(reader, getEncoderType());
            newInstanceMethodBuilder.addStatement("return new $T(reader, $L())", rawTypeName, CodecProcessor.MNAME_GET_ENCODER_TYPE);
        } else {
            // MyBean.NewInstance();
            newInstanceMethodBuilder.addStatement("return new $T()", rawTypeName);
        }
    }
    // endregion

    // region field
    private void addReadStatement(VariableElement variableElement, AptFieldProps fieldProps, AptClassProps aptClassProps) {
        final String fieldName = variableElement.getSimpleName().toString();
        MethodSpec.Builder builder = readFieldsMethodBuilder;
        if (!AptUtils.isBlank(fieldProps.readProxy)) { // 自定义读
            if (aptClassProps.codecProxyTypeElement != null) {
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                builder.addStatement("$T.$L(inst, reader, $L)", aptClassProps.codecProxyClassName, fieldProps.readProxy, serialName(fieldName));
            } else {
                builder.addStatement("inst.$L(reader, $L)", fieldProps.readProxy, serialName(fieldName));
            }
            return;
        }
        final String readMethodName = getReadMethodName(variableElement);
        final ExecutableElement setterMethod = processor.findPublicSetter(variableElement, allFieldsAndMethodWithInherit);
        // 优先用setter，否则直接赋值
        boolean hasCustomSetter = !AptUtils.isBlank(fieldProps.setter);
        if (hasCustomSetter || setterMethod != null) {
            final String fieldAccess = hasCustomSetter ? fieldProps.setter : setterMethod.getSimpleName().toString();
            if (readMethodName.equals(MNAME_READ_OBJECT)) {
                // 读对象时要传入类型信息和Factory
                // inst.setName(reader.readObject(names_name, types_name, factories_name))
                if (fieldProps.implMirror != null) {
                    builder.addStatement("inst.$L(reader.$L($L, $L, $L))",
                            fieldAccess, readMethodName,
                            serialName(fieldName), serialTypeArg(fieldName), serialFactory(fieldName));
                } else {
                    builder.addStatement("inst.$L(reader.$L($L, $L, null))",
                            fieldAccess, readMethodName,
                            serialName(fieldName), serialTypeArg(fieldName));
                }
            } else {
                // inst.setName(reader.readString(names_.name))
                builder.addStatement("inst.$L(reader.$L($L))",
                        fieldAccess, readMethodName,
                        serialName(fieldName));
            }
        } else {
            if (readMethodName.equals(MNAME_READ_OBJECT)) {
                // 读对象时要传入类型信息和Factory
                // inst.name = reader.readObject(names_name, types_name, factories_name)
                if (fieldProps.implMirror != null) {
                    builder.addStatement("inst.$L = reader.$L($L, $L, $L)",
                            fieldName, readMethodName,
                            serialName(fieldName), serialTypeArg(fieldName), serialFactory(fieldName));
                } else {
                    builder.addStatement("inst.$L = reader.$L($L, $L, null)",
                            fieldName, readMethodName,
                            serialName(fieldName), serialTypeArg(fieldName));
                }
            } else {
                // inst.name = reader.readString(names_.name)
                builder.addStatement("inst.$L = reader.$L($L)",
                        fieldName, readMethodName,
                        serialName(fieldName));
            }
        }
    }

    private void addWriteStatement(VariableElement variableElement, AptFieldProps fieldProps, AptClassProps aptClassProps) {
        final String fieldName = variableElement.getSimpleName().toString();
        MethodSpec.Builder builder = this.writeFieldsMethodBuilder;
        if (!AptUtils.isBlank(fieldProps.writeProxy)) { // 自定义写
            if (aptClassProps.codecProxyTypeElement != null) {
                // 方法名是CodecProxy指定的，因此应当存在，不做校验
                builder.addStatement("$T.$L(inst, writer, $L)", aptClassProps.codecProxyClassName, fieldProps.writeProxy, serialName(fieldName));
            } else {
                builder.addStatement("inst.$L(writer, $L)", fieldProps.writeProxy, serialName(fieldName));
            }
            return;
        }
        // 优先用getter，否则直接访问
        String fieldAccess;
        boolean hasCustomGetter = !AptUtils.isBlank(fieldProps.getter);
        ExecutableElement getterMethod = processor.findPublicGetter(variableElement, allFieldsAndMethodWithInherit);
        if (hasCustomGetter) {
            fieldAccess = fieldProps.getter + "()";
        } else if (getterMethod != null) {
            fieldAccess = getterMethod.getSimpleName() + "()";
        } else {
            fieldAccess = fieldName;
        }

        // 处理数字 -- 涉及WireType和Style，short,byte,char不再指定WireType，意义不大
        final String writeMethodName = getWriteMethodName(variableElement);
        switch (variableElement.asType().getKind()) {
            case INT, LONG -> {
                // writer.writeInt(names_fieldName, inst.field, WireType.VARINT, NumberStyle.SIMPLE)
                builder.addStatement("writer.$L($L, inst.$L, $T.$L, $T.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess,
                        processor.typeName_WireType, fieldProps.wireType,
                        processor.typeName_NumberStyle, fieldProps.numberStyle);
                return;
            }
            case FLOAT, DOUBLE, SHORT, BYTE, CHAR -> {
                // writer.writeInt(names_fieldName, inst.field, NumberStyle.SIMPLE)
                builder.addStatement("writer.$L($L, inst.$L, $T.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess,
                        processor.typeName_NumberStyle, fieldProps.numberStyle);
                return;
            }
        }

        // 其它类型
        switch (writeMethodName) {
            case MNAME_WRITE_STRING -> {
                // writer.writeString(names_fieldName, inst.getName(), StringStyle.AUTO)
                builder.addStatement("writer.$L($L, inst.$L, $T.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess,
                        processor.typeName_StringStyle, fieldProps.stringStyle);
            }
            case MNAME_WRITE_OBJECT -> {
                // 写Object时传入类型信息和Style
                // writer.writeObject(names_fieldName, inst.getName(), types_name, ObjectStyle.INDENT)
                if (fieldProps.objectStyle != null) {
                    builder.addStatement("writer.$L($L, inst.$L, $L, $T.$L)",
                            writeMethodName, serialName(fieldName), fieldAccess, serialTypeArg(fieldName),
                            processor.typeName_ObjectStyle, fieldProps.objectStyle);
                } else {
                    builder.addStatement("writer.$L($L, inst.$L, $L, null)",
                            writeMethodName, serialName(fieldName), fieldAccess, serialTypeArg(fieldName));
                }
            }
            default -> {
                // writer.writeBytes(names_fieldName, inst.getName())
                // writer.writeBoolean(names_fieldName, inst.getName())
                builder.addStatement("writer.$L($L, inst.$L)",
                        writeMethodName, serialName(fieldName), fieldAccess);
            }
        }
    }

    // endregion

    // region util

    // 虽然多了临时字符串拼接，但可以大幅降低字符串模板的复杂度
    private String serialName(String fieldName) {
        return SchemaGenerator.nameFileName(fieldName);
    }

    private String serialTypeArg(String fieldName) {
        return SchemaGenerator.typeInfoFieldName(fieldName);
    }

    private String serialFactory(String fieldName) {
        return SchemaGenerator.factoryFieldName(fieldName);
    }

    /** 获取writer写字段的方法名 */
    private String getWriteMethodName(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        if (isPrimitiveType(typeMirror)) {
            return primitiveWriteMethodNameMap.get(typeMirror.getKind());
        }
        if (processor.isString(typeMirror)) {
            return MNAME_WRITE_STRING;
        }
        if (processor.isByteArray(typeMirror)) {
            return MNAME_WRITE_BYTES;
        }
        if (processor.isObjectPtr(typeMirror)) {
            return MNAME_WRITE_PTR;
        }
        if (processor.isObjectLitePtr(typeMirror)) {
            return MNAME_WRITE_LITE_PTR;
        }
        if (processor.isLocalDateTime(typeMirror)) {
            return MNAME_WRITE_DATETIME;
        }
        return MNAME_WRITE_OBJECT;
    }

    /** 获取reader读字段的方法名 */
    private String getReadMethodName(VariableElement variableElement) {
        TypeMirror typeMirror = variableElement.asType();
        if (isPrimitiveType(typeMirror)) {
            return primitiveReadMethodNameMap.get(typeMirror.getKind());
        }
        if (processor.isString(typeMirror)) {
            return MNAME_READ_STRING;
        }
        if (processor.isByteArray(typeMirror)) {
            return MNAME_READ_BYTES;
        }
        if (processor.isObjectPtr(typeMirror)) {
            return MNAME_READ_PTR;
        }
        if (processor.isObjectLitePtr(typeMirror)) {
            return MNAME_READ_LITE_PTR;
        }
        if (processor.isLocalDateTime(typeMirror)) {
            return MNAME_READ_DATETIME;
        }
        return MNAME_READ_OBJECT;
    }

    private static boolean isPrimitiveType(TypeMirror typeMirror) {
        return typeMirror.getKind().isPrimitive();
    }

    private static final String MNAME_READ_STRING = "readString";
    private static final String MNAME_READ_BYTES = "readBytes";
    private static final String MNAME_READ_OBJECT = "readObject";

    private static final String MNAME_READ_PTR = "readPtr";
    private static final String MNAME_READ_LITE_PTR = "readLitePtr";
    private static final String MNAME_READ_DATETIME = "readDateTime";
    private static final String MNAME_READ_TIMESTAMP = "readTimestamp";

    private static final String MNAME_WRITE_STRING = "writeString";
    private static final String MNAME_WRITE_BYTES = "writeBytes";
    private static final String MNAME_WRITE_OBJECT = "writeObject";

    private static final String MNAME_WRITE_PTR = "writePtr";
    private static final String MNAME_WRITE_LITE_PTR = "writeLitePtr";
    private static final String MNAME_WRITE_DATETIME = "writeDateTime";
    private static final String MNAME_WRITE_TIMESTAMP = "writeTimestamp";

    private static final Map<TypeKind, String> primitiveReadMethodNameMap = new EnumMap<>(TypeKind.class);
    private static final Map<TypeKind, String> primitiveWriteMethodNameMap = new EnumMap<>(TypeKind.class);

    static {
        for (TypeKind typeKind : TypeKind.values()) {
            if (!typeKind.isPrimitive()) {
                continue;
            }
            final String name = BeanUtils.firstCharToUpperCase(typeKind.name().toLowerCase());
            primitiveReadMethodNameMap.put(typeKind, "read" + name);
            primitiveWriteMethodNameMap.put(typeKind, "write" + name);
        }
    }
    // endregion
}
