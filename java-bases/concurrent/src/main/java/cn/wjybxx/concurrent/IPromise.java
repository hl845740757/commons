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

import javax.annotation.concurrent.ThreadSafe;
import java.util.concurrent.CancellationException;

/**
 * promise由任务的的创建者和执行者共同持有，它们都有权将任务置为完成状态。
 * （唯独任务自身不具备这个权限）
 *
 * @author wjybxx
 * date - 2023/11/6
 */
@ThreadSafe
public interface IPromise<T> extends IFuture<T> {

    /**
     * 尝试将future置为正在计算状态
     * 只有成功将future从pending状态更新为computing状态时返回true
     */
    default boolean trySetComputing() {
        return trySetComputing2() == FutureState.PENDING;
    }

    /**
     * 尝试将future置为正在计算状态
     * 该接口有更好的返回值，不过一般情况下还是推荐{@link #trySetComputing()}
     *
     * @return 之前的状态
     */
    FutureState trySetComputing2();

    /**
     * 将future置为计算中状态，如果future之前不处于pending状态，则抛出{@link IllegalStateException}
     */
    void setComputing();

    /**
     * 尝试将future置为成功完成状态，如果future已进入完成状态，则返回false
     */
    boolean trySetResult(T result);

    /**
     * 将future置为成功完成状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     */
    void setResult(T result);

    /**
     * 尝试将future置为失败完成状态，如果future已进入完成状态，则返回false
     *
     * @param cause 如果为{@link CancellationException}，则等同于取消
     */
    boolean trySetException(Throwable cause);

    /**
     * 将future置为失败状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     *
     * @param cause 如果为{@link CancellationException}，则等同于取消
     */
    void setException(Throwable cause);


    /**
     * 将Future置为已取消状态，如果future已进入完成状态，则返回false
     *
     * @param code 相关的取消码
     */
    boolean trySetCancelled(int code);

    /**
     * 将Future置为已取消状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     *
     * @param code 相关的取消码
     */
    void setCancelled(int code);

    /**
     * 将Future置为已取消状态，如果future已进入完成状态，则返回false
     */
    boolean trySetCancelled();

    /**
     * 将Future置为已取消状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     */
    void setCancelled();

    /**
     * 将目标future的结果传输到当前Promise
     * 如果目标future已完成，且当前promise尚未完成，则尝试传输结果到promise
     * <p>
     * {@link IFuture#tryTransferTo(IPromise)}
     *
     * @return 当且仅当由目标future使当前promise进入完成状态时返回true。
     */
    boolean tryTransferFrom(IFuture<? extends T> input);

}