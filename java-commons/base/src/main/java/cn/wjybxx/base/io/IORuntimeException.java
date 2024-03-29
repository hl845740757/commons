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

package cn.wjybxx.base.io;

/**
 * 用于替代{@link java.io.IOException}的非受检异常。
 * java的受检异常要慎用。。。
 *
 * @author wjybxx
 * date - 2024/1/23
 */
public class IORuntimeException extends RuntimeException {

    public IORuntimeException() {
    }

    public IORuntimeException(String message) {
        super(message);
    }

    public IORuntimeException(Throwable cause) {
        super(cause);
    }

    public IORuntimeException(String message, Throwable cause) {
        super(message, cause);
    }

    public IORuntimeException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }
}
