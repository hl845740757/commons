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

import javax.annotation.concurrent.NotThreadSafe;

/**
 * @author wjybxx
 * date - 2023/11/18
 */
@NotThreadSafe
public final class CancelCodeBuilder {

    private int code = CancelCodes.REASON_DEFAULT;

    public CancelCodeBuilder() {
    }

    /** 启用选项 */
    public void Enable(int optionMask) {
        code |= optionMask;
    }

    /** 禁用选项 */
    public void Disable(int optionMask) {
        code &= ~optionMask;
    }

    /** 取消的原因 */
    public int getReason() {
        return CancelCodes.getReason(code);
    }

    public CancelCodeBuilder setReason(int reason) {
        code = CancelCodes.setReason(code, reason);
        return this;
    }

    /** 紧急程度 */
    public int getDegree() {
        return CancelCodes.getDegree(code);
    }

    public CancelCodeBuilder setDegree(int degree) {
        code = CancelCodes.setDegree(code, degree);
        return this;
    }

    /** 是否中断线程 */
    public boolean isInterruptible() {
        return CancelCodes.isInterruptible(code);
    }

    public CancelCodeBuilder setInterruptible(boolean value) {
        code = CancelCodes.setInterruptible(code, value);
        return this;
    }

    /** 是否无需立即从任务队列中删除 */
    public boolean isWithoutRemove() {
        return CancelCodes.isWithoutRemove(code);
    }

    public CancelCodeBuilder setWithoutRemove(boolean value) {
        code = CancelCodes.setWithoutRemove(code, value);
        return this;
    }

    public int build() {
        return CancelCodes.checkCode(code);
    }

}