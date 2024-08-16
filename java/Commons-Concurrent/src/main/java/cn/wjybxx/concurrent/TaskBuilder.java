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

    public static final int TYPE_ACTION = 0;
    public static final int TYPE_ACTION_CTX = 1;

    public static final int TYPE_FUNC = 2;
    public static final int TYPE_FUNC_CTX = 3;

    public static final int TYPE_TIMESHARING = 4;
    @Deprecated
    public static final int TYPE_TASK = 5; // java端不使用，用于C#

    private final int type;
    private final Object task;
    private Object ctx;
    private int options;

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

    // region factory

    public static TaskBuilder<Object> newAction(Runnable task) {
        return new TaskBuilder<>(TYPE_ACTION, task);
    }

    public static TaskBuilder<Object> newAction(Runnable task, ICancelToken cancelToken) {
        return new TaskBuilder<>(TYPE_ACTION, task, cancelToken);
    }

    public static TaskBuilder<Object> newAction(Consumer<IContext> task, IContext ctx) {
        return new TaskBuilder<>(TYPE_ACTION_CTX, task, ctx);
    }

    public static <V> TaskBuilder<V> newFunc(Callable<? extends V> task) {
        return new TaskBuilder<>(TYPE_FUNC, task);
    }

    public static <V> TaskBuilder<V> newFunc(Callable<? extends V> task, ICancelToken cancelToken) {
        return new TaskBuilder<>(TYPE_FUNC, task, cancelToken);
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

    /** 任务是否接收context类型参数 */
    public static boolean isTaskAcceptContext(int type) {
        switch (type) {
            case TYPE_ACTION_CTX,
                 TYPE_FUNC_CTX,
                 TYPE_TIMESHARING -> {
                return true;
            }
            default -> {
                return false;
            }
        }
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

    /** 任务是否接收context类型参数 */
    public boolean isTaskAcceptContext() {
        return isTaskAcceptContext(type);
    }

    /** 任务的上下文 */
    public IContext getCtx() {
        return isTaskAcceptContext(type) ? (IContext) ctx : null;
    }

    public TaskBuilder<V> setCtx(IContext ctx) {
        if (!isTaskAcceptContext(type)) {
            throw new IllegalStateException();
        }
        this.ctx = ctx == null ? IContext.NONE : ctx;
        return this;
    }

    /** 任务绑定的取消令牌 */
    public ICancelToken getCancelToken() {
        return isTaskAcceptContext(type) ? null : (ICancelToken) ctx;
    }

    public TaskBuilder<V> setCancelToken(ICancelToken cancelToken) {
        if (isTaskAcceptContext(type)) {
            throw new IllegalStateException();
        }
        this.ctx = cancelToken == null ? ICancelToken.NONE : cancelToken;
        return this;
    }

    // endregion

    // region options

    /** 启用选项 */
    public TaskBuilder<V> enable(int taskOption) {
        this.options = TaskOption.enable(options, taskOption);
        return this;
    }

    /** 禁用选项 */
    public TaskBuilder<V> disable(int taskOption) {
        this.options = TaskOption.disable(options, taskOption);
        return this;
    }

    /** 获取任务的阶段 */
    public int getSchedulePhase() {
        return TaskOption.getSchedulePhase(options);
    }

    /** @param phase 任务的调度阶段 */
    public TaskBuilder<V> setSchedulePhase(int phase) {
        this.options = TaskOption.setSchedulePhase(options, phase);
        return this;
    }

    /** 获取任务优先级 */
    public int getPriority() {
        return TaskOption.getPriority(options);
    }

    /** 设置任务的优先级 */
    public TaskBuilder<V> setPriority(int priority) {
        options = TaskOption.setPriority(options, priority);
        return this;
    }

    public int getOptions() {
        return options;
    }

    public TaskBuilder<V> setOptions(int options) {
        this.options = options;
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
