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

import cn.wjybxx.base.ex.NoLogRequiredException;

/**
 * 不打印堆栈的取消异常
 *
 * @author wjybxx
 * date 2023/4/3
 */
public final class StacklessCancellationException extends BetterCancellationException implements NoLogRequiredException {

    public static final StacklessCancellationException INST1 = new StacklessCancellationException(1);
    private static final StacklessCancellationException INST2 = new StacklessCancellationException(2);
    private static final StacklessCancellationException INST3 = new StacklessCancellationException(3);
    private static final StacklessCancellationException INST4 = new StacklessCancellationException(4);

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
        return switch (code) {
            case 1 -> INST1;
            case 2 -> INST2;
            case 3 -> INST3;
            case 4 -> INST4;
            default -> new StacklessCancellationException(code);
        };
    }
}