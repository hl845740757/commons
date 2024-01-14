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

package cn.wjybxx.common.concurrent;

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

    @Override
    public IRegistration register(Consumer<? super ICancelToken> action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration register(BiConsumer<? super ICancelToken, ? super IContext> action, IContext ctx) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration registerRun(Runnable action) {
        return TOMBSTONE;
    }

    @Override
    public IRegistration registerChild(ICancelTokenSource child) {
        return TOMBSTONE;
    }

    private static final IRegistration TOMBSTONE = () -> {};

}
