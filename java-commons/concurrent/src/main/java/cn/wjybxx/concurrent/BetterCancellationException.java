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
        this.code = ICancelToken.checkCode(code);
    }

    public BetterCancellationException(int code, String message) {
        super(formatMessage(code, message));
        this.code = ICancelToken.checkCode(code);
    }

    /** 取消码 */
    public int getCode() {
        return code;
    }

    private static String formatMessage(int code, String message) {
        if (message == null) {
            return "code: " + code;
        }
        return String.format("code: %d, msg: %s", code, message);
    }
}