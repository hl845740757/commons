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

import java.util.concurrent.Executor;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * @author wjybxx
 * date - 2024/1/9
 */
final class UncancellableToken implements ICancelToken {

    static final UncancellableToken INST = new UncancellableToken();

    private UncancellableToken() {
    }

    @Override
    public ICancelToken asReadonly() {
        return this;
    }

    // region code

    @Override
    public int cancelCode() {
        return 0;
    }

    @Override
    public boolean isCancelling() {
        return false;
    }

    @Override
    public int reason() {
        return 0;
    }

    @Override
    public int degree() {
        return 0;
    }

    @Override
    public boolean isInterruptible() {
        return false;
    }

    @Override
    public boolean isWithoutRemove() {
        return false;
    }

    @Override
    public void checkCancel() {

    }

    // endregion

    // region 监听器

    private static final IRegistration TOMBSTONE = () -> {};

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAccept(Consumer<? super ICancelToken> action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, Consumer<? super ICancelToken> action, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAccept(BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAccept(BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super ICancelToken> action, IContext ctx, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRun(Runnable action, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRun(Runnable action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Runnable action, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRun(Consumer<? super IContext> action, IContext ctx, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRun(Consumer<? super IContext> action, IContext ctx) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<? super IContext> action, IContext ctx) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenRunAsync(Executor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenNotify(CancelTokenListener action, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenNotify(CancelTokenListener action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenNotifyAsync(Executor executor, CancelTokenListener action, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenTransferTo(ICancelTokenSource child, int options) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenTransferTo(ICancelTokenSource child) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration thenTransferToAsync(Executor executor, ICancelTokenSource child, int options) {
        return TOMBSTONE;
    }
    // endregion
}
