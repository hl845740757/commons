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

import java.util.concurrent.TimeUnit;

/**
 * 取消令牌源由任务的创建者（发起者）持有，具备取消权限。
 * <p>
 * ps：{@link ICancelTokenSource}和{@link ICancelToken}之间的关系，
 * 其实就是{@link IPromise}和{@link IFuture}之间的关系，
 * 取消信号的传递是本就可以通过{@link IFuture}实现的，只是语义上不那么清楚。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public interface ICancelTokenSource extends ICancelToken {

    /**
     * 将Token置为取消状态
     *
     * @param cancelCode 取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}
     * @return 如果Token已被取消，则返回旧值（非0）；如果Token尚未被取消，则将Token更新为取消状态，并返回0。
     * @throws IllegalArgumentException      如果code小于等于0；或reason部分为0
     * @throws UnsupportedOperationException 如果context是只读的
     */
    int cancel(int cancelCode);

    /** 使用默认原因取消 */
    default int cancel() {
        return cancel(ICancelToken.REASON_DEFAULT); // 末位1，默认情况
    }

    /**
     * 该方法主要用于兼容JDK
     *
     * @param mayInterruptIfRunning 是否可以中断目标线程；注意该参数由任务自身处理，且任务监听了取消信号才有用
     */
    default int cancel(boolean mayInterruptIfRunning) {
        return cancel(mayInterruptIfRunning
                ? (ICancelToken.REASON_DEFAULT & ICancelToken.MASK_INTERRUPT)
                : ICancelToken.REASON_DEFAULT);
    }

    /**
     * 在一段时间后发送取消命令
     *
     * @param cancelCode 取消码
     * @param millisecondsDelay 延迟时间(毫秒) -- 单线程版的话，真实单位取决于约定。
     */
    void cancelAfter(int cancelCode, long millisecondsDelay);

    /**
     * 在一段时间后发送取消命令
     *
     * @param cancelCode 取消码
     * @param delay 延迟时间
     * @param timeUnit 时间单位
     */
    void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit);

}