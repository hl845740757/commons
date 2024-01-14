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

import java.util.concurrent.Future.State;

/**
 * JDK设定的任务状态{@link State}并未将【等待中】和【执行中】这两种状态分开，这其实是不好的，
 * 用户有时是需要感知着两种状态的 -- JDK不区分可能是为了兼容。
 *
 * @author wjybxx
 * date - 2024/1/9
 */
public enum FutureState {

    /** 任务尚在队列中等待 */
    PENDING(0),

    /** 任务已开始执行 */
    COMPUTING(1),

    /** 任务执行成功 - 完成状态 */
    SUCCESS(2),

    /** 任务执行失败 - 完成状态 */
    FAILED(3),

    /** 任务被取消 - 完成状态 */
    CANCELLED(4);

    private final int value;

    FutureState(int value) {
        this.value = value;
    }

    /** 是否表示完成状态 */
    public boolean isDone() {
        return value >= 2;
    }

    /** 是否表示失败或被取消 */
    public boolean isFailedOrCancelled() {
        return value >= 3;
    }

    /** 转换为jdk的状态枚举 */
    public State toJdkState() {
        return switch (this) {
            case PENDING, COMPUTING -> State.RUNNING;
            case SUCCESS -> State.SUCCESS;
            case FAILED -> State.FAILED;
            case CANCELLED -> State.CANCELLED;
        };
    }
}