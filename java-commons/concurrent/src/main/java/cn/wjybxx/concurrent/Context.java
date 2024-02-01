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
package cn.wjybxx.concurrent;

import cn.wjybxx.base.ObjectUtils;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 默认上下文实现
 * <h3>黑板类型</h3>
 * 默认实现假设了父上下文和当前上下文的黑板类型一致，这个假设并不总是成立，但一般情况下是如此；
 * 如果该实现不满足需求，用户可实现自己的上下文类型。
 *
 * @author wjybxx
 * date - 2023/11/18
 */
public class Context<T> implements IContext {

    private final Context<T> parent;
    private final ICancelToken cancelToken;
    private final T blackboard;
    private final Object sharedProps;

    public Context(T blackboard) {
        this(null, blackboard, null, null);
    }

    public Context(T blackboard, Object sharedProps) {
        this(null, blackboard, sharedProps, null);
    }

    public Context(T blackboard, Object sharedProps, ICancelToken cancelToken) {
        this(null, blackboard, sharedProps, cancelToken);
    }

    /**
     * 一般不建议直接调用该方法，而是通过{@link #withBlackboard(Object)}等创建子上下文，否则无法处理上下文继承问题。
     *
     * @param parent      父节点
     * @param blackboard  黑板
     * @param sharedProps 共享属性
     * @param cancelToken 取消令牌
     */
    public Context(Context<T> parent, T blackboard, Object sharedProps, ICancelToken cancelToken) {
        this.parent = parent;
        this.cancelToken = ObjectUtils.nullToDef(cancelToken, ICancelToken.NONE);
        this.blackboard = blackboard;
        this.sharedProps = sharedProps;
    }

    // region props

    /**
     * 根上下文
     * 1.根上下文中可能保存着一些有用的行为。
     * 2.记录更上下文可以避免深度的递归查找。
     * 3.没有父节点的Context的根为自己。
     * <p>
     * 默认的实现不缓存(root)上下文，如果用户需要频繁访问root上下文，可实现自己的上下文类型，
     * 将root缓存在每一级的context上，以减少查找开销。
     */
    public Context<T> root() {
        Context<T> root = this;
        while (root.parent != null) {
            root = root.parent;
        }
        return root;
    }

    /**
     * 父上下文
     * 由于我们没有提供默认的黑板实现，因此需要支持用户迭代上下文。
     */
    public Context<T> parent() {
        return parent;
    }

    @Nonnull
    @Override
    public ICancelToken cancelToken() {
        return cancelToken;
    }

    @Override
    public T blackboard() {
        return blackboard;
    }

    public Object sharedProps() {
        return sharedProps;
    }

    // endregion

    // region factory

    public static <U> Context<U> ofBlackboard(U blackboard) {
        return new Context<>(null, blackboard, null, null);
    }

    public static <U> Context<U> ofBlackboard(U blackboard, Object sharedProps) {
        return new Context<>(null, blackboard, sharedProps, null);
    }

    public static <U> Context<U> ofBlackboard(U blackboard, Object sharedProps, ICancelToken cancelToken) {
        return new Context<>(null, blackboard, sharedProps, cancelToken);
    }

    /** 创建一个仅包含取消令牌的上下文 */
    public static <U> Context<U> ofCancelToken(ICancelToken cancelToken) {
        Objects.requireNonNull(cancelToken);
        return new Context<>(null, null, null, cancelToken);
    }

    /** 用于子类重写 */
    protected Context<T> newContext(Context<T> parent, T blackboard, Object sharedProps, ICancelToken cancelToken) {
        return new Context<>(parent, blackboard, sharedProps, cancelToken);
    }

    // endregion

    // region 创建子上下文

    public Context<T> childWithBlackboard(T blackboard) {
        return newContext(this, blackboard, sharedProps, cancelToken);
    }

    public Context<T> childWithBlackboard(T blackboard, Object sharedProps) {

        return newContext(this, blackboard, sharedProps, cancelToken);
    }

    public Context<T> childWithBlackboard(T blackboard, Object sharedProps, ICancelToken cancelToken) {
        return newContext(this, blackboard, sharedProps, cancelToken);
    }

    public Context<T> childWithCancelToken(ICancelToken cancelToken) {
        return newContext(this, blackboard, sharedProps, cancelToken);
    }

    // endregion

    // region with

    public Context<T> withBlackboard(T blackboard) {
        if (blackboard == this.blackboard) {  // 较大概率
            return this;
        }
        return newContext(parent, blackboard, sharedProps, cancelToken);
    }

    public Context<T> withBlackboard(T blackboard, Object sharedProps) {

        return newContext(parent, blackboard, sharedProps, cancelToken);
    }

    public Context<T> withBlackboard(T blackboard, Object sharedProps, ICancelToken cancelToken) {
        return newContext(parent, blackboard, sharedProps, cancelToken);
    }

    /** 使用给定取消令牌 */
    public Context<T> withCancelToken(ICancelToken cancelToken) {
        if (cancelToken == this.cancelToken) { // 较大概率
            return this;
        }
        return newContext(parent, blackboard, sharedProps, cancelToken);
    }

    /** 去掉取消令牌 */
    @Override
    public Context<T> withoutCancelToken() {
        if (this.cancelToken == ICancelToken.NONE) { // 较大概率
            return this;
        }
        return newContext(this, blackboard, sharedProps, cancelToken);
    }

    // endregion
}