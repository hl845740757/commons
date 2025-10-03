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

    public static final ObjectPtr EMPTY = new ObjectPtr(0);

    public static final int MASK_LOCAL_NAME = 1;
    public static final int MASK_NAMESPACE = 1 << 1;
    public static final int MASK_TYPE = 1 << 2;

    /** 引用对象的本地id */
    private final long localId;
    /** 引用对象的本地name - 优先级高于LocalId */
    private final String localName;
    /** 引用对象所属的命名空间 - 集合库/对象桶 */
    private final String namespace;
    /** 引用的对象的大类型 -- 给业务使用的，用于快速引用分析 */
    private final int type;

    public ObjectPtr(long localId) {
        this(localId, null, null, 0);
    }

    public ObjectPtr(long localId, String localName) {
        this(localId, localName, null, 0);
    }

    public ObjectPtr(long localId, String localName, String namespace, int type) {
        // 空字符串转null以兼容default构建的实例
        this.localId = localId;
        this.localName = ObjectUtils.emptyToDef(localName, null);
        this.namespace = ObjectUtils.emptyToDef(namespace, null);
        this.type = type;
        if (type != 0 && isEmpty()) {
            throw new IllegalArgumentException();
        }
    }

    public boolean isEmpty() {
        return localId == 0
                && ObjectUtils.isEmpty(localName)
                && ObjectUtils.isEmpty(namespace);
    }

    public boolean canBeAbbreviated() {
        return type == 0
                && ObjectUtils.isEmpty(localName)
                && ObjectUtils.isEmpty(namespace);
    }

    public boolean hasLocalId() {
        return localId != 0;
    }

    public boolean hasLocalName() {
        return !ObjectUtils.isEmpty(localName);
    }

    public boolean hasNamespace() {
        return !ObjectUtils.isEmpty(namespace);
    }

    public long getLocalId() {
        return localId;
    }

    public String getLocalName() {
        return localName;
    }

    public String getNamespace() {
        return namespace;
    }

    public int getType() {
        return type;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectPtr objectPtr = (ObjectPtr) o;
        return localId == objectPtr.localId
                && type == objectPtr.type
                && Objects.equals(localName, objectPtr.localName)
                && Objects.equals(namespace, objectPtr.namespace);
    }

    @Override
    public int hashCode() {
        int result = Long.hashCode(localId);
        result = 31 * result + Objects.hashCode(localName);
        result = 31 * result + Objects.hashCode(namespace);
        result = 31 * result + type;
        return result;
    }

    @Override
    public String toString() {
        return "ObjectPtr{" +
                "localId=" + localId +
                ", localName='" + localName + '\'' +
                ", namespace='" + namespace + '\'' +
                ", type=" + type +
                '}';
    }

    // endregion

    // 属性名
    public static final String NAMES_NAMESPACE = "ns";
    public static final String NAMES_LOCAL_ID = "localId";
    public static final String NAMES_LOCAL_NAME = "localName";
    public static final String NAMES_TYPE = "type";

    public static final int NUMBERS_NAMESPACE = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_LOCAL_ID = DsonLites.makeFullNumberZeroIdep(1);
    public static final int NUMBERS_LOCAL_NAME = DsonLites.makeFullNumberZeroIdep(2);
    public static final int NUMBERS_TYPE = DsonLites.makeFullNumberZeroIdep(3);
}