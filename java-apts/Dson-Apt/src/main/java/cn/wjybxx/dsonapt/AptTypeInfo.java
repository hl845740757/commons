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

import javax.lang.model.type.TypeMirror;
import java.util.List;

/**
 * @author wjybxx
 * date - 2024/4/3
 */
class AptTypeInfo {

    static final int TYPE_UNKNOWN = 0;
    static final int TYPE_COLLECTION = 1;
    static final int TYPE_MAP = 2;

    /** 判断类型 */
    final int type;
    /** 声明类型 */
    final TypeMirror declared;
    /** 泛型类型信息 */
    final List<? extends TypeMirror> typeArgs;
    /** 注解指定的实现类 - 可能为null */
    final TypeMirror impl;

    public AptTypeInfo(int type, TypeMirror declared, List<? extends TypeMirror> typeArgs, TypeMirror impl) {
        this.declared = declared;
        this.impl = impl;
        this.type = type;
        this.typeArgs = typeArgs == null ? List.of() : typeArgs;
    }

    public static AptTypeInfo of(TypeMirror declared, TypeMirror impl) {
        return new AptTypeInfo(TYPE_UNKNOWN, declared, List.of(), impl);
    }

    public static AptTypeInfo of(TypeMirror declared, List<? extends TypeMirror> typeArgs, TypeMirror impl) {
        return new AptTypeInfo(TYPE_UNKNOWN, declared, typeArgs, impl);
    }

    public static AptTypeInfo ofCollection(TypeMirror declared, List<? extends TypeMirror> typeArgs, TypeMirror impl) {
        return new AptTypeInfo(TYPE_COLLECTION, declared, typeArgs, impl);
    }

    public static AptTypeInfo ofMap(TypeMirror declared, List<? extends TypeMirror> typeArgs, TypeMirror impl) {
        return new AptTypeInfo(TYPE_MAP, declared, typeArgs, impl);
    }
}
