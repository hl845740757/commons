/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree;

import cn.wjybxx.base.ObjectPath;

import javax.annotation.Nullable;

/**
 * 行为树加载器
 * 1.虽命名为TreeLoader，但可加载任意导出对象，只因该Loader最初是为行为树设计的。
 * 2.Loader只能加载编辑器中的Entry（入口）对象，由于编辑器会为Root自动创建Entry数据，因此等价于Loader只能加载Root对象。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public interface TreeLoader {

    // region load

    /**
     * 1.加载时，通常应按照名字加载，再尝试按照guid加载。
     * 2.如果对象是一棵树，行为树的结构必须是稳定的。
     *
     * @param path 行为树的名字或guid
     * @return 编辑器导出的对象
     */
    @Nullable
    Object tryLoadObject(ObjectPath path);

    default Object loadObject(ObjectPath path) {
        Object result = tryLoadObject(path);
        if (result == null) {
            throw new IllegalArgumentException("target object is absent, path: " + path);
        }
        return result;
    }

    /**
     * 尝试加载行为树的根节点
     *
     * @param path 行为树的名字或guid
     * @return rootTask
     */
    @Nullable
    @SuppressWarnings({"unchecked", "rawtypes"})
    default <T> Task<T> tryLoadRootTask(ObjectPath path) {
        Object result = tryLoadObject(path);
        if (result == null) {
            return null;
        }
        if (!(result instanceof Task task)) {
            throw new IllegalArgumentException("target object is not a task, path: " + path);
        }
        return task;
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    default <T> Task<T> loadRootTask(ObjectPath path) {
        Object result = tryLoadObject(path);
        if (result == null) {
            throw new IllegalArgumentException("target tree is absent, path: " + path);
        }
        if (!(result instanceof Task task)) {
            throw new IllegalArgumentException("target object is not a task, path: " + path);
        }
        return task;
    }

    /** path可能不包含name信息，因此推荐重写该方法，正确赋值行为树的name */
    default <T> TaskEntry<T> loadTree(ObjectPath path) {
        final Task<T> rootTask = loadRootTask(path);
        return new TaskEntry<>(path.localPath, rootTask, null, null, this);
    }

    // endregion

    // region NullLoader

    static TreeLoader nullLoader() {
        return NullLoader.INSTANCE;
    }

    class NullLoader implements TreeLoader {

        static final NullLoader INSTANCE = new NullLoader();

        @Override
        public Object tryLoadObject(ObjectPath path) {
            return null;
        }
    }
}