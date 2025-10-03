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
     * 资产路径
     * (如果为空，表示引用当前资产内的对象)
     */
    public String assetPath;
    /**
     * 对象在资产内的名字
     * (如果name不为空，则使用name查找对象，即localName的优先级高于localId)
     */
    public String localName;
    /**
     * 对象在资产内的id
     * (如果目标资产是数组，则可能是下标)
     */
    public long localId;
    /**
     * 引用的类型
     */
    public int type;

    public ObjectPath() {
    }

    public ObjectPath(String assetPath, long localId, String localName) {
        this.assetPath = assetPath;
        this.localId = localId;
        this.localName = localName;
        this.type = 0;
    }

    public ObjectPath(String assetPath, long localId, String localName, int type) {
        this.assetPath = assetPath;
        this.localId = localId;
        this.localName = localName;
        this.type = type;
    }

    public boolean isEmpty() {
        return localId == 0
                && ObjectUtils.isEmpty(localName)
                && ObjectUtils.isEmpty(assetPath);
    }

    public boolean hasLocalId() {
        return localId != 0;
    }

    public boolean hasLocalName() {
        return !ObjectUtils.isEmpty(localName);
    }

    public boolean hasAssetPath() {
        return !ObjectUtils.isEmpty(assetPath);
    }

    // region getter/setter

    public String getAssetPath() {
        return assetPath;
    }

    public void setAssetPath(String assetPath) {
        this.assetPath = assetPath;
    }

    public String getLocalName() {
        return localName;
    }

    public void setLocalName(String localName) {
        this.localName = localName;
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
                && Objects.equals(localName, objectPtr.localName)
                && Objects.equals(assetPath, objectPtr.assetPath);
    }

    @Override
    public int hashCode() {
        int result = Long.hashCode(localId);
        result = 31 * result + Objects.hashCode(localName);
        result = 31 * result + Objects.hashCode(assetPath);
        result = 31 * result + type;
        return result;
    }

    @Override
    public String toString() {
        return "ObjectPath{" +
                "localId=" + localId +
                ", localName='" + localName + '\'' +
                ", assetPath='" + assetPath + '\'' +
                ", type=" + type +
                '}';
    }
    // endregion
}