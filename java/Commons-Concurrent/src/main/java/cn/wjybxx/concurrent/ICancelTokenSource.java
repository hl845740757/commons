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

import cn.wjybxx.base.concurrent.CancelCodeBuilder;
import cn.wjybxx.base.concurrent.CancelCodes;

/**
 * 取消令牌源由任务的创建者（发起者）持有，具备取消权限。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public interface ICancelTokenSource extends ICancelToken {

    /**
     * 将Token置为取消状态
     *
     * @param cancelCode 取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}
     * @return 成功使用给定取消码进入取消状态则返回true，否则返回false
     * @throws IllegalArgumentException 如果code小于等于0；或reason部分为0
     */
    boolean cancel(int cancelCode);

    /** 使用默认原因取消 */
    default boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT); // 末位1，默认情况
    }

    /**
     * 创建一个同类型实例。
     * 1.原型对象，避免具体类型依赖。
     * 2.默认情况下，其它上下文应当拷贝。
     *
     * @param copyCode 是否拷贝当前取消码
     * @return 取消令牌
     */
    ICancelTokenSource newInstance(boolean copyCode);

    default ICancelTokenSource newInstance() {
        return newInstance(false);
    }
}