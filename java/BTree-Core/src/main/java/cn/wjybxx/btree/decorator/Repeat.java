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
package cn.wjybxx.btree.decorator;

import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskInlinable;
import cn.wjybxx.btree.TaskStatus;

/**
 * 重复N次
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class Repeat<T> extends LoopDecorator<T> {

    public static final int MODE_ALWAYS = 0;
    public static final int MODE_ONLY_SUCCESS = 1;
    public static final int MODE_ONLY_FAILED = 2;
    public static final int MODE_NEVER = 3;

    /** 考虑到Java枚举与其它语言的兼容性问题，我们在编辑器中使用数字 */
    private int countMode = MODE_ALWAYS;
    /** 需要重复的次数，-1表示无限重复 */
    private int required = 1;
    /** 当前计数 */
    private transient int count;

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        count = 0;
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (required < -1) {
            throw new IllegalStateException("required < -1");
        }
        count = 0;
    }

    @Override
    protected void enter(int reentryId) {
        super.enter(reentryId);
        if (required == 0) {
            setSuccess();
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        inlineHelper.stopInline();
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        boolean match = switch (countMode) {
            case MODE_ALWAYS -> true;
            case MODE_ONLY_SUCCESS -> child.isSucceeded();
            case MODE_ONLY_FAILED -> child.isFailed();
            default -> false;
        };
        if (match) {
            count++;
            if (required >= 0 && count >= required) {
                setSuccess();
                return;
            }
        }

        if (!hasNextLoop()) {
            setFailed(TaskStatus.MAX_LOOP_LIMIT);
        } else {
            template_execute(false);
        }
    }

    public int getCountMode() {
        return countMode;
    }

    public void setCountMode(int countMode) {
        this.countMode = countMode;
    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }
}