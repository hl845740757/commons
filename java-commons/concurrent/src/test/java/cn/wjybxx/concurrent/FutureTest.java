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

import cn.wjybxx.base.mutable.MutableLong;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import org.apache.commons.lang3.StringUtils;
import org.junit.jupiter.api.Assertions;
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
                    .newMultiProducer(RingBufferEvent::new)
                    .build())
            .build();

    /** 用于内联测试 */
    private static long curSequence() {
        return globalEventLoop.getBarrier().sequence();
    }

    // region basic

    @Test
    void testCtx() {
        IExecutor executor = (command, options) -> command.run();
        IContext rootCtx = new Context<>("efg");
        FutureUtils.submitFunc(executor, (context -> {
                    Assertions.assertSame(rootCtx, context);
                    return (String) context.blackboard();
                }), rootCtx)
                .resultNow();
    }

    @Test
    void testCancel() {
        CancelTokenSource cts = new CancelTokenSource(1);
        IContext rootCtx = Context.ofCancelToken(cts);

        IExecutor executor = (command, options) -> command.run();
        IFuture<String> future = FutureUtils.submitFunc(executor, ctx -> "hello", rootCtx);
        Assertions.assertTrue(future.isCancelled());
    }

    @Test
    void testAwait() throws InterruptedException {
        PromiseTask<String> promiseTask = PromiseTask.ofCallable(() -> "hello", new Promise<>());
        globalEventLoop.schedule(promiseTask, 10, TimeUnit.MILLISECONDS);

        Assertions.assertTrue(promiseTask.future().await(100, TimeUnit.SECONDS));
        Assertions.assertTrue(promiseTask.future().isDone());
    }

    @Test
    void testBlockingOp() {
        Throwable ex = globalEventLoop.submitRun(() -> {
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
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor, () -> first, 0)
                .thenAccept((context, r) -> {
                    Assertions.assertEquals(first, r);
                })
                .resultNow();
    }

    @Test
    void testAcceptAsync() {
        final String first = "abc";
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    return first;
                })
                .thenAcceptAsync(globalEventLoop, (context, r) -> {
                    // 无内联选项时，消费序号会增加
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertTrue(sequence.longValue() < curSequence());

                    Assertions.assertEquals(first, r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testAcceptAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    return first;
                })
                .thenAcceptAsync(globalEventLoop, (context, r) -> {
                    // 有内联选项时，不会提交任务
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(sequence.longValue(), curSequence());

                    Assertions.assertEquals(first, r);
                }, null, TaskOption.STAGE_TRY_INLINE)
                .awaitUninterruptibly()
                .resultNow();
    }
    // endregion

    // region apply

    @Test
    void testApply() {
        final String first = "abc";
        IExecutor executor = (command, options) -> command.run();
        String r2 = FutureUtils.submitCall(executor, () -> first, 0)
                .thenApply((ctx, r) -> StringUtils.reverse(r))
                .resultNow();
        Assertions.assertEquals(StringUtils.reverse(first), r2);
    }

    @Test
    void testApplyAsync() {
        final String first = "abc";
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    return first;
                })
                .thenApplyAsync(globalEventLoop, (context, r) -> {
                    // 无内联选项时，消费序号会增加
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertTrue(sequence.longValue() < curSequence());

                    Assertions.assertEquals(first, r);
                    return StringUtils.reverse(r);
                })
                .awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testApplyAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    return first;
                })
                .thenApplyAsync(globalEventLoop, (context, r) -> {
                    // 有内联选项时，不会提交任务
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(sequence.longValue(), curSequence());

                    Assertions.assertEquals(first, r);
                    return StringUtils.reverse(r);
                }, null, TaskOption.STAGE_TRY_INLINE)
                .awaitUninterruptibly()
                .resultNow();
    }
    // endregion

    // region catching

    @Test
    void testCatching() {
        final String first = "abc";
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor, () -> {throw new RuntimeException();})
                .catching(RuntimeException.class, (ctx, ex) -> first)
                .thenAccept((ctx, s) -> {
                    Assertions.assertEquals(first, s);
                });
    }

    @Test
    void testCatchingAsync() {
        final String first = "abc";
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    throw new RuntimeException();
                })
                .catchingAsync(globalEventLoop, RuntimeException.class, (ctx, ex) -> {
                    // 无内联选项时，消费序号会增加
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertTrue(sequence.longValue() < curSequence());
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
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    throw new RuntimeException();
                })
                .catchingAsync(globalEventLoop, RuntimeException.class, (ctx, ex) -> {
                    // 有内联选项时，不会提交任务
                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(sequence.longValue(), curSequence());
                    return first;
                }, null, TaskOption.STAGE_TRY_INLINE)
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
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor, () -> first, 0)
                .whenComplete((k, v, s) -> {})
                .thenAccept((iContext, s) -> {
                    Assertions.assertEquals(first, s);
                });
    }

    @Test
    void testWhenCompleteAsync() {
        final String first = "abc";
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    return first;
                })
                .whenCompleteAsync(globalEventLoop, (ctx, r, ex) -> {
                    // 无内联选项时，消费序号会增加
                    Assertions.assertTrue(sequence.longValue() < curSequence());

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
        MutableLong sequence = new MutableLong(0);
        globalEventLoop.submitCall(() -> {
                    sequence.setValue(curSequence());
                    return first;
                })
                .whenCompleteAsync(globalEventLoop, (ctx, r, ex) -> {
                    // 有内联选项时，不会提交任务
                    Assertions.assertEquals(sequence.longValue(), curSequence());

                    Assertions.assertTrue(globalEventLoop.inEventLoop());
                    Assertions.assertEquals(first, r);
                }, null, TaskOption.STAGE_TRY_INLINE)
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
        IExecutor executor = (command, options) -> command.run();
        IFuture<String> future = FutureUtils.submitCall(executor, () -> first, 0);

        future.onCompleted((f) -> {
            Assertions.assertEquals(first, f.resultNow());
        }, 0);
    }

    @Test
    void testOnCompleteAsync() {
        final String first = "abc";
        MutableLong sequence = new MutableLong(0);
        IFuture<String> future = globalEventLoop.submitCall(() -> {
            sequence.setValue(curSequence());
            return first;
        });
        future.onCompleted(f -> {
            // 无内联选项时，消费序号会增加
            Assertions.assertTrue(globalEventLoop.inEventLoop());
            Assertions.assertTrue(sequence.longValue() < curSequence());

            Assertions.assertEquals(first, f.resultNow());
        });
        future.awaitUninterruptibly()
                .resultNow();
    }

    @Test
    void testOnCompleteAsyncInline() {
        final String first = "abc"; // 怎么测？？？
        MutableLong sequence = new MutableLong(0);
        IFuture<String> future = globalEventLoop.submitCall(() -> {
            sequence.setValue(curSequence());
            return first;
        });
        future.onCompleted(f -> {
            // 有内联选项时，不会提交任务
            Assertions.assertTrue(globalEventLoop.inEventLoop());
            Assertions.assertEquals(sequence.longValue(), curSequence());

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
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor, () -> first, 0)
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

        FutureUtils.submitCall(executor, () -> {throw new RuntimeException();}, 0)
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