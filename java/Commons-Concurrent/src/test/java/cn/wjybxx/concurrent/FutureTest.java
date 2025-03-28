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

import cn.wjybxx.base.function.FunctionUtils;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import org.apache.commons.lang3.StringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/10
 */
public class FutureTest {

    /** 用于测试异步执行 */
    private static final DisruptorEventLoop<IAgentEvent> globalEventLoop = EventLoopBuilder.newDisruptBuilder()
            .setThreadFactory(new DefaultThreadFactory("Scheduler", true))
            .setEventSequencer(RingBufferEventSequencer
                    .newMultiProducer(AgentEvent::new)
                    .build())
            .build();

    private static final IExecutor immediateExecutor = Runnable::run;

    @BeforeEach
    void setUp() {
        // 必须等待其它任务完成
        globalEventLoop.submit(FunctionUtils.emptyRunnable()).awaitUninterruptibly();
    }

    // region basic

    @Test
    void testCtx() {
        IExecutor executor = immediateExecutor;
        String rootCtx = "efg";
        FutureUtils.submitFunc(executor, (ctx -> {
                    Assertions.assertSame(rootCtx, ctx);
                    return (String) ctx;
                }), rootCtx)
                .resultNow();
    }

    @Test
    void testCancel() {
        CancelTokenSource cts = new CancelTokenSource(1);

        IExecutor executor = immediateExecutor;
        IFuture<String> future = FutureUtils.submitFunc(executor, ctx -> "hello", cts);
        Assertions.assertTrue(future.isCancelled());
    }

    @Test
    void testAwait() throws InterruptedException {
        Promise<String> promise = new Promise<>();
        PromiseTask<String> promiseTask = PromiseTask.ofFunction(() -> "hello", null, 0, promise);
        globalEventLoop.schedule(promiseTask, 10, TimeUnit.MILLISECONDS);

        Assertions.assertTrue(promise.await(100, TimeUnit.SECONDS));
        Assertions.assertTrue(promise.isDone());
    }

    @Test
    void testBlockingOp() {
        Throwable ex = globalEventLoop.submitAction(() -> {
                    Promise<Object> promise = new Promise<>(globalEventLoop);
                    promise.join();
                })
                .awaitUninterruptibly()
                .exceptionNow();
        Assertions.assertInstanceOf(BlockingOperationException.class, ex);
    }

    // endregion

    // region accept
    @Test
    void testAccept() {
        final String first = "abc";
        IExecutor executor = immediateExecutor;
        FutureUtils.submitFunc(executor, () -> first, 0)
                .thenAccept((context, r) -> {
                    Assertions.assertEquals(first, r);
                })
                .resultNow();
    }

    @Test
    void testAcceptAsync() {
        final String first = "abc";
        globalEventLoop.submitFunc(() -> first)
                .thenAcceptAsync(globalEventLoop, (context, r) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testAcceptAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        globalEventLoop.submitFunc(() -> first)
                .thenAcceptAsync(globalEventLoop, (context, r) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                }, null, TaskOptions.STAGE_TRY_INLINE)
                .awaitUninterruptibly()
                .resultNow();
    }
    // endregion

    // region apply

    @Test
    void testApply() {
        final String first = "abc";
        IExecutor executor = immediateExecutor;
        String r2 = FutureUtils.submitFunc(executor, () -> first, 0)
                .thenApply((ctx, r) -> StringUtils.reverse(r))
                .resultNow();
        Assertions.assertEquals(StringUtils.reverse(first), r2);
    }

    @Test
    void testApplyAsync() {
        final String first = "abc";
        globalEventLoop.submitFunc(() -> first)
                .thenApplyAsync(globalEventLoop, (context, r) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                    return StringUtils.reverse(r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testApplyAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        globalEventLoop.submitFunc(() -> first)
                .thenApplyAsync(globalEventLoop, (context, r) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                    return StringUtils.reverse(r);
                }, null, TaskOptions.STAGE_TRY_INLINE)
                .awaitUninterruptibly()
                .resultNow();
    }
    // endregion

    // region catching

    @Test
    void testCatching() {
        final String first = "abc";
        IExecutor executor = immediateExecutor;
        FutureUtils.submitFunc(executor, () -> {throw new RuntimeException();})
                .catching(RuntimeException.class, (ctx, ex) -> first)
                .thenAccept((ctx, s) -> {
                    Assertions.assertEquals(first, s);
                });
    }

    @Test
    void testCatchingAsync() {
        final String first = "abc";
        globalEventLoop.submitFunc(() -> {
                    throw new RuntimeException();
                })
                .catchingAsync(globalEventLoop, RuntimeException.class, (ctx, ex) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    return first;
                })
                .thenAccept((ctx, r) -> {
                    Assertions.assertEquals(first, r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testCatchingAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        globalEventLoop.submitFunc(() -> {
                    throw new RuntimeException();
                })
                .catchingAsync(globalEventLoop, RuntimeException.class, (ctx, ex) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    return first;
                }, null, TaskOptions.STAGE_TRY_INLINE)
                .thenAccept((ctx, r) -> {
                    Assertions.assertEquals(first, r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    // endregion

    // region whenComplete

    @Test
    void testWhenComplete() {
        final String first = "abc";
        IExecutor executor = immediateExecutor;
        FutureUtils.submitFunc(executor, () -> first, 0)
                .whenComplete((k, v, s) -> {})
                .thenAccept((iContext, s) -> {
                    Assertions.assertEquals(first, s);
                });
    }

    @Test
    void testWhenCompleteAsync() {
        final String first = "abc";
        globalEventLoop.submitFunc(() -> first)
                .whenCompleteAsync(globalEventLoop, (ctx, r, ex) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                })
                .thenAccept((ctx, r) -> {
                    Assertions.assertEquals(first, r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testWhenCompleteAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        globalEventLoop.submitFunc(() -> first)
                .whenCompleteAsync(globalEventLoop, (ctx, r, ex) -> {
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                }, null, TaskOptions.STAGE_TRY_INLINE)
                .thenAccept((ctx, r) -> {
                    Assertions.assertEquals(first, r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }
    // endregion

    // region onCompleted

    @Test
    void testOnComplete() {
        final String first = "abc";
        IExecutor executor = immediateExecutor;
        IFuture<String> future = FutureUtils.submitFunc(executor, () -> first, 0);

        future.onCompleted((f) -> {
            Assertions.assertEquals(first, f.resultNow());
        }, 0);
    }

    @Test
    void testOnCompleteAsync() {
        final String first = "abc";
        IFuture<String> future = globalEventLoop.submitFunc(() -> first);
        future.onCompleted(f -> {
            Assertions.assertTrue(globalEventLoop.inEventLoop());
            Assertions.assertEquals(first, f.resultNow());
        });
        future.awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testOnCompleteAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        IFuture<String> future = globalEventLoop.submitFunc(() -> first);
        future.onCompleted(f -> {
            Assertions.assertTrue(globalEventLoop.inEventLoop());
            Assertions.assertEquals(first, f.resultNow());
        });
        future.awaitUninterruptibly()
                .resultNow();
    }
    // endregion

    // region handle

    @Test
    void testHandle() {
        final String first = "abc";
        final String fallbackResult = "fallback:" + first;
        IExecutor executor = immediateExecutor;
        FutureUtils.submitFunc(executor, () -> first, 0)
                .handle((ctx, v, ex) -> {
                    if (ex != null) {
                        return fallbackResult;
                    }
                    return v;
                })
                .thenAccept((ctx, v) -> {
                    Assertions.assertEquals(first, v);
                })
                .join();

        FutureUtils.submitFunc(executor, () -> {
                    throw new RuntimeException();
                }, 0)
                .handle((ctx, v, ex) -> {
                    if (ex != null) {
                        return fallbackResult;
                    }
                    return v;
                })
                .thenAccept((ctx, v) -> {
                    Assertions.assertEquals(fallbackResult, v);
                })
                .join();
    }

    // endregion
}