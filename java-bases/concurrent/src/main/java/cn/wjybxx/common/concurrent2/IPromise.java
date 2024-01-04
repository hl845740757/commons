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
package cn.wjybxx.common.concurrent2;

import java.util.concurrent.CancellationException;

/**
 * @author wjybxx
 * date - 2023/11/6
 */
public interface IPromise<T> extends IFuture<T> {

    /** 默认情况下是否记录日志 */
    boolean defLogCause = Boolean.parseBoolean(System.getProperty("cn.wjybxx.common.concurrent.IPromise.defLogCause", "false"));

    /**
     * 尝试将future置为正在计算状态
     * 只有成功将future从pending状态更新为computing状态时返回true
     */
    default boolean trySetComputing() {
        return trySetComputing(false);
    }

    /**
     * 尝试将future置为正在计算状态
     * 如果future之前不处于pending状态，则返回true。
     * 如果future之前已处于computing状态，则返回给定值
     */
    boolean trySetComputing(boolean resultIfComputing);

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
     * @param cause    如果为{@link CancellationException}，则等同于取消
     * @param logCause 是否记录日志
     *                 注意：即便为true，如果异常是{@link NoLogRequiredException}，那么也不记录日志
     */
    boolean trySetException(Throwable cause, boolean logCause);

    /**
     * 将future置为失败状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     *
     * @param cause    如果为{@link CancellationException}，则等同于取消
     * @param logCause 是否记录日志
     *                 注意：即便为true，如果异常是{@link NoLogRequiredException}，那么也不记录日志
     */
    void setException(Throwable cause, boolean logCause);

    /**
     * 将Future置为已取消状态，如果future已进入完成状态，则返回false
     */
    boolean trySetCancelled();

    /**
     * 将Future置为已取消状态，如果future已进入完成状态，则抛出{@link IllegalStateException}
     */
    void setCancelled();

    default boolean trySetException(Throwable cause) {
        return trySetException(cause, defLogCause);
    }

    default void setException(Throwable cause) {
        setException(cause, defLogCause);
    }

    // region 重写签名

    @Override
    IPromise<T> await() throws InterruptedException;

    @Override
    IPromise<T> awaitUninterruptedly();
    // endregion
}