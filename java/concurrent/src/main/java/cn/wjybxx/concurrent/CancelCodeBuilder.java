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

import javax.annotation.concurrent.NotThreadSafe;

/**
 * @author wjybxx
 * date - 2023/11/18
 */
@NotThreadSafe
public final class CancelCodeBuilder {

    private int code = ICancelToken.REASON_DEFAULT;

    public CancelCodeBuilder() {
    }

    /** 取消的原因 */
    public int getReason() {
        return ICancelToken.reason(code);
    }

    public CancelCodeBuilder setReason(int reason) {
        if (reason <= 0 || reason > ICancelToken.MAX_REASON) {
            throw new IllegalArgumentException("reason");
        }
        code &= (~ICancelToken.MASK_REASON);
        code |= reason;
        return this;
    }

    /** 紧急程度 */
    public int getDegree() {
        return ICancelToken.degree(code);
    }

    public CancelCodeBuilder setDegree(int degree) {
        if (degree < 0 || degree > ICancelToken.MAX_DEGREE) {
            throw new IllegalArgumentException("degree");
        }
        code &= (~ICancelToken.MASK_DEGREE);
        code |= (degree << ICancelToken.OFFSET_DEGREE);
        return this;
    }

    /** 是否中断线程 */
    public boolean isInterruptible() {
        return ICancelToken.isInterruptible(code);
    }

    public CancelCodeBuilder setInterruptible(boolean value) {
        if (value) {
            code |= ICancelToken.MASK_INTERRUPT;
        } else {
            code &= (~ICancelToken.MASK_INTERRUPT);
        }
        return this;
    }

    /** 是否无需立即从任务队列中删除 */
    public boolean isWithoutRemove() {
        return ICancelToken.isWithoutRemove(code);
    }

    public CancelCodeBuilder setWithoutRemove(boolean value) {
        if (value) {
            code |= ICancelToken.MASK_WITHOUT_REMOVE;
        } else {
            code &= (~ICancelToken.MASK_WITHOUT_REMOVE);
        }
        return this;
    }

    public int build() {
        return ICancelToken.checkCode(code);
    }

}