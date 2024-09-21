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

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Predicate;

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
     * @param nameOrGuid 行为树的名字或guid
     * @return 编辑器导出的对象
     */
    @Nullable
    Object tryLoadObject(String nameOrGuid);

    default Object loadObject(String nameOrGuid) {
        Object result = tryLoadObject(nameOrGuid);
        if (result == null) {
            throw new IllegalArgumentException("target object is absent, name: " + nameOrGuid);
        }
        return result;
    }

    /**
     * 批量加载指定文件中的对象
     *
     * @param fileName 文件名，通常不建议带扩展后缀
     * @param filter   过滤器，为null则加载给定文件全部的入口对象；不要修改Entry对象的数据。
     */
    default List<Object> loadManyFromFile(String fileName, @Nullable Predicate<? super IEntry> filter) {
        return loadManyFromFile(fileName, filter, false);
    }

    /**
     * 批量加载指定文件中的对象
     *
     * @param fileName 文件名，通常不建议带扩展后缀
     * @param filter   过滤器，为null则加载给定文件全部的入口对象；不要修改Entry对象的数据。
     * @param sharable 是否共享；如果为true，则返回前不进行拷贝
     */
    List<Object> loadManyFromFile(String fileName, @Nullable Predicate<? super IEntry> filter, boolean sharable);

    /**
     * 尝试加载行为树的根节点
     *
     * @param treeName 行为树的名字或guid
     * @return rootTask
     */
    @Nullable
    @SuppressWarnings({"unchecked", "rawtypes"})
    default <T> Task<T> tryLoadRootTask(String treeName) {
        Object result = tryLoadObject(treeName);
        if (result == null) {
            return null;
        }
        if (!(result instanceof Task task)) {
            throw new IllegalArgumentException("target object is not a task, name: " + treeName);
        }
        return task;
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    default <T> Task<T> loadRootTask(String treeName) {
        Object result = tryLoadObject(treeName);
        if (result == null) {
            throw new IllegalArgumentException("target tree is absent, name: " + treeName);
        }
        if (!(result instanceof Task task)) {
            throw new IllegalArgumentException("target object is not a task, name: " + treeName);
        }
        return task;
    }

    default <T> TaskEntry<T> loadTree(String treeName) {
        final Task<T> rootTask = loadRootTask(treeName);
        return new TaskEntry<>(treeName, rootTask, null, null, this);
    }

    // endregion

    // region

    /**
     * 编辑器中的Entry节点抽象。
     * 接口层不处理数据和行为分离架构下的配置需求，用户在具体的Entry上处理即可。
     */
    interface IEntry {

        /** 入口对象的名字(别名) -- 可能为null */
        @Nullable
        String getName();

        /** 入口对象的guid -- 一定存在 */
        String getGuid();

        /** 入口对象的标记信息 */
        int getFlags();

        /** 入口对象的类型，通常用于表示其作用 */
        int getType();

        /** 入口对象绑定的Root对象 */
        Object getRoot();

    }

    // endregion

    // region NullLoader

    static TreeLoader nullLoader() {
        return NullLoader.INSTANCE;
    }

    class NullLoader implements TreeLoader {

        static final NullLoader INSTANCE = new NullLoader();

        @Override
        public Object tryLoadObject(String nameOrGuid) {
            return null;
        }

        @Override
        public List<Object> loadManyFromFile(String fileName, @Nullable Predicate<? super IEntry> filter, boolean sharable) {
            return new ArrayList<>();
        }
    }
}