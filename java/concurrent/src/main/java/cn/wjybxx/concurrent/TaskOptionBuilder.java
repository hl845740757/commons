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
        return TaskOption.getSchedulePhase(options);
    }

    /** @param phase 任务的调度阶段 */
    public TaskOptionBuilder setSchedulePhase(int phase) {
        this.options = TaskOption.setSchedulePhase(options, phase);
        return this;
    }

    /** 获取任务优先级 */
    public int getPriority() {
        return TaskOption.getPriority(options);
    }

    /** 设置任务的优先级 */
    public TaskOptionBuilder setPriority(int priority) {
        options = TaskOption.setPriority(options, priority);
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