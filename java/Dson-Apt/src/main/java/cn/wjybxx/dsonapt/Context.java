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

import com.squareup.javapoet.AnnotationSpec;
import com.squareup.javapoet.TypeSpec;

import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.Element;
import javax.lang.model.element.TypeElement;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.DeclaredType;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date - 2023/12/10
 */
class Context {

    final TypeElement typeElement;
    AnnotationMirror dsonSerialAnnoMirror;
    AnnotationMirror linkerGroupAnnoMirror;
    AnnotationMirror linkerBeanAnnoMirror;

    // region cache
    List<? extends Element> allFieldsAndMethodWithInherit; // 所有的字段和方法缓存
    List<VariableElement> allFields; // 所有的实例字段缓存

    AptClassProps aptClassProps; // 类注解缓存
    List<AnnotationSpec> additionalAnnotations; // 生成代码附加注解
    final Map<VariableElement, AptFieldProps> fieldPropsMap = new HashMap<>(); // 字段的注解缓存
    final List<VariableElement> serialFields = new ArrayList<>(); // 所有可序列化字段缓存，检测类型数据时写入
    // endregion

    TypeSpec.Builder typeBuilder;
    DeclaredType superDeclaredType;
    String outPackage; // 输出目录

    public Context(TypeElement typeElement) {
        this.typeElement = typeElement;
    }
}