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

import javax.annotation.concurrent.Immutable;

/**
 * 对象轻量指针，
 * ObjectPtr是{@link ObjectPtr}的特化版，用于自定义序列化压缩空间。
 *
 * @author wjybxx
 * date - 2024/4/26
 */
@Immutable
public class ObjectLitePtr {

    /** 引用对象的本地id -- 如果目标对象是容器中的一员，该值是其容器内编号 */
    private final long localId;
    /** 引用对象所属的命名空间 */
    private final String namespace;
    /** 引用的对象的大类型 -- 给业务使用的，用于快速引用分析 */
    private final byte type;
    /** 引用的解析策略 -- 自定义解析规则 */
    private final byte policy;

    public ObjectLitePtr(long localId) {
        this(localId, null, (byte) 0, (byte) 0);
    }

    public ObjectLitePtr(long localId, String namespace) {
        this(localId, namespace, (byte) 0, (byte) 0);
    }

    public ObjectLitePtr(long localId, String namespace, byte type, byte policy) {
        if (localId < 0) throw new IllegalArgumentException("localId cant be negative, v: " + localId);
        this.localId = localId;
        this.namespace = namespace == null ? "" : namespace;
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
        return localId == 0 && ObjectUtils.isBlank(namespace);
    }

    public boolean hasLocalId() {
        return localId != 0;
    }

    public boolean hasNamespace() {
        return !ObjectUtils.isBlank(namespace);
    }

    public long getLocalId() {
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

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectLitePtr objectLitePtr = (ObjectLitePtr) o;

        if (localId != objectLitePtr.localId) return false;
        if (type != objectLitePtr.type) return false;
        if (policy != objectLitePtr.policy) return false;
        return namespace.equals(objectLitePtr.namespace);
    }

    @Override
    public int hashCode() {
        int result = (int) (localId ^ (localId >>> 32));
        result = 31 * result + namespace.hashCode();
        result = 31 * result + type;
        result = 31 * result + policy;
        return result;
    }

    // endregion

    @Override
    public String toString() {
        return "ObjectLitePtr{" +
                "localId=" + localId +
                ", namespace=" + namespace +
                ", type=" + type +
                ", policy=" + policy +
                '}';
    }

}