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

import java.util.concurrent.TimeoutException;

/**
 * 不打印堆栈的超时异常
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class StacklessTimeoutException extends TimeoutException {

    public static final StacklessTimeoutException INST = new StacklessTimeoutException();

    public StacklessTimeoutException() {
    }

    public StacklessTimeoutException(String message) {
        super(message);
    }

    public final Throwable fillInStackTrace() {
        return this;
    }

}