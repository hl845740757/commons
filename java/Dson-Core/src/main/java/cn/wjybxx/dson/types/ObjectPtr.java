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

import cn.wjybxx.base.ObjectPath;
import cn.wjybxx.base.ObjectUtils;

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

    public static final int MASK_COLLECTION = 1;
    public static final int MASK_LOCAL_PATH = 1 << 1;
    public static final int MASK_TYPE = 1 << 2;

    /**
     * 目标对象所属的集合(文件路径、资产路径、db路径)
     * (如果为空，表示引用当前资产内的对象)
     */
    private final String collection;
    /**
     * 对象在集合内的路径(或name)
     * (如果字段不为空，则优先使用localPath查找对象，即localPath的优先级高于localId)
     */
    private final String localPath;
    /**
     * 对象在集合内的id
     * (如果目标集合是数组，则可能是下标)
     */
    private final long localId;
    /**
     * 引用类型
     * (用于引用分析，也可以表示如何解析引用等)
     */
    private final int type;

    public ObjectPtr(long localId) {
        this(null, null, localId, 0);
    }

    public ObjectPtr(String collection, String localPath, long localId, int type) {
        // 空字符串转null以兼容default构建的实例
        this.collection = ObjectUtils.emptyToDef(collection, null);
        this.localPath = ObjectUtils.emptyToDef(localPath, null);
        this.localId = localId;
        this.type = type;
        if (type != 0 && isEmpty()) {
            throw new IllegalArgumentException();
        }
    }

    public boolean isEmpty() {
        return localId == 0
                && ObjectUtils.isEmpty(localPath)
                && ObjectUtils.isEmpty(collection);
    }

    public boolean canBeAbbreviated() {
        return type == 0
                && ObjectUtils.isEmpty(localPath)
                && ObjectUtils.isEmpty(collection);
    }

    public boolean hasCollection() {
        return !ObjectUtils.isEmpty(collection);
    }

    public boolean hasLocalPath() {
        return !ObjectUtils.isEmpty(localPath);
    }

    public boolean hasLocalId() {
        return localId != 0;
    }

    public String getCollection() {
        return collection;
    }

    public String getLocalPath() {
        return localPath;
    }

    public long getLocalId() {
        return localId;
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
        return "ObjectPtr{" +
                "localId=" + localId +
                ", localPath='" + localPath + '\'' +
                ", collection='" + collection + '\'' +
                ", type=" + type +
                '}';
    }

    // endregion

    // 属性名
    public static final String NAMES_COLLECTION = "coll";
    public static final String NAMES_LOCAL_PATH = "localPath";
    public static final String NAMES_LOCAL_ID = "localId";
    public static final String NAMES_TYPE = "type";

    // 转换
    public static ObjectPtr OfObjectPath(ObjectPath path) {
        return new ObjectPtr(path.collection, path.localPath, path.localId, path.type);
    }

    public ObjectPath toObjectPath() {
        return new ObjectPath(collection, localPath, localId, type);
    }
}