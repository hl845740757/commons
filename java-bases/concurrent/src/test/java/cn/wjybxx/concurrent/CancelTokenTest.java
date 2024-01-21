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
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/12
 */
public class CancelTokenTest {

    @Test
    void testRegisterBeforeCancel() {
        CancelTokenSource cts = new CancelTokenSource();
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.registerRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
            cts.cancel(1);
            Assertions.assertNotNull(signal.getValue());
        }
    }

    /** 测试是否立即执行 */
    @Test
    void testRegisterAfterCancel() {
        CancelTokenSource cts = new CancelTokenSource(1);
        {
            final MutableObject<String> signal = new MutableObject<>();
            cts.registerRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNotNull(signal.getValue());
        }
    }

    @Test
    void testRegisterChild() {
        final MutableObject<String> signal = new MutableObject<>();
        CancelTokenSource child = new CancelTokenSource();
        {
            child.registerRun(() -> {
                signal.setValue("cancelled");
            });
            Assertions.assertNull(signal.getValue());
        }
        CancelTokenSource cts = new CancelTokenSource();
        cts.registerChild(child);
        cts.cancel(1);

        Assertions.assertNotNull(signal.getValue());
    }

    /** unregister似乎比deregister的使用率更高... */
    @Test
    void testUnregister() {
        CancelTokenSource cts = new CancelTokenSource(0);
        {
            final MutableObject<String> signal = new MutableObject<>();
            IRegistration handle = cts.registerRun(() -> {
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
        CancelTokenSource cts = new CancelTokenSource(0);
        {
            // 通知是单线程的，因此无需使用Atomic
            final MutableInt counter = new MutableInt(0);
            final int count = 5;
            List<IRegistration> registrationList = new ArrayList<>(count);
            for (int i = 0; i < count; i++) {
                registrationList.add(cts.registerRun(counter::increment));
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
    @Test
    void testInterrupt() {
        CancelTokenSource cts = new CancelTokenSource(0);
        cts.cancel(1);

        Thread thread = Thread.currentThread();
        cts.registerRun(thread::interrupt);

        boolean interrupted;
        try {
            thread.join(10 * 1000);
            interrupted = false;
        } catch (InterruptedException ignore) {
            interrupted = true;
        }
        Assertions.assertTrue(interrupted);
    }

    @Test
    void testDelayInterrupt() {
        CancelTokenSource cts = new CancelTokenSource(0);
        cts.cancelAfter(1, 100, TimeUnit.MILLISECONDS);

        Thread thread = Thread.currentThread();
        cts.registerRun(thread::interrupt);

        boolean interrupted;
        try {
            thread.join(10 * 1000);
            interrupted = false;
        } catch (InterruptedException ignore) {
            interrupted = true;
        }
        Assertions.assertTrue(interrupted);
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

        CancelTokenSource cts = new CancelTokenSource(0);
        cts.cancel(code);

        Assertions.assertEquals(code, cts.cancelCode());
        Assertions.assertEquals(reason, cts.reason());
        Assertions.assertEquals(degree, cts.degree());
        Assertions.assertTrue(cts.isInterruptible());
    }
}