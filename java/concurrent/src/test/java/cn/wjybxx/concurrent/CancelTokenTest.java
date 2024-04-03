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

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.mutable.MutableInt;
import cn.wjybxx.base.mutable.MutableObject;
import cn.wjybxx.disruptor.RingBufferEventSequencer;
import cn.wjybxx.sequential.UniCancelTokenSource;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * @author wjybxx
 * date - 2024/1/12
 */
public class CancelTokenTest {

    /** 用于测试异步执行 */
    private static final EventLoop globalEventLoop = EventLoopBuilder.newDisruptBuilder()
            .setThreadFactory(new DefaultThreadFactory("Scheduler", true))
            .setEventSequencer(RingBufferEventSequencer
                    .newMultiProducer(RingBufferEvent::new)
                    .build())
            .build();

    @BeforeAll
    static void beforeAll() {
        LoggerFactory.getILoggerFactory(); // init
    }

    private static final AtomicInteger mode = new AtomicInteger(0);

    private static ICancelTokenSource newTokenSource() {
        if ((mode.incrementAndGet() & 1) == 0) {
            return new CancelTokenSource();
        } else {
            return new UniCancelTokenSource();
        }
    }

    private static ICancelTokenSource newTokenSource(int code) {
        if ((mode.incrementAndGet() & 1) == 0) {
            return newTokenSource(code);
        } else {
            return new UniCancelTokenSource(code);
        }
    }

    // region 公共测试

    @RepeatedTest(4)
    void testRegisterBeforeCancel() {
        ICancelTokenSource cts = newTokenSource();
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    /** 测试是否立即执行 */
    @RepeatedTest(4)
    void testRegisterAfterCancel() {
        ICancelTokenSource cts = newTokenSource(1);
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNotNull(signal.getValue());
        }
    }

    /** unregister似乎比deregister的使用率更高... */
    @RepeatedTest(4)
    void testUnregister() {
        ICancelTokenSource cts = newTokenSource(0);
        {
            final MutableObject<String> signal = new MutableObject<>();
            IRegistration handle = cts.thenRun(() -> {
                signal.setValue("cancelled");
            });
            handle.close();

            cts.cancel(1);
            Assertions.assertNull(signal.getValue());
        }
    }

    /** 测试多个监听的取消 */
    @RepeatedTest(10)
    void testUnregister2() {
        ICancelTokenSource cts = newTokenSource(0);
        {
            // 通知是单线程的，因此无需使用Atomic
            final MutableInt counter = new MutableInt(0);
            final int count = 10;
            List<IRegistration> registrationList = new ArrayList<>(count);
            for (int i = 0; i < count; i++) {
                registrationList.add(cts.thenRun(counter::increment));
            }
            // 打乱顺序，然后随机取消一部分
            Collections.shuffle(registrationList);

            int cancelCount = MathCommon.SHARED_RANDOM.nextInt(count);
            for (int i = 0; i < cancelCount; i++) {
                registrationList.get(i).close();
            }
            cts.cancel(1);
            Assertions.assertEquals(count - cancelCount, counter.intValue());
        }
    }

    /** 测试在已取消的令牌上监听取消，然后中断线程 */
    @RepeatedTest(4)
    void testInterrupt() {
        ICancelTokenSource cts = newTokenSource(0);
        cts.cancel(1);

        Thread thread = Thread.currentThread();
        cts.thenRun(thread::interrupt);

        boolean interrupted;
        try {
            thread.join(10 * 1000);
            interrupted = false;
        } catch (InterruptedException ignore) {
            interrupted = true;
        }
        Assertions.assertTrue(interrupted);
    }

    @RepeatedTest(4)
    void testThenAccept() {
        ICancelTokenSource cts = newTokenSource();
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenAccept((token) -> {
                Assertions.assertSame(cts, token);
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    @RepeatedTest(4)
    void testThenAcceptCtx() {
        ICancelTokenSource cts = newTokenSource();
        Context<String> rootCtx = Context.ofBlackboard("root");
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenAccept((token, ctx) -> {
                Assertions.assertSame(rootCtx, ctx);
                Assertions.assertSame(cts, token);
                signal.setValue("cancelled");
            }, rootCtx);
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    @RepeatedTest(4)
    void testThenRun() {
        ICancelTokenSource cts = newTokenSource();
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    @RepeatedTest(4)
    void testThenRunCtx() {
        ICancelTokenSource cts = newTokenSource();
        Context<String> rootCtx = Context.ofBlackboard("root");
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenRun((ctx) -> {
                Assertions.assertSame(rootCtx, ctx);
                signal.setValue("cancelled");
            }, rootCtx);
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    @RepeatedTest(4)
    void testNotify() {
        ICancelTokenSource cts = newTokenSource();
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.thenNotify((token) -> {
                Assertions.assertSame(cts, token);
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    @RepeatedTest(4)
    void testTransferTo() {
        final MutableObject<String> signal = new MutableObject<>();
        ICancelTokenSource child = newTokenSource();
        {
            child.thenRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
        }
        ICancelTokenSource cts = newTokenSource();
        cts.thenTransferTo(child);
        cts.cancel(1);

        Assertions.assertNotNull(signal.getValue());
    }

    // endregion

    // region Cts

    @Test
    void testThenAcceptAsync() {
        ICancelTokenSource cts = newTokenSource();
        {
            final Promise<String> signal = new Promise<>();
            cts.thenAcceptAsync(globalEventLoop, (token) -> {
                Assertions.assertTrue(globalEventLoop.inEventLoop());
                Assertions.assertSame(cts, token);
                signal.trySetResult("cancelled");
            });

            Assertions.assertFalse(signal.isDone());
            cts.cancel(1);
            signal.awaitUninterruptibly();
            Assertions.assertNotNull(signal.resultNow());
        }
    }

    @Test
    void testThenAcceptCtxAsync() {
        ICancelTokenSource cts = newTokenSource();
        Context<String> rootCtx = Context.ofBlackboard("root");
        {
            final Promise<String> signal = new Promise<>();
            cts.thenAcceptAsync(globalEventLoop, (token, ctx) -> {
                Assertions.assertTrue(globalEventLoop.inEventLoop());
                Assertions.assertSame(rootCtx, ctx);
                Assertions.assertSame(cts, token);
                signal.trySetResult("cancelled");
            }, rootCtx);

            Assertions.assertFalse(signal.isDone());
            cts.cancel(1);
            signal.awaitUninterruptibly();
            Assertions.assertNotNull(signal.resultNow());
        }
    }

    @Test
    void testThenRunAsync() {
        ICancelTokenSource cts = newTokenSource();
        {
            final Promise<String> signal = new Promise<>();
            cts.thenRunAsync(globalEventLoop, () -> {
                Assertions.assertTrue(globalEventLoop.inEventLoop());
                signal.trySetResult("cancelled");
            });

            Assertions.assertFalse(signal.isDone());
            cts.cancel(1);
            signal.awaitUninterruptibly();
            Assertions.assertNotNull(signal.resultNow());
        }
    }

    @Test
    void testThenRunCtxAsync() {
        ICancelTokenSource cts = newTokenSource();
        Context<String> rootCtx = Context.ofBlackboard("root");
        {
            final Promise<String> signal = new Promise<>();
            cts.thenRunAsync(globalEventLoop, (ctx) -> {
                Assertions.assertTrue(globalEventLoop.inEventLoop());
                Assertions.assertSame(rootCtx, ctx);
                signal.trySetResult("cancelled");
            }, rootCtx);

            Assertions.assertFalse(signal.isDone());
            cts.cancel(1);
            signal.awaitUninterruptibly();
            Assertions.assertNotNull(signal.resultNow());
        }
    }

    @Test
    void testDelayInterrupt() {
        ICancelTokenSource cts = new CancelTokenSource();
        cts.cancelAfter(1, 100, TimeUnit.MILLISECONDS);

        Thread thread = Thread.currentThread();
        cts.thenRun(thread::interrupt);

        boolean interrupted;
        try {
            thread.join(10 * 1000);
            interrupted = false;
        } catch (InterruptedException ignore) {
            interrupted = true;
        }
        Assertions.assertTrue(interrupted);
    }

    // endregion

    // region UniCts

    /** 测试{@link UniCancelTokenSource#unregister(Object)} */
    @RepeatedTest(10)
    void testUniUnregister() {
        UniCancelTokenSource cts = new UniCancelTokenSource();
        {
            // 通知是单线程的，因此无需使用Atomic
            final MutableInt counter = new MutableInt(0);
            final int count = 10;
            List<Runnable> actionList = new ArrayList<>(count);
            for (int i = 0; i < count; i++) {
                Runnable action = counter::increment;
                cts.thenRun(action);
                actionList.add(action);
            }
            // 打乱顺序，然后随机取消一部分
            Collections.shuffle(actionList);

            int cancelCount = MathCommon.SHARED_RANDOM.nextInt(count);
            for (int i = 0; i < cancelCount; i++) {
                Assertions.assertTrue(cts.unregister(actionList.get(i)), "unregister failed");
            }
            cts.cancel(1);
            Assertions.assertEquals(count - cancelCount, counter.intValue());
        }
    }

    @Test
    void testCancelCode() {
        int reason = 1024;
        int degree = 7;

        CancelCodeBuilder builder = new CancelCodeBuilder()
                .setReason(reason)
                .setDegree(degree)
                .setInterruptible(true);
        Assertions.assertEquals(reason, builder.getReason());
        Assertions.assertEquals(degree, builder.getDegree());
        Assertions.assertTrue(builder.isInterruptible());
        final int code = builder.build();

        ICancelTokenSource cts = newTokenSource(0);
        cts.cancel(code);

        Assertions.assertEquals(code, cts.cancelCode());
        Assertions.assertEquals(reason, cts.reason());
        Assertions.assertEquals(degree, cts.degree());
        Assertions.assertTrue(cts.isInterruptible());
    }
}