/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import javax.annotation.Nullable;

/**
 * 该异常用于定时任务返回结果。
 * 周期任务需要返回结果的情况不常见，因此我们通过异常实现。
 *
 * @author wjybxx
 * date - 2025/5/7
 */
public final class TaskResultException extends RuntimeException {

    /** 共享Null实例 */
    public static final TaskResultException NULL = new TaskResultException(null);

    private final Object result;

    public TaskResultException(@Nullable Object result) {
        super(null, null, false, false);
        this.result = result;
    }

    public Object getResult() {
        return result;
    }

    @Override
    public synchronized Throwable fillInStackTrace() {
        return this;
    }
}