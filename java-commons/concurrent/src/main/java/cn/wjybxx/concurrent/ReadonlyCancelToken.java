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

import java.util.Objects;
import java.util.concurrent.CancellationException;
import java.util.concurrent.Executor;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * @author wjybxx
 * date - 2024/1/9
 */
public final class ReadonlyCancelToken implements ICancelToken {

    private final ICancelToken cancelToken;

    public ReadonlyCancelToken(ICancelToken tokenSource) {
        this.cancelToken = Objects.requireNonNull(tokenSource);
    }

    @Override
    public ICancelToken asReadonly() {
        return this;
    }

    @Override
    public void checkCancel() {
        // 避免不必要的转发
        if (cancelToken.isCancelling()) {
            throw new CancellationException();
        }
    }

    // region 转发

    @Override
    public int cancelCode() {
        return cancelToken.cancelCode();
    }

    @Override
    public boolean isCancelling() {
        return cancelToken.isCancelling();
    }

    @Override
    public int reason() {
        return cancelToken.reason();
    }

    @Override
    public int degree() {
        return cancelToken.degree();
    }

    @Override
    public boolean isInterruptible() {
        return cancelToken.isInterruptible();
    }

    @Override
    public boolean isWithoutRemove() {
        return cancelToken.isWithoutRemove();
    }

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action, int options) {
        return cancelToken.thenAccept(action, options);
    }

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action) {
        return cancelToken.thenAccept(action);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action) {
        return cancelToken.thenAcceptAsync(executor, action);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action, int options) {
        return cancelToken.thenAcceptAsync(executor, action, options);
    }

    @Override
    public IRegistration thenAccept(BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx, int options) {
        return cancelToken.thenAccept(action, ctx, options);
    }

    @Override
    public IRegistration thenAccept(BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
        return cancelToken.thenAccept(action, ctx);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
        return cancelToken.thenAcceptAsync(executor, action, ctx);
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx, int options) {
        return cancelToken.thenAcceptAsync(executor, action, ctx, options);
    }

    @Override
    public IRegistration thenRun(Runnable action, int options) {
        return cancelToken.thenRun(action, options);
    }

    @Override
    public IRegistration thenRun(Runnable action) {
        return cancelToken.thenRun(action);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action) {
        return cancelToken.thenRunAsync(executor, action);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action, int options) {
        return cancelToken.thenRunAsync(executor, action, options);
    }

    @Override
    public IRegistration thenRun(Consumer<? super IContext> action, IContext ctx, int options) {
        return cancelToken.thenRun(action, ctx, options);
    }

    @Override
    public IRegistration thenRun(Consumer<? super IContext> action, IContext ctx) {
        return cancelToken.thenRun(action, ctx);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<? super IContext> action, IContext ctx) {
        return cancelToken.thenRunAsync(executor, action, ctx);
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        return cancelToken.thenRunAsync(executor, action, ctx, options);
    }

    @Override
    public IRegistration thenNotify(CancelTokenListener action, int options) {
        return cancelToken.thenNotify(action, options);
    }

    @Override
    public IRegistration thenNotify(CancelTokenListener action) {
        return cancelToken.thenNotify(action);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action) {
        return cancelToken.thenNotifyAsync(executor, action);
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action, int options) {
        return cancelToken.thenNotifyAsync(executor, action, options);
    }

    @Override
    public IRegistration thenTransferTo(ICancelTokenSource child, int options) {
        return cancelToken.thenTransferTo(child, options);
    }

    @Override
    public IRegistration thenTransferTo(ICancelTokenSource child) {
        return cancelToken.thenTransferTo(child);
    }

    @Override
    public IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child) {
        return cancelToken.thenTransferToAsync(executor, child);
    }

    @Override
    public IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child, int options) {
        return cancelToken.thenTransferToAsync(executor, child, options);
    }

    // endregion

}
