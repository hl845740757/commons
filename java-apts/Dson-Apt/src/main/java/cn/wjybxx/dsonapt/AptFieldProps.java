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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.lang.model.element.AnnotationMirror;
import javax.lang.model.element.AnnotationValue;
import javax.lang.model.element.VariableElement;
import javax.lang.model.type.TypeMirror;
import javax.lang.model.util.Types;
import java.util.List;
import java.util.Map;

/**
 * 我们将字段的所有信息都收集该类上，这样可以更好的支持LinkerBean。
 * 因此该类关联的几个注解的数据，而不仅仅是{@code FieldProps}。
 *
 * @author wjybxx
 * date 2023/4/6
 */
class AptFieldProps {

    public static final String TYPE_END_OF_OBJECT = "END_OF_OBJECT";
    public static final String TYPE_BINARY = "BINARY";
    public static final String TYPE_EXT_STRING = "EXT_STRING";
    public static final String TYPE_EXT_INT32 = "EXT_INT32";
    public static final String TYPE_EXT_INT64 = "EXT_INT64";
    public static final String TYPE_EXT_DOUBLE = "EXT_DOUBLE";

    public static final String DEFAULT_NUMBER_STYLE = "SIMPLE";
    public static final String DEFAULT_STRING_STYLE = "AUTO";

    /** 字段序列化时的名字 */
    public String name = "";
    /** 取值方法 */
    public String getter = "";
    /** 赋值方法 */
    public String setter = "";

    /** 实现类 */
    public TypeMirror implMirror;
    /** 写代理方法名 */
    public String writeProxy = "";
    /** 读代理方法名 */
    public String readProxy = "";

    /** 绑定类型 */
    public String dsonType = null; // 该属性只有显式声明才有效
    public int dsonSubType = 0;

    /** 绑定style */
    public String numberStyle = DEFAULT_NUMBER_STYLE;
    public String stringStyle = DEFAULT_STRING_STYLE;
    public String objectStyle = null; // 该属性只有显式声明才有效

    /** 序列化是否忽略 -- null表示未指定 */
    public Boolean ignore;

    // region parse
    @Nonnull
    public static AptFieldProps parse(Types typeUtils, VariableElement element, TypeMirror annoMirror) {
        final AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, element, annoMirror);
        if (annotationMirror == null) {
            return new AptFieldProps();
        }

        final AptFieldProps props = new AptFieldProps();
        final Map<String, AnnotationValue> annoValueMap = AptUtils.getAnnotationValuesMap(annotationMirror);
        props.name = getStringValue(annoValueMap, "name", props.name);
        props.getter = getStringValue(annoValueMap, "getter", props.getter);
        props.setter = getStringValue(annoValueMap, "setter", props.setter);

        props.dsonType = getEnumConstantName(annoValueMap, "dsonType", null);
        props.dsonSubType = getIntValue(annoValueMap, "dsonSubType", props.dsonSubType);

        props.numberStyle = getEnumConstantName(annoValueMap, "numberStyle", props.numberStyle);
        props.stringStyle = getEnumConstantName(annoValueMap, "stringStyle", props.stringStyle);
        props.objectStyle = getEnumConstantName(annoValueMap, "objectStyle", props.objectStyle);

        props.writeProxy = getStringValue(annoValueMap, "writeProxy", props.writeProxy);
        props.readProxy = getStringValue(annoValueMap, "readProxy", props.readProxy);
        final AnnotationValue impl = annoValueMap.get("impl");
        if (impl != null) {
            props.implMirror = AptUtils.getAnnotationValueTypeMirror(impl);
        }
        return props;
    }

    public void parseIgnore(Types typeUtils, VariableElement element, TypeMirror ignoreTypeMirror) {
        AnnotationMirror annotationMirror = AptUtils.findAnnotation(typeUtils, element, ignoreTypeMirror);
        if (annotationMirror != null) {
            this.ignore = AptUtils.getAnnotationValueValue(annotationMirror, "value");
        }
    }

    private static String getStringValue(Map<String, AnnotationValue> annoValueMap, String pname, String def) {
        AnnotationValue annoValue = annoValueMap.get(pname);
        if (annoValue == null) return def;
        String str = (String) annoValue.getValue();
        return str.trim();
    }

    private static Integer getIntValue(Map<String, AnnotationValue> annoValueMap, String pname, Integer def) {
        AnnotationValue annoValue = annoValueMap.get(pname);
        if (annoValue == null) return def;
        return (Integer) annoValue.getValue();
    }

    private static Boolean getBoolValue(Map<String, AnnotationValue> annoValueMap, String pname, Boolean def) {
        AnnotationValue annoValue = annoValueMap.get(pname);
        if (annoValue == null) return def;
        return (Boolean) annoValue.getValue();
    }

    private static String getEnumConstantName(Map<String, AnnotationValue> annoValueMap, String pname, String def) {
        AnnotationValue annoValue = annoValueMap.get(pname);
        if (annoValue == null) return def;

        VariableElement enumConstant = (VariableElement) getFirstValue(annoValue);
        if (enumConstant == null) return def;
        return enumConstant.getSimpleName().toString();
    }

    private static Object getFirstValue(@Nullable AnnotationValue annoValue) {
        if (annoValue == null) return null;
        Object objValue = annoValue.getValue();
        if (objValue instanceof List<?> list) {
            if (list.isEmpty()) return null;
            annoValue = (AnnotationValue) list.get(0);
            return annoValue.getValue();
        }
        return objValue;
    }
    // endregion
}