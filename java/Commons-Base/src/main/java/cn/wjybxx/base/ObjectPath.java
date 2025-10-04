/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

package cn.wjybxx.base;

import java.util.Objects;

/**
 * 资产对象路径(指针)
 * <p>
 * 注：该对象是Dson库中的ObjectPtr的替代物，用于避免引入Dson库。
 *
 * @author wjybxx
 * date - 2025/10/3
 */
public final class ObjectPath {
    /**
     * 目标对象所属的集合(文件路径、资产路径、db路径)
     * (如果为空，表示引用当前资产内的对象)
     */
    public String collection;
    /**
     * 对象在集合内的路径(或name)
     * (如果字段不为空，则优先使用localPath查找对象，即localPath的优先级高于localId)
     */
    public String localPath;
    /**
     * 对象在集合内的id
     * (如果目标集合是数组，则可能是下标)
     */
    public long localId;
    /**
     * 引用类型
     * (用于引用分析，也可以表示如何解析引用等)
     */
    public int type;

    /** 引用目标缓存，辅助字段 */
    public transient Object obj;

    public ObjectPath() {
    }

    public ObjectPath(long localId) {
        this.localId = localId;
    }

    public ObjectPath(String collection, String localPath, long localId) {
        this.collection = collection;
        this.localPath = localPath;
        this.localId = localId;
    }

    public ObjectPath(String collection, String localPath, long localId, int type) {
        this.collection = collection;
        this.localId = localId;
        this.localPath = localPath;
        this.type = type;
    }

    public boolean isEmpty() {
        return localId == 0
                && ObjectUtils.isEmpty(localPath)
                && ObjectUtils.isEmpty(collection);
    }

    public boolean hasLocalId() {
        return localId != 0;
    }

    public boolean hasLocalPath() {
        return !ObjectUtils.isEmpty(localPath);
    }

    public boolean hasAssetPath() {
        return !ObjectUtils.isEmpty(collection);
    }

    // region getter/setter

    public String getCollection() {
        return collection;
    }

    public void setCollection(String collection) {
        this.collection = collection;
    }

    public String getLocalPath() {
        return localPath;
    }

    public void setLocalPath(String localPath) {
        this.localPath = localPath;
    }

    public long getLocalId() {
        return localId;
    }

    public void setLocalId(long localId) {
        this.localId = localId;
    }

    public int getType() {
        return type;
    }

    public void setType(int type) {
        this.type = type;
    }

    // endregion

    // region equals
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ObjectPath objectPtr = (ObjectPath) o;
        return localId == objectPtr.localId
                && type == objectPtr.type
                && Objects.equals(localPath, objectPtr.localPath)
                && Objects.equals(collection, objectPtr.collection);
    }

    @Override
    public int hashCode() {
        int result = Long.hashCode(localId);
        result = 31 * result + Objects.hashCode(localPath);
        result = 31 * result + Objects.hashCode(collection);
        result = 31 * result + type;
        return result;
    }

    @Override
    public String toString() {
        return "ObjectPath{" +
                "localId=" + localId +
                ", localPath='" + localPath + '\'' +
                ", assetPath='" + collection + '\'' +
                ", type=" + type +
                '}';
    }
    // endregion
}