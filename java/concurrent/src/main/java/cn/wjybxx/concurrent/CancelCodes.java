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

import java.util.concurrent.TimeUnit;

/**
 * 取消码辅助类
 *
 * @author wjybxx
 * date - 2024/4/11
 */
public class CancelCodes {

    /**
     * 原因的掩码
     * 1.如果cancelCode不包含其它信息，就等于reason
     * 2.设定为20位，可达到100W
     */
    public static final int MASK_REASON = 0xFFFFF;
    /** 紧迫程度的掩码（4it）-- 0表示未指定 */
    public static final int MASK_DEGREE = 0x00F0_0000;
    /** 预留4bit */
    public static final int MASK_REVERSED = 0x0F00_0000;
    /** 中断的掩码 （1bit） */
    public static final int MASK_INTERRUPT = 1 << 28;
    /** 告知任务无需执行删除逻辑 -- 慎用 */
    public static final int MASK_WITHOUT_REMOVE = 1 << 29;
    /** 表示取消信号来自Future的取消接口 */
    public static final int MASK_FROM_FUTURE = 1 << 30;

    /** 最大取消原因 */
    public static final int MAX_REASON = MASK_REASON;
    /** 最大紧急程度 */
    public static final int MAX_DEGREE = 15;

    /** 取消原因的偏移量 */
    public static final int OFFSET_REASON = 0;
    /** 紧急度的偏移量 */
    public static final int OFFSET_DEGREE = 20;

    /** 默认原因 */
    public static final int REASON_DEFAULT = 1;
    /** 执行超时 -- {@link ICancelTokenSource#cancelAfter(int, long, TimeUnit)}就可使用 */
    public static final int REASON_TIMEOUT = 2;
    /** Executor关闭 -- Executor关闭不一定会取消任务 */
    public static final int REASON_SHUTDOWN = 3;

    private CancelCodes() {
    }

    /** 取消码是否表示已收到取消信号 */
    public static boolean isCancelling(int code) {
        return code != 0;
    }

    /** 计算取消码中的原因 */
    public static int getReason(int code) {
        return code & MASK_REASON;
    }

    /** 计算取消码终归的紧急程度 */
    public static int getDegree(int code) {
        return (code & MASK_DEGREE) >>> OFFSET_DEGREE;
    }

    /** 取消指令中是否要求了中断线程 */
    public static boolean isInterruptible(int code) {
        return (code & MASK_INTERRUPT) != 0;
    }

    /** 取消指令中是否要求了无需删除 */
    public static boolean isWithoutRemove(int code) {
        return (code & MASK_WITHOUT_REMOVE) != 0;
    }

    /** 取消信号是否来自future接口 */
    public static boolean isFromFuture(int code) {
        return (code & MASK_FROM_FUTURE) != 0;
    }

    /** 设置紧急程度 */
    public static int setDegree(int code, int value) {
        if (value < 0 || value > MAX_DEGREE) {
            throw new IllegalArgumentException("degree");
        }
        code &= (~MASK_DEGREE);
        code |= (value << OFFSET_DEGREE);
        return code;
    }

    /** 设置取消原因 */
    public static int setReason(int code, int value) {
        if (value <= 0 || value > MAX_REASON) {
            throw new IllegalArgumentException("reason");
        }
        code &= (~MASK_REASON);
        code |= value;
        return code;
    }

    /** 设置中断标记 */
    public static int setInterruptible(int code, boolean value) {
        return value
                ? code | MASK_INTERRUPT
                : code & (~MASK_INTERRUPT);
    }

    /** 设置是否不立即删除 */
    public static int setWithoutRemove(int code, boolean value) {
        return value
                ? code | MASK_WITHOUT_REMOVE
                : code & (~MASK_WITHOUT_REMOVE);
    }

    /**
     * 检查取消码的合法性
     *
     * @return argument
     */
    public static int checkCode(int code) {
        if (getReason(code) == 0) {
            throw new IllegalArgumentException("reason is absent");
        }
        return code;
    }
}
