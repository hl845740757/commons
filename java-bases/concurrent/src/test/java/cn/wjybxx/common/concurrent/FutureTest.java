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

package cn.wjybxx.common.concurrent;

import org.apache.commons.lang3.StringUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/10
 */
public class FutureTest {

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
        CompletableFuture.delayedExecutor(10, TimeUnit.MILLISECONDS)
                .execute(promiseTask);

        Assertions.assertTrue(promiseTask.future().await(100, TimeUnit.SECONDS));
        Assertions.assertTrue(promiseTask.future().isDone());
    }

    @Test
    void testApply() {
        final String first = "abc";
        IExecutor executor = (command, options) -> command.run();
        String r2 = FutureUtils.submitCall(executor, () -> first, 0)
                .thenApply((ctx, r) -> StringUtils.reverse(r))
                .toFuture()
                .resultNow();
        Assertions.assertEquals(StringUtils.reverse(first), r2);
    }

    @Test
    void testAccept() {
        final String first = "abc";
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor, () -> first, 0)
                .thenApply((ctx, r) -> StringUtils.reverse(r))
                .thenAccept((context, r2) -> {
                    Assertions.assertEquals(StringUtils.reverse(first), r2);
                }).toFuture()
                .resultNow();
    }
}
