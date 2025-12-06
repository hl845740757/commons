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

import javax.annotation.Nonnull;

/**
 * 简单等待N帧
 * <p>
 * 注：每Execute一次代表一次心跳，如果需要取用游戏的真实帧号，请自行实现。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public class SimpleWaitFrame<T> extends LeafTask<T> {

    private int required = 1;
    private transient int count;

    public SimpleWaitFrame() {
    }

    public SimpleWaitFrame(int required) {
        this.required = required;
    }

    @Override
    protected void enter(int reentryId) {
        count = 0;
        if (required == 0) {
            setSuccess();
        }
    }

    @Override
    protected void execute() {
        if (++count >= required) {
            setSuccess();
        }
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

    public int getCount() {
        return count;
    }
}