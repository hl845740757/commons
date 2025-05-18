/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

package cn.wjybxx.base.concurrent;

import javax.annotation.Nullable;
import java.util.Objects;
import java.util.concurrent.Executor;
import java.util.function.Consumer;

/**
 * 该工具类用于实现代码复用
 * (主要涉及到取消令牌)
 *
 * @author wjybxx
 * date - 2025/5/18
 */
public class ExecutorCoreUtils {

    /** 当时是否在事件循环线程 */
    public static boolean inEventLoop(@Nullable Executor executor) {
        return executor instanceof SingleThreadExecutor eventLoop && eventLoop.inEventLoop();
    }

    /** 获取上下文中的取消令牌 */
    public static ICancelToken getCancelToken(Object ctx, int options) {
        if (ctx == null || TaskOptions.isEnabled(options, TaskOptions.STAGE_UNCANCELLABLE_CTX)) {
            return ICancelToken.NONE;
        }
        if (ctx instanceof ICancelToken cancelToken) {
            return cancelToken;
        }
        if (ctx instanceof IContext ctx2) {
            return ctx2.cancelToken();
        }
        return ICancelToken.NONE;
    }

    /** 获取上下文中的取消信号 */
    public static boolean isCancelRequested(Object ctx, int options) {
        if (ctx == null || TaskOptions.isEnabled(options, TaskOptions.STAGE_UNCANCELLABLE_CTX)) {
            return false;
        }
        if (ctx instanceof ICancelToken cancelToken) {
            return cancelToken.isCancelRequested();
        }
        if (ctx instanceof IContext ctx2) {
            return ctx2.cancelToken().isCancelRequested();
        }
        return false;
    }

    /** 判断是否可以不提交任务，而是立即执行 */
    public static boolean isInlinable(@Nullable Executor e, int options) {
        if (e == null) return true;
        return TaskOptions.isEnabled(options, TaskOptions.STAGE_TRY_INLINE)
                && e instanceof SingleThreadExecutor eventLoop
                && eventLoop.inEventLoop();
    }

    // region box

    public static ITask toTask(Runnable action, int options) {
        // 注意：不论options是否为0都需要封装，否则action为ITask类型时将产生错误
        Objects.requireNonNull(action, "action");
        return new Task1(action, options);
    }

    public static ITask toTask(Runnable action, ICancelToken cancelToken, int options) {
        Objects.requireNonNull(action, "action");
        return new Task2(action, cancelToken, options);
    }

    public static ITask toTask(Consumer<Object> action, Object ctx, int options) {
        Objects.requireNonNull(action, "action");
        return new Task3(action, ctx, options);
    }

    // endregion

    // region wrapper

    private static class Task1 implements ITask {

        private Runnable action;
        private final int options;

        public Task1(Runnable action, int options) {
            this.action = action;
            this.options = options;
        }

        @Override
        public int getOptions() {
            return options;
        }

        @Override
        public void run() {
            Runnable action = this.action;
            {
                this.action = null;
            }
            action.run();
        }
    }

    private static class Task2 implements ITask {

        private Runnable action;
        private ICancelToken cancelToken;
        private final int options;

        public Task2(Runnable action, ICancelToken cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        @Override
        public int getOptions() {
            return options;
        }

        @Override
        public void run() {
            Runnable action = this.action;
            ICancelToken cancelToken = this.cancelToken;
            {
                this.action = null;
                this.cancelToken = null;
            }
            if (cancelToken != null && cancelToken.isCancelRequested()) {
                return; // 抛出异常没有意义，检测信号即可
            }
            action.run();
        }
    }

    private static class Task3 implements ITask {

        private Consumer<Object> action;
        private Object ctx;
        private final int options;

        public Task3(Consumer<Object> action, Object ctx, int options) {
            this.action = action;
            this.ctx = ctx;
            this.options = options;
        }

        @Override
        public int getOptions() {
            return options;
        }

        @Override
        public void run() {
            Consumer<Object> action = this.action;
            Object ctx = this.ctx;
            {
                this.action = null;
                this.ctx = null;
            }
            if (ExecutorCoreUtils.isCancelRequested(ctx, options)) {
                return;
            }
            action.accept(ctx);
        }
    }

    // endregion
}