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
import com.squareup.javapoet.TypeName;

import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.AnnotationValue;
import javax.lang.model.element.Element;
import javax.lang.model.element.TypeElement;
import javax.lang.model.type.TypeMirror;
import java.util.List;
import java.util.Objects;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * 我们将Class的所有信息都收集该类上，这样可以更好的支持LinkerBean。
 *
 * @author wjybxx
 * date 2023/4/13
 */
class AptClassProps {

    /** 获取单例的方法名 */
    public String singleton;
    /** skip指定的字段（不自动序列化的字段），可能为{@code ClassName.FieldName}等格式。 */
    public Set<String> skipFields = Set.of();
    /** 裁剪过的字段名，去掉了类名，只包含FieldName -- 以确定是否进行类名测试 */
    public transient Set<String> clippedSkipFields = Set.of();
    /** 需要为生成类附加的注解 -- Class数组 */
    public List<TypeMirror> additionalAnnotations = List.of();

    /** 编解码代理类 -- LinkerBean */
    public transient TypeElement codecProxyTypeElement;
    public transient TypeName codecProxyClassName;
    public transient List<? extends Element> codecProxyEnclosedElements;

    public AptClassProps() {
    }

    public boolean isSingleton() {
        return !AptUtils.isBlank(singleton);
    }

    public static AptClassProps parse(AnnotationMirror annotationMirror) {
        Objects.requireNonNull(annotationMirror, "annotationMirror");
        final AptClassProps props = new AptClassProps();
        props.singleton = AptUtils.getAnnotationValueValue(annotationMirror, "singleton", "").trim();
        // 解析需要跳过的字段 -- 数组属性总是返回List<AnnotationValue>
        List<AnnotationValue> skipFields = AptUtils.getAnnotationValueValue(annotationMirror, "skipFields", List.of());
        if (!skipFields.isEmpty()) {
            props.skipFields = skipFields.stream()
                    .map(e -> (String) e.getValue())
                    .collect(Collectors.toSet());
            // 截取FieldName
            props.clippedSkipFields = props.skipFields.stream()
                    .map(e -> {
                        int index = e.lastIndexOf('.');
                        return index < 0 ? e : e.substring(index + 1);
                    })
                    .collect(Collectors.toUnmodifiableSet());
        }
        // 解析附加注解 - 简单注解
        List<AnnotationValue> annotations = AptUtils.getAnnotationValueValue(annotationMirror, "annotations", List.of());
        if (!annotations.isEmpty()) {
            props.additionalAnnotations = annotations.stream()
                    .map(AptUtils::getAnnotationValueTypeMirror)
                    .toList();
        }
        return props;
    }

}
