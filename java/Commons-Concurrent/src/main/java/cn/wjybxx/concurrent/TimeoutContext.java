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

import cn.wjybxx.base.annotation.Internal;

import javax.annotation.concurrent.NotThreadSafe;

/**
 * 超时管理上下文
 *
 * @author wjybxx
 * date 2023/4/12
 */
@Internal
@NotThreadSafe
public final class TimeoutContext {

    /** 剩余时间 */
    private long timeLeft;
    /** 上次触发时间，用于固定延迟下计算deltaTime */
    private long lastTriggerTime;

    public TimeoutContext(long timeout, long timeCreate) {
        this.timeLeft = timeout;
        this.lastTriggerTime = timeCreate;
    }

    /**
     * @param realTriggerTime  真实触发时间 -- 真正被调度的时间
     * @param logicTriggerTime 逻辑触发时间（期望的调度时间） -- 调度前计算的应该被调度的时间
     * @param isFixedRate      是否是fixedRate类型任务
     */
    public void beforeCall(long realTriggerTime, long logicTriggerTime, boolean isFixedRate) {
        if (isFixedRate) {
            timeLeft -= (logicTriggerTime - lastTriggerTime);
            lastTriggerTime = logicTriggerTime;
        } else {
            timeLeft -= (realTriggerTime - lastTriggerTime);
            lastTriggerTime = realTriggerTime;
        }
    }

    public long getTimeLeft() {
        return timeLeft;
    }

    public boolean isTimeout() {
        return timeLeft <= 0;
    }

}