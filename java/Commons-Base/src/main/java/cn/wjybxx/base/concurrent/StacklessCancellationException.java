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
package cn.wjybxx.base.concurrent;

import cn.wjybxx.base.ex.NoLogRequiredException;

/**
 * 不打印堆栈的取消异常
 *
 * @author wjybxx
 * date 2023/4/3
 */
public final class StacklessCancellationException extends BetterCancellationException implements NoLogRequiredException {

    private static final StacklessCancellationException[] INST_CACHE = new StacklessCancellationException[10];

    public static final StacklessCancellationException DEFAULT;
    public static final StacklessCancellationException TIMEOUT;
    public static final StacklessCancellationException TRIGGER_COUNT_LIMIT;

    static {
        for (int idx = 0; idx < INST_CACHE.length; idx++) {
            INST_CACHE[idx] = new StacklessCancellationException(idx + 1);
        }
        DEFAULT = INST_CACHE[0];
        TIMEOUT = INST_CACHE[CancelCodes.REASON_TIMEOUT - 1];
        TRIGGER_COUNT_LIMIT = INST_CACHE[CancelCodes.REASON_TRIGGER_COUNT_LIMIT - 1];
    }

    public StacklessCancellationException(int code) {
        super(code);
    }

    public StacklessCancellationException(int code, String message) {
        super(code, message);
    }

    public Throwable fillInStackTrace() {
        return this;
    }

    public static StacklessCancellationException instOf(int code) {
        if (code > 0 && code <= INST_CACHE.length) {
            return INST_CACHE[code - 1];
        }
        return new StacklessCancellationException(code);
    }
}