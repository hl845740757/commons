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

import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/30
 */
public class ScheduledPromise<T> extends Promise<T> implements IScheduledPromise<T> {

    private IScheduledFutureTask<? extends T> task;

    public ScheduledPromise() {
    }

    public ScheduledPromise(Executor executor) {
        super(executor);
    }

    @Override
    public void setTask(IScheduledFutureTask<? extends T> task) {
        this.task = task;
    }

    @Override
    public long getDelay(TimeUnit unit) {
        if (task == null) {
            return Long.MAX_VALUE; // 可能已解绑
        }
        return task.getDelay(unit);
    }

}
