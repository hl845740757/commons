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

import cn.wjybxx.base.concurrent.ExecutorCoreUtils;
import cn.wjybxx.base.concurrent.ICancelToken;
import cn.wjybxx.base.concurrent.TaskOptions;
import cn.wjybxx.base.pool.ConcurrentObjectPool;

import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * ps：
 * 1.该类的数据是（部分）开放的，以支持不同的扩展。
 * 2.该对象不可以返回给用户，底层会进行池化
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public class PromiseTask<V> implements IFutureTask<V> {

    // 低8位和TaskOptions保持一致，以方便继承数据
    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    public static final int MASK_TASK_TYPE = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种，可支持复杂的调度 */
    public static final int MASK_SCHEDULE_TYPE = 0xF000;

    /** 延时任务已触发过 */
    public static final int MASK_TRIGGERED = 1 << 16;
    /** 延时任务有超时时间 */
    public static final int MASK_HAS_DEADLINE = 1 << 17;
    /** 延时任务有次数限制 */
    public static final int MASK_HAS_COUNTDOWN = 1 << 18;
    /** 延时任务是否已启动 */
    public static final int MASK_STARTED = 1 << 19;
    /** 延时任务是否已停止 */
    public static final int MASK_STOPPED = 1 << 20;

    /** 任务类型的偏移量 */
    public static final int OFFSET_TASK_TYPE = 8;
    /** 调度类型的偏移量 */
    public static final int OFFSET_SCHEDULE_TYPE = 12;

    /** 用户的任务 */
    private Object task;
    /** 任务上下文 */
    private Object ctx;
    /** 调度选项 */
    protected int options;
    /** 任务关联的promise */
    protected IPromise<V> promise;
    /** 控制标记 */
    protected int ctl;

    protected PromiseTask() {
    }

    // region init

    /** 用于池化 */
    protected final void init(int taskType, Object task, Object ctx, int options, IPromise<V> promise) {
        this.task = Objects.requireNonNull(task, "action");
        this.ctx = ctx;
        this.options = options;
        this.promise = Objects.requireNonNull(promise, "promise");

        this.ctl = (options & TaskOptions.MASK_CTL_RESERVED);
        this.ctl |= (taskType << OFFSET_TASK_TYPE);
    }

    // endregion

    // region open

    @Override
    public final int getOptions() {
        return options;
    }

    @Override
    public final IFuture<V> future() {
        return promise;
    }

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public final int getTaskType() {
        return (ctl & MASK_TASK_TYPE) >> OFFSET_TASK_TYPE;
    }

    /** 任务是否启用了指定选项 */
    public final boolean isEnabled(int taskOption) {
        return TaskOptions.isEnabled(options, taskOption);
    }

    // endregion

    // region core

    protected void prepareToRecycle() {
        if (getClass() == PromiseTask.class) {
            POOL.release(this);
        }
    }

    /** 注意：如果task和promise之间是双向绑定的，需要解除绑定 */
    protected void reset() {
        task = null;
        ctx = null;
        options = 0;
        promise = null;
        ctl = 0;
    }

    /** 获取关联的取消令牌 */
    protected final ICancelToken getCancelToken() {
        return ExecutorCoreUtils.getCancelToken(ctx, options);
    }

    /** 运行其它类型任务 */
    @SuppressWarnings("unchecked")
    protected final V runTask() throws Exception {
        int type = (ctl & MASK_TASK_TYPE) >> OFFSET_TASK_TYPE;
        switch (type) {
            case TaskBuilder.TYPE_ACTION -> {
                Runnable task = (Runnable) this.task;
                task.run();
                return null;
            }
            case TaskBuilder.TYPE_FUNC -> {
                Callable<V> task = (Callable<V>) this.task;
                return task.call();
            }
            case TaskBuilder.TYPE_FUNC_CTX -> {
                Function<Object, V> task = (Function<Object, V>) this.task;
                return task.apply(ctx);
            }
            case TaskBuilder.TYPE_ACTION_CTX -> {
                Consumer<Object> task = (Consumer<Object>) this.task;
                task.accept(ctx);
                return null;
            }
            default -> {
                throw new AssertionError("type: " + type);
            }
        }
    }

    /** 子类不要直接调用该方法，该方法会触发回收 */
    @Override
    public void run() {
        runImpl();
        prepareToRecycle();
    }

    protected final void runImpl() {
        IPromise<V> promise = this.promise;
        ICancelToken cancelToken = getCancelToken();
        if (cancelToken.isCancelRequested()) {
            trySetCancelled(promise, cancelToken);
            return;
        }
        if (!promise.trySetComputing()) {
            return;
        }
        try {
            V result = runTask();
            promise.trySetResult(result);
        } catch (Throwable e) {
            promise.trySetException(e);
        }
    }

    // region util

    protected static boolean trySetCancelled(IPromise<?> promise, ICancelToken cancelToken) {
        int cancelCode = cancelToken.cancelCode();
        assert cancelCode != 0;
        return promise.trySetCancelled(cancelCode);
    }

    protected static boolean trySetCancelled(IPromise<?> promise, ICancelToken cancelToken, int def) {
        int cancelCode = cancelToken.cancelCode();
        if (cancelCode == 0) cancelCode = def;
        return promise.trySetCancelled(cancelCode);
    }

    // endregion

    // region factory
    private static final ConcurrentObjectPool<PromiseTask<?>> POOL = new ConcurrentObjectPool<>(
            PromiseTask::new, PromiseTask::reset, TaskPoolConfig.getPoolSize(TaskPoolType.PROMISE_TASK));

    /**
     * 申请一个PromiseTask对象，Task在进入完成状态后会自动回收。
     * 注意：该对象不可返回给用户！该对象不可返回给用户！该对象不可返回给用户！
     *
     * @param taskType 任务类型 -- 注意上下文的类型
     * @param task     用户的任务，支持的类型见{@link TaskBuilder#taskType(Object)}
     * @param ctx      任务关联的上下文
     * @param options  任务的调度选项
     * @param promise  任务关联的promise
     */
    public static <V> PromiseTask<V> acquire(int taskType, Object task, Object ctx, int options,
                                             IPromise<V> promise) {
        @SuppressWarnings("unchecked") PromiseTask<V> promiseTask = (PromiseTask<V>) POOL.acquire();
        promiseTask.init(taskType, task, ctx, options, promise);
        return promiseTask;
    }

    public static PromiseTask<?> ofAction(Runnable action, ICancelToken cancelToken, int options, IPromise<?> promise) {
        return acquire(TaskBuilder.TYPE_ACTION, action, cancelToken, options, promise);
    }

    public static PromiseTask<?> ofAction(Consumer<Object> action, Object ctx, int options, IPromise<?> promise) {
        return acquire(TaskBuilder.TYPE_ACTION_CTX, action, ctx, options, promise);
    }

    public static <V> PromiseTask<V> ofFunction(Callable<? extends V> action, ICancelToken cancelToken, int options, IPromise<V> promise) {
        return acquire(TaskBuilder.TYPE_FUNC, action, cancelToken, options, promise);
    }

    public static <V> PromiseTask<V> ofFunction(Function<Object, ? extends V> action, Object ctx, int options, IPromise<V> promise) {
        return acquire(TaskBuilder.TYPE_FUNC_CTX, action, ctx, options, promise);
    }

    public static <V> PromiseTask<V> ofBuilder(TaskBuilder<V> builder, IPromise<V> promise) {
        return acquire(builder.getType(), builder.getTask(), builder.getCtx(), builder.getOptions(), promise);
    }
    // endregion

}