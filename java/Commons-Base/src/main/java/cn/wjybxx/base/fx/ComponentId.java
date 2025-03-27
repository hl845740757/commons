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

import cn.wjybxx.base.AbstractConstant;
import cn.wjybxx.base.Constant;

import java.util.Objects;

/**
 * 组件id
 * <p>
 * 1.可通过{@link ComponentDefine}定义组件的信息
 * 2.是否和Entity同生命周期（禁止提前删除），是属于组件实例的属性，并非这一类组件的共同属性。
 *
 * @param <T> 组件的类型，主要用于编码提示
 * @author wjybxx
 * date - 2024/6/22
 */
public class ComponentId<T extends IComponent> extends AbstractConstant {

    /** 高速缓存下标 */
    public final int index;
    /** 组件类型 */
    public final ComponentKind kind;
    /** 是否是共享组件 -- 通常共享组件的所有方法都不被框架调用；甚至不会被注入实体的引用 */
    public final boolean shared;
    /** 最大可挂载数量 */
    public final int maxCount;
    /** 启用的函数，扫描重写的方法计算得到 -- 共享组件的该值将被修正为0 */
    public final long enableFuncs;
    /** 关联的类型 -- 不定义为泛型，避免奇怪的问题 */
    public final Class<?> clazz;

    /** 业务自定义flags */
    public final long flags;
    /** 用户扩展数据 -- 必须的不可变的 */
    public final Object extraInfo;

    /** 子类慎重重写 */
    protected ComponentId(Builder<T> builder) {
        super(builder);
        assert builder.getCacheIndex() >= 0;
        this.index = builder.getCacheIndex();
        this.kind = Objects.requireNonNull(builder.kind, "kind");
        this.shared = builder.shared;
        this.maxCount = Math.max(1, builder.maxCount);
        this.enableFuncs = builder.enableFuncs;
        this.clazz = Objects.requireNonNull(builder.clazz, "clazz");

        this.flags = builder.flags;
        this.extraInfo = builder.extraInfo;
    }

    /** 是否是私有脚本 --- 需要被框架调度 */
    public final boolean isPrivateScript() {
        return !shared && kind == ComponentKind.SCRIPT;
    }

    public static <T extends IComponent> Builder<T> newBuilder(String name, Class<?> clazz) {
        return new Builder<>(name, clazz);
    }

    public static class Builder<T extends IComponent> extends Constant.Builder<ComponentId<T>> {
        /** 关联的类型 */
        private final Class<?> clazz;
        /** 组件类型，默认为需要框架调度的脚本 */
        private ComponentKind kind = ComponentKind.SCRIPT;
        /** 是否可共享 */
        private boolean shared = false;
        /** 最大可挂载数量 */
        private int maxCount = 1;
        /** 启用的函数，扫描重写的方法计算得到 -- 共享组件该值修正为0 */
        private long enableFuncs;

        /** 业务自定义flags */
        private long flags;
        /** 用户扩展数据 -- 必须的不可变的 */
        private Object extraInfo;

        protected Builder(String name, Class<?> clazz) {
            super(name);
            this.clazz = Objects.requireNonNull(clazz, "clazz");
            setRequireCacheIndex(true); // 需要分配缓存索引
        }

        @Override
        protected ComponentId<T> build() {
            return new ComponentId<>(this);
        }

        public Class<?> getClazz() {
            return clazz;
        }

        public ComponentKind getKind() {
            return kind;
        }

        public Builder<T> setKind(ComponentKind kind) {
            this.kind = kind;
            return this;
        }

        public boolean isShared() {
            return shared;
        }

        public Builder<T> setShared(boolean shared) {
            this.shared = shared;
            return this;
        }

        public int getMaxCount() {
            return maxCount;
        }

        public Builder<T> setMaxCount(int maxCount) {
            this.maxCount = maxCount;
            return this;
        }

        public long getEnableFuncs() {
            return enableFuncs;
        }

        public Builder<T> setEnableFuncs(long enableFuncs) {
            this.enableFuncs = enableFuncs;
            return this;
        }

        public long getFlags() {
            return flags;
        }

        public Builder<T> setFlags(long flags) {
            this.flags = flags;
            return this;
        }

        public Object getExtraInfo() {
            return extraInfo;
        }

        public Builder<T> setExtraInfo(Object extraInfo) {
            this.extraInfo = extraInfo;
            return this;
        }
    }
}