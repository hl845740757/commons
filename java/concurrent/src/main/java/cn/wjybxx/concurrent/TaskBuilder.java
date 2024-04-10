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
public sealed class TaskBuilder<V> extends TaskOptionBuilder permits ScheduledTaskBuilder {

    public static final int TYPE_ACTION = 0;
    public static final int TYPE_ACTION_CTX = 1;

    public static final int TYPE_FUNC = 2;
    public static final int TYPE_FUNC_CTX = 3;

    public static final int TYPE_TIMESHARING = 4;
    @Deprecated
    public static final int TYPE_TASK = 5; // java端不使用，用于C#

    private final int type;
    private final Object task;
    private IContext ctx;

    protected TaskBuilder(int type, Object task) {
        this.task = Objects.requireNonNull(task);
        this.type = type;
        this.ctx = IContext.NONE;
    }

    protected TaskBuilder(int type, Object task, IContext ctx) {
        this.task = Objects.requireNonNull(task);
        this.type = type;
        this.ctx = ctx == null ? IContext.NONE : ctx;
    }

    protected TaskBuilder(TaskBuilder<? extends V> taskBuilder) {
        super.setOptions(taskBuilder.getOptions());
        this.task = taskBuilder.task;
        this.type = taskBuilder.type;
        this.ctx = taskBuilder.ctx;
    }

    // region factory

    public static TaskBuilder<?> newAction(Runnable task) {
        return new TaskBuilder<>(TYPE_ACTION, task);
    }

    public static <V> TaskBuilder<V> newAction(Consumer<IContext> task, IContext ctx) {
        return new TaskBuilder<>(TYPE_ACTION_CTX, task, ctx);
    }

    public static <V> TaskBuilder<V> newFunc(Callable<? extends V> task) {
        return new TaskBuilder<>(TYPE_FUNC, task);
    }

    public static <V> TaskBuilder<V> newFunc(Function<IContext, ? extends V> task, IContext ctx) {
        return new TaskBuilder<>(TYPE_FUNC_CTX, task, ctx);
    }

    public static <V> TaskBuilder<V> newTimeSharing(TimeSharingTask<? super V> task) {
        return new TaskBuilder<>(TYPE_TIMESHARING, task, IContext.NONE);
    }

    public static <V> TaskBuilder<V> newTimeSharing(TimeSharingTask<? super V> task, IContext ctx) {
        return new TaskBuilder<>(TYPE_TIMESHARING, task, ctx);
    }

    /** 计算任务的类型 */
    public static int taskType(Object task) {
        Objects.requireNonNull(task);
        if (task instanceof Runnable) {
            return TYPE_ACTION;
        }
        if (task instanceof Consumer<?>) {
            return TYPE_ACTION_CTX;
        }
        if (task instanceof Callable<?>) {
            return TYPE_FUNC;
        }
        if (task instanceof Function<?, ?>) {
            return TYPE_FUNC_CTX;
        }
        if (task instanceof TimeSharingTask<?>) {
            return TYPE_TIMESHARING;
        }
        throw new IllegalArgumentException("unsupported task type: " + task.getClass());
    }
    // endregion

    // region props

    /** 任务的类型 */
    public int getType() {
        return type;
    }

    public Object getTask() {
        return task;
    }

    /** 任务的上下文 */
    public IContext getCtx() {
        return ctx;
    }

    /**
     * 任务的上下文
     * 即使用户的任务不接收ctx，executor也可能需要
     */
    public TaskBuilder<V> setCtx(IContext ctx) {
        this.ctx = ctx == null ? IContext.NONE : ctx;
        return this;
    }

    // endregion

    // region options

    public TaskBuilder<V> enable(int taskOption) {
        super.enable(taskOption);
        return this;
    }

    public TaskBuilder<V> disable(int taskOption) {
        super.disable(taskOption);
        return this;
    }

    public TaskBuilder<V> setSchedulePhase(int phase) {
        super.setSchedulePhase(phase);
        return this;
    }

    public TaskBuilder<V> setPriority(int priority) {
        super.setPriority(priority);
        return this;
    }

    public TaskBuilder<V> setOptions(int options) {
        super.setOptions(options);
        return this;
    }

    // endregion

    public ScheduledTaskBuilder<V> toScheduledBuilder() {
        if (this instanceof ScheduledTaskBuilder<V> sb) {
            return sb;
        }
        return new ScheduledTaskBuilder<>(this);
    }
}
