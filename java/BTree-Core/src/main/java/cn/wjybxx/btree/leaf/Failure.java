/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree.leaf;

import cn.wjybxx.btree.LeafTask;
import cn.wjybxx.btree.TaskStatus;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
public class Failure<T> extends LeafTask<T> {

    private int failureStatus;

    @Override
    protected void execute() {
        setFailed(TaskStatus.toFailure(failureStatus));
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

    public int getFailureStatus() {
        return failureStatus;
    }

    public void setFailureStatus(int failureStatus) {
        this.failureStatus = failureStatus;
    }
}
