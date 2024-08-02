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

package cn.wjybxx.dson.types;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.dson.DsonLites;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * 对象指针
 *
 * @author wjybxx
 * date - 2023/5/26
 */
@Immutable
public final class ObjectPtr {

    public static final int MASK_NAMESPACE = 1;
    public static final int MASK_TYPE = 1 << 1;
    public static final int MASK_POLICY = 1 << 2;

    /** 引用对象的本地id -- 如果目标对象是容器中的一员，该值是其容器内编号 */
    private final String localId;
    /** 引用对象所属的命名空间 */
    private final String namespace;
    /** 引用的对象的大类型 -- 给业务使用的，用于快速引用分析 */
    private final byte type;
    /** 引用的解析策略 -- 自定义解析规则 */
    private final byte policy;

    public ObjectPtr(String localId) {
        this(localId, null, (byte) 0, (byte) 0);
    }

    public ObjectPtr(String localId, String namespace) {
        this(localId, namespace, (byte) 0, (byte) 0);
    }

    public ObjectPtr(String localId, String namespace, byte type, byte policy) {
        this.localId = ObjectUtils.nullToDef(localId, "");
        this.namespace = ObjectUtils.nullToDef(namespace, "");
        this.type = type;
        this.policy = policy;

        if (isEmpty() && (type != 0 || policy != 0)) {
            throw new IllegalStateException();
        }
    }

    public boolean canBeAbbreviated() {
        return ObjectUtils.isBlank(namespace) && type == 0 && policy == 0;
    }

    public boolean isEmpty() {
        // localId一般不为空，放前面测试
        return ObjectUtils.isBlank(localId) && ObjectUtils.isBlank(namespace);
    }

    public boolean hasLocalId() {
        return !ObjectUtils.isBlank(localId);
    }

    public boolean hasNamespace() {
        return !ObjectUtils.isBlank(namespace);
    }

    public String getLocalId() {
        return localId;
    }

    public String getNamespace() {
        return namespace;
    }

    public byte getPolicy() {
        return policy;
    }

    public byte getType() {
        return type;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectPtr objectPtr = (ObjectPtr) o;

        if (type != objectPtr.type) return false;
        if (policy != objectPtr.policy) return false;
        if (!Objects.equals(namespace, objectPtr.namespace)) return false;
        return Objects.equals(localId, objectPtr.localId);
    }

    @Override
    public int hashCode() {
        int result = namespace != null ? namespace.hashCode() : 0;
        result = 31 * result + (localId != null ? localId.hashCode() : 0);
        result = 31 * result + type;
        result = 31 * result + policy;
        return result;
    }
    // endregion

    @Override
    public String toString() {
        return "ObjectPtr{" +
                "namespace='" + namespace + '\'' +
                ", localId='" + localId + '\'' +
                ", type=" + type +
                ", policy=" + policy +
                '}';
    }

    // 属性名
    public static final String NAMES_NAMESPACE = "ns";
    public static final String NAMES_LOCAL_ID = "localId";
    public static final String NAMES_TYPE = "type";
    public static final String NAMES_POLICY = "policy";

    public static final int NUMBERS_NAMESPACE = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_LOCAL_ID = DsonLites.makeFullNumberZeroIdep(1);
    public static final int NUMBERS_TYPE = DsonLites.makeFullNumberZeroIdep(2);
    public static final int NUMBERS_POLICY = DsonLites.makeFullNumberZeroIdep(3);
}