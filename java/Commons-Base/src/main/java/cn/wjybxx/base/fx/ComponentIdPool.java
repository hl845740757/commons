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

package cn.wjybxx.base.fx;

import cn.wjybxx.base.ConstantMap;
import cn.wjybxx.base.ConstantPool;
import cn.wjybxx.base.ObjectUtils;

import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.BiFunction;

/**
 * 组件id池
 * 1.常量池用于定义命名空间
 * 2.用户使用的时候，应当封装一个静态类，尽量将常用的组件id都定义下来。
 *
 * @author wjybxx
 * date - 2025/3/26
 */
public final class ComponentIdPool {

    private final ConstantPool<ComponentId<?>> pool;
    private final BiFunction<ComponentIdPool, Class<?>, ComponentId.Builder<?>> cidParser;
    private final ConcurrentHashMap<Class<?>, ComponentId<?>> class2CidMap = new ConcurrentHashMap<>();

    private ComponentIdPool(ConstantPool<ComponentId<?>> pool,
                            BiFunction<ComponentIdPool, Class<?>, ComponentId.Builder<?>> cidParser) {
        this.pool = pool;
        this.cidParser = cidParser;
    }

    /** 创建一个新的池：即新的命名空间 */
    public static ComponentIdPool newPool() {
        return new ComponentIdPool(ConstantPool.newPool(null), null);
    }

    /** 创建一个新的池：即新的命名空间 */
    public static ComponentIdPool newPool(BiFunction<ComponentIdPool, Class<?>, ComponentId.Builder<?>> cidParser) {
        return new ComponentIdPool(ConstantPool.newPool(null), cidParser);
    }

    // region 委托

    /** 构建一个组件id */
    @SuppressWarnings("unchecked")
    public <T> ComponentId<T> create(ComponentId.Builder<T> builder) {
        return (ComponentId<T>) pool.newInstance(builder);
    }

    /** 创建或使用既有的组件id */
    @SuppressWarnings("unchecked")
    public <T> ComponentId<T> valueOf(ComponentId.Builder<T> builder) {
        return (ComponentId<T>) pool.valueOf(builder);
    }

    /**
     * @return 如果存在对应的文件名常量，则返回true
     */
    public boolean exists(String name) {
        return pool.exists(name);
    }

    /**
     * @return 返回常量名关联的常量，若不存在则返回null。
     */
    public ComponentId<?> get(String name) {
        return pool.get(name);
    }

    /**
     * @throws IllegalArgumentException 如果不存在对应的常量
     */
    public ComponentId<?> getOrThrow(String name) {
        return pool.getOrThrow(name);
    }

    /**
     * 创建一个常量对象快照
     */
    public ConstantMap<ComponentId<?>> newConstantMap() {
        return pool.newConstantMap();
    }

    /** 组件id数量 */
    public int size() {
        return pool.size();
    }

    /** 所有的组件id -- 快照 */
    public List<ComponentId<?>> values() {
        return pool.values();
    }

    // endregion

    /**
     * 获取类型关联的组件Id
     *
     * @param clazz 应该使用超类的class查询，否则可能导致异常
     * @return 可能是超类的组件id
     */
    @SuppressWarnings("unchecked")
    public <T> ComponentId<T> valueOf(Class<T> clazz) {
        Objects.requireNonNull(clazz, "clazz");
        // 先从缓存中查询
        ComponentId<?> cid = class2CidMap.get(clazz);
        if (cid != null) {
            return (ComponentId<T>) cid;
        }
        // 优先处理重定向
        ComponentRedirect annotationRedirect = clazz.getAnnotation(ComponentRedirect.class);
        if (annotationRedirect != null && annotationRedirect.value() != clazz) {
            cid = valueOf((Class<?>) annotationRedirect.value());
            class2CidMap.put(clazz, cid);
            return (ComponentId<T>) cid;
        }
        // 通过builder查询
        ComponentId.Builder<T> builder;
        if (cidParser != null) {
            builder = (ComponentId.Builder<T>) cidParser.apply(this, clazz);
        } else {
            ComponentDefine annotationDefine = clazz.getAnnotation(ComponentDefine.class);
            if (annotationDefine == null) {
                builder = ComponentId.newBuilder(clazz.getSimpleName());
            } else {
                String compName = annotationDefine.name();
                if (ObjectUtils.isBlank(compName)) {
                    compName = clazz.getSimpleName();
                }
                builder = ComponentId.<T>newBuilder(compName)
                        .setKind(annotationDefine.kind())
                        .setShared(annotationDefine.shared())
                        .setMaxCount(annotationDefine.maxCount())
                        .setFlags(annotationDefine.flags())
                        .setGroupKey(annotationDefine.groupKey())
                        .setMountPath(annotationDefine.mountPath());
                //
                if (annotationDefine.baseType() != Object.class) {
                    ComponentId<?> baseComponentId = valueOf(annotationDefine.baseType());
                    builder.setBaseId(baseComponentId);
                    builder.setCacheIndex(baseComponentId.index);
                }
            }
        }
        cid = pool.valueOf(builder);
        class2CidMap.put(clazz, cid);
        return (ComponentId<T>) cid;
    }
}