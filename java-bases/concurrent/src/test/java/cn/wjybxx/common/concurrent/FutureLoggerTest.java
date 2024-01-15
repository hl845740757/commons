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

import org.junit.jupiter.api.Test;
import org.slf4j.event.Level;

/**
 * @author wjybxx
 * date - 2024/1/15
 */
public class FutureLoggerTest {

    /** trace应该是打印不出来的 */
    @Test
    void testTrace() {
        FutureLogger.setLogLevel(Level.TRACE);
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor,()-> {
            throw new RuntimeException("Trace");
        });
    }

    /** trace应该是打印不出来的 */
    @Test
    void testWarn() {
        FutureLogger.setLogLevel(Level.WARN);
        IExecutor executor = (command, options) -> command.run();
        FutureUtils.submitCall(executor,()-> {
            throw new RuntimeException("Warn");
        });
    }
}
