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

package cn.wjybxx.btree;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2025/12/6
 */
public class WaitFrame<T> extends LeafTask<T> {

    private int required = 1;
    private transient int enterFrame;
    private transient int exitFrame;

    public WaitFrame() {
    }

    public WaitFrame(int required) {
        this.required = required;
    }

    private TimingTaskEntry<T> getTaskEntry0() {
        return (TimingTaskEntry<T>) taskEntry;
    }

    public int getRunFrames() {
        if (isRunning()) {
            return getTaskEntry0().frameCount - enterFrame;
        }
        return exitFrame - enterFrame;
    }

    @Override
    protected void enter(int reentryId) {
        enterFrame = getTaskEntry0().frameCount;
    }

    @Override
    protected void execute() {
        int count = getTaskEntry0().frameCount - enterFrame;
        if (count >= required) {
            setSuccess();
        }
    }

    @Override
    protected void exit() {
        exitFrame = getTaskEntry0().frameCount;
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }
}