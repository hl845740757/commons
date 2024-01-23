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
 * @author wjybxx
 * date - 2024/1/14
 */
public class TaskOptionBuilder {

    private int options = 0;

    /** 启用选项 */
    public TaskOptionBuilder enable(int taskOption) {
        this.options = TaskOption.enable(options, taskOption);
        return this;
    }

    /** 禁用选项 */
    public TaskOptionBuilder disable(int taskOption) {
        this.options = TaskOption.disable(options, taskOption);
        return this;
    }

    /** 获取任务的阶段 */
    public int getSchedulePhase() {
        return options & TaskOption.MASK_SCHEDULE_PHASE;
    }

    /** @param phase 任务的调度阶段 */
    public TaskOptionBuilder setSchedulePhase(int phase) {
        if (phase < 0 || phase > TaskOption.MASK_SCHEDULE_PHASE) {
            throw new IllegalArgumentException("phase: " + phase);
        }
        this.options &= ~TaskOption.MASK_SCHEDULE_PHASE;
        this.options |= phase;
        return this;
    }

    public int getOptions() {
        return options;
    }

    public TaskOptionBuilder setOptions(int options) {
        this.options = options;
        return this;
    }

}