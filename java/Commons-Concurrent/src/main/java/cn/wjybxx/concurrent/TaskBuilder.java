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

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 任务构建器
 *
 * @author wjybxx
 * date - 2024/1/11
 */
@NotThreadSafe
public sealed class TaskBuilder<V> permits ScheduledTaskBuilder {

    public static final int TYPE_EMPTY = 0;
    public static final int TYPE_ACTION = 1;
    public static final int TYPE_ACTION_STATE = 2;
    public static final int TYPE_FUNC = 3;
    public static final int TYPE_FUNC_STATE = 4;
    @Deprecated
    private static final int TYPE_TASK = 5; // java端不使用，用于C#

    private final int type;
    private final Object task;
    private Object ctx;
    protected int options;

    protected TaskBuilder(int type, Object task) {
        this.task = Objects.requireNonNull(task);
        this.type = type;
    }

    protected TaskBuilder(int type, Object task, Object ctx) {
        this.task = Objects.requireNonNull(task);
        this.type = type;
        this.ctx = ctx;
    }

    protected TaskBuilder(TaskBuilder<? extends V> taskBuilder) {
        this.task = taskBuilder.task;
        this.type = taskBuilder.type;
        this.ctx = taskBuilder.ctx;
        this.options = taskBuilder.options;
    }

    // region props

    /** 任务的类型 */
    public int getType() {
        return type;
    }

    /** 绑定的任务 */
    public Object getTask() {
        return task;
    }

    /** 任务的上下文 */
    public Object getCtx() {
        return ctx;
    }

    /** 设置上下文 */
    public TaskBuilder<V> setCtx(Object ctx) {
        this.ctx = ctx;
        return this;
    }
    // endregion

    // region options

    /** 是否启用了某选项 */
    public boolean isEnabled(int taskOption) {
        return TaskOptions.isEnabled(options, taskOption);
    }

    /** 启用选项 */
    public TaskBuilder<V> enable(int taskOption) {
        this.options = TaskOptions.enable(options, taskOption);
        return this;
    }

    /** 禁用选项 */
    public TaskBuilder<V> disable(int taskOption) {
        this.options = TaskOptions.disable(options, taskOption);
        return this;
    }

    /** 最终options */
    public int getOptions() {
        return options;
    }

    public TaskBuilder<V> setOptions(int options) {
        this.options = options;
        return this;
    }

    // endregion

    // region factory

    public static TaskBuilder<Object> newAction(Runnable task) {
        return new TaskBuilder<>(TYPE_ACTION, task);
    }

    public static TaskBuilder<Object> newAction(Runnable task, ICancelToken cancelToken) {
        return new TaskBuilder<>(TYPE_ACTION, task, cancelToken);
    }

    public static TaskBuilder<Object> newAction(Consumer<Object> task, Object ctx) {
        return new TaskBuilder<>(TYPE_ACTION_STATE, task, ctx);
    }

    public static <V> TaskBuilder<V> newFunc(Callable<? extends V> task) {
        return new TaskBuilder<>(TYPE_FUNC, task);
    }

    public static <V> TaskBuilder<V> newFunc(Callable<? extends V> task, ICancelToken cancelToken) {
        return new TaskBuilder<>(TYPE_FUNC, task, cancelToken);
    }

    public static <V> TaskBuilder<V> newFunc(Function<Object, ? extends V> task, Object ctx) {
        return new TaskBuilder<>(TYPE_FUNC_STATE, task, ctx);
    }

    public ScheduledTaskBuilder<V> toScheduledBuilder() {
        if (this instanceof ScheduledTaskBuilder<V> sb) {
            return sb;
        }
        return new ScheduledTaskBuilder<>(this);
    }
    // endregion
}
