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
    public IRegistration register(Consumer<? super ICancelToken> action) {
        return cancelToken.register(action);
    }

    @Override
    public IRegistration registerTyped(CancelTokenListener action) {
        return cancelToken.registerTyped(action);
    }

    @Override
    public IRegistration registerRun(Runnable action) {
        return cancelToken.registerRun(action);
    }

    @Override
    public IRegistration registerChild(ICancelTokenSource child) {
        return cancelToken.registerChild(child);
    }
}
