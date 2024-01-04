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
package cn.wjybxx.common.concurrent2;

/**
 * @author wjybxx
 * date - 2023/11/18
 */
public class CancelCodeBuilder {

    private int code = IContext.REASON_DEFAULT;

    public CancelCodeBuilder() {
    }

    public int getReason() {
        return reason(code);
    }

    public CancelCodeBuilder setReason(int reason) {
        if (reason <= 0 || reason > 65535) {
            throw new IllegalArgumentException("reason");
        }
        code &= (~IContext.MASK_REASON);
        code |= reason;
        return this;
    }

    public int getDegree() {
        return urgencyDegree(code);
    }

    public CancelCodeBuilder setDegree(int degree) {
        if (degree < 0 || degree > 15) {
            throw new IllegalArgumentException("degree");
        }
        code &= (~IContext.MASK_DEGREE);
        code |= (degree << 16);
        return this;
    }

    public boolean isInterruptible() {
        return (code & IContext.MASK_INTERRUPT) != 0;
    }

    public CancelCodeBuilder setInterruptible(boolean interruptible) {
        if (interruptible) {
            code |= IContext.MASK_INTERRUPT;
        } else {
            code &= (~IContext.MASK_INTERRUPT);
        }
        return this;
    }

    public int build() {
        return checkCode(code);
    }

    // region 可对外

    /** 计算取消码中的原因 */
    public static int reason(int code) {
        return code & IContext.MASK_REASON;
    }

    /** 计算取消码终归的紧急程度 */
    public static int urgencyDegree(int code) {
        return (code & IContext.MASK_DEGREE) >>> 16;
    }

    /**
     * 检查取消码的合法性
     *
     * @return argument
     */
    public static int checkCode(int code) {
        if (code == 0) {
            throw new IllegalArgumentException("cancelCode cant be 0");
        }
        if (reason(code) == 0) {
            throw new IllegalArgumentException("reason is absent");
        }
        return code;
    }

    //endregion
}