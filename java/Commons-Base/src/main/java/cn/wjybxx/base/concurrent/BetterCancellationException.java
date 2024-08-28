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

import java.util.Objects;
import java.util.concurrent.CancellationException;

/**
 * 该异常可以传递取消码
 *
 * @author wjybxx
 * date - 2024/1/15
 */
public class BetterCancellationException extends CancellationException {

    private final int code;

    public BetterCancellationException(int code) {
        super(formatMessage(code, null));
        this.code = CancelCodes.checkCode(code);
    }

    public BetterCancellationException(int code, String message) {
        super(formatMessage(code, message));
        this.code = CancelCodes.checkCode(code);
    }

    /** 取消码 */
    public int getCode() {
        return code;
    }

    private static String formatMessage(int code, String message) {
        if (message == null) {
            return "The task was canceled, code: " + code;
        }
        return String.format("The task was canceled, code: %d, message: %s", code, message);
    }

    /**
     * 捕获目标异常 -- 在目标异常的堆栈基础上增加当前堆栈。
     * 作用：异步任务在重新抛出异常时应当记录当前堆栈，否则会导致用户的代码被中断而没有被记录。
     */
    public static BetterCancellationException capture(Exception ex) {
        Objects.requireNonNull(ex);
        if (ex instanceof StacklessCancellationException slex) {
            return new BetterCancellationException(slex.getCode(), slex.getMessage());
        }
        BetterCancellationException r;
        if (ex instanceof BetterCancellationException ex2) {
            r = new BetterCancellationException(ex2.getCode(), ex.getMessage());
        } else {
            r = new BetterCancellationException(CancelCodes.REASON_DEFAULT);
        }
        r.initCause(ex);
        return r;
    }
}