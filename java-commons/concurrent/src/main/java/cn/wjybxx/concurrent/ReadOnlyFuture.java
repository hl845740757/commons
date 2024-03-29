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

/**
 * 为{@link IPromise}提供只读视图
 *
 * @author wjybxx
 * date - 2024/1/9
 */
public final class ReadOnlyFuture<T> extends ForwardFuture<T> {

    /** @param promise 使用future类型可避免更具体的依赖 */
    public ReadOnlyFuture(IFuture<T> promise) {
        super(promise);
    }

    @Override
    public IFuture<T> asReadonly() {
        return this;
    }

    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        return false;
    }

}
