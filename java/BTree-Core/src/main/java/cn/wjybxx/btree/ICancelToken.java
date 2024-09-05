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

package cn.wjybxx.btree;

import cn.wjybxx.base.concurrent.CancelCodes;

/**
 * 行为树模块使用的取消令牌
 * 1.行为树模块需要的功能不多，且需要进行一些特殊的优化，因此去除对Concurrent模块的依赖。
 * 2.关于取消码的设计，可查看<see cref="CancelCodes"/>类。
 * 3.继承<see cref="ICancelTokenListener"/>是为了方便通知子Token。
 * 4.在行为树模块，Task在运行期间最多只应该添加一次监听。
 * 5.Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
 *
 * @author wjybxx
 * date - 2024/7/14
 */
public interface ICancelToken extends ICancelTokenListener {

    /** 重置状态(行为树模块取消令牌需要复用) */
    void reset();

    // region code

    /**
     * 取消码
     * 1.按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
     * 2.低20位为取消原因；高12位为特殊信息 {@link CancelCodes#MASK_REASON}
     * 3.不为0表示已发起取消请求
     * 4.取消时至少赋值一个信息，reason通常应该赋值
     */
    int cancelCode();

    /**
     * 是否已收到取消信号
     * 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
     */
    default boolean isCancelling() {
        return cancelCode() != 0;
    }

    /**
     * 取消的原因
     * (1~10为底层使用，10以上为用户自定义)T
     */
    default int reason() {
        return CancelCodes.getReason(cancelCode());
    }

    /** 取消的紧急程度 */
    default int degree() {
        return CancelCodes.getDegree(cancelCode());
    }

    // endregion

    //region 取消操作

    /** 使用默认取消码 {@link CancelCodes#REASON_DEFAULT} */
    boolean cancel();

    /**
     * 发送取消信号
     *
     * @param cancelCode 取消码；reason部分需大于0
     * @return 是否成功已给定取消码进入取消状态
     * @throws IllegalArgumentException 如果code小于等于0；或reason部分为0
     */
    boolean cancel(int cancelCode);

    //endregion

    //region 监听器

    /** 添加监听器 */
    void addListener(ICancelTokenListener listener);

    /** 删除指定监听器 */
    boolean remListener(ICancelTokenListener listener);

    /**
     * 删除监听器
     * 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
     *
     * @param listener        要删除的监听器
     * @param firstOccurrence 是否强制正向查找删除
     * @return 存在匹配的监听器则返回true
     */
    boolean remListener(ICancelTokenListener listener, boolean firstOccurrence);

    /** 查询是否存在给定的监听器 */
    boolean hasListener(ICancelTokenListener listener);

    //endregion

    //region 其它

    /** 创建一个同类型实例(默认只拷贝环境数据) */
    default ICancelToken newInstance() {
        return newInstance(false);
    }

    /**
     * 创建一个同类型实例(默认只拷贝环境数据)
     *
     * @param copyCode 是否拷贝当前取消码
     * @return 新实例
     */
    ICancelToken newInstance(boolean copyCode);

    //endregion
}