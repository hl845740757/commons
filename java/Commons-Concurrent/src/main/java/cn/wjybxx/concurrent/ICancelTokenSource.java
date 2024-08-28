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

import cn.wjybxx.base.concurrent.CancelCodeBuilder;
import cn.wjybxx.base.concurrent.CancelCodes;

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
     * @return 成功使用给定取消码进入取消状态则返回true，否则返回false
     * @throws IllegalArgumentException 如果code小于等于0；或reason部分为0
     */
    boolean cancel(int cancelCode);

    /** 使用默认原因取消 */
    default boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT); // 末位1，默认情况
    }

    /**
     * 该方法主要用于兼容JDK
     *
     * @param mayInterruptIfRunning 是否可以中断目标线程；注意该参数由任务自身处理，且任务监听了取消信号才有用
     */
    default boolean cancel(boolean mayInterruptIfRunning) {
        return cancel(mayInterruptIfRunning
                ? (CancelCodes.REASON_DEFAULT & CancelCodes.MASK_INTERRUPT)
                : CancelCodes.REASON_DEFAULT);
    }

    /**
     * 在一段时间后发送取消命令
     *
     * @param cancelCode        取消码
     * @param millisecondsDelay 延迟时间(毫秒) -- 单线程版的话，真实单位取决于约定。
     */
    void cancelAfter(int cancelCode, long millisecondsDelay);

    /**
     * 在一段时间后发送取消命令
     *
     * @param cancelCode 取消码
     * @param delay      延迟时间
     * @param timeUnit   时间单位
     */
    void cancelAfter(int cancelCode, long delay, TimeUnit timeUnit);

    /**
     * 创建一个同类型实例。
     * 1.原型对象，避免具体类型依赖。
     * 2.默认情况下，其它上下文应当拷贝。
     *
     * @param copyCode 是否拷贝当前取消码
     * @return 取消令牌
     */
    ICancelTokenSource newInstance(boolean copyCode);

    default ICancelTokenSource newInstance() {
        return newInstance(false);
    }
}