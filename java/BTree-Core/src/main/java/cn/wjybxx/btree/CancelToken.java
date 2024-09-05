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

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.concurrent.CancelCodes;

import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

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
public class CancelToken implements ICancelTokenListener {

    /** 取消码 -- 0表示未收到信号 */
    private int code;
    /** 监听器列表 -- 通知期间可能会被重用 */
    private final List<ICancelTokenListener> listeners = new ArrayList<>();
    /** 是否正在通知 -- 处理通知期间删除监听器问题 */
    private boolean firing = false;
    /** 用于检测复用 -- short应当足够 */
    private short reentryId = 0;

    public CancelToken() {
    }

    public CancelToken(int code) {
        this.code = code;
    }

    /** 创建一个同类型实例(默认只拷贝环境数据) */
    public CancelToken newInstance() {
        return newInstance(false);
    }

    /**
     * 创建一个同类型实例(默认只拷贝环境数据)
     *
     * @param copyCode 是否拷贝当前取消码
     * @return 新实例
     */
    public CancelToken newInstance(boolean copyCode) {
        return new CancelToken(copyCode ? code : 0);
    }

    /** 重置状态(行为树模块取消令牌需要复用) */
    public void reset() {
        reentryId++;
        code = 0;
        listeners.clear();
        firing = false;
    }

    protected final int getReentryId() {
        return reentryId;
    }

    protected final boolean isFiring() {
        return firing;
    }

    //region query

    /**
     * 取消码
     * 1.按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
     * 2.低20位为取消原因；高12位为特殊信息 {@link CancelCodes#MASK_REASON}
     * 3.不为0表示已发起取消请求
     * 4.取消时至少赋值一个信息，reason通常应该赋值
     */
    public final int cancelCode() {
        return code;
    }

    /**
     * 是否已收到取消信号
     * 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
     */
    public final boolean isCancelling() {
        return code != 0;
    }

    /**
     * 取消的原因
     * (1~10为底层使用，10以上为用户自定义)
     */
    public final int reason() {
        return CancelCodes.getReason(code);
    }

    /** 取消的紧急程度 */
    public final int degree() {
        return CancelCodes.getDegree(code);
    }

    //endregion

    //region cancel

    /**
     * 发送取消信号，使用默认取消码 {@link CancelCodes#REASON_DEFAULT}
     */
    public final boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT);
    }

    /**
     * 发送取消信号
     *
     * @param cancelCode 取消码；reason部分需大于0
     * @return 是否成功已给定取消码进入取消状态
     * @throws IllegalArgumentException 如果code小于等于0；或reason部分为0
     */
    public final boolean cancel(int cancelCode) {
        CancelCodes.checkCode(cancelCode);
        int r = this.code;
        if (r == 0) {
            this.code = cancelCode;
            postComplete(this);
            return true;
        }
        return false;
    }

    private static void postComplete(CancelToken cancelToken) {
        List<ICancelTokenListener> listeners = cancelToken.listeners;
        if (listeners.isEmpty()) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        cancelToken.firing = true;
        {
            for (var idx = 0; idx < listeners.size(); idx++) {
                var listener = listeners.set(idx, TOMBSTONE); // 标记为删除，HasListener将返回false
                try {
                    listener.onCancelRequested(cancelToken);
                } catch (Exception e) {
                    Task.logger.info("listener caught exception", e);
                }
                // 在通知期间被Reset
                if (reentryId != cancelToken.reentryId) {
                    return;
                }
            }
        }
        listeners.clear();
        cancelToken.firing = false;
    }

    //endregion

    //region 监听器

    /** 添加监听器 */
    public final void addListener(ICancelTokenListener listener) {
        Objects.requireNonNull(listener);
        if (listener == this) throw new IllegalArgumentException("add self");
        if (code != 0) {
            try {
                listener.onCancelRequested(this);
            } catch (Exception e) {
                Task.logger.info("listener caught exception", e);
            }
        } else {
            listeners.add(listener);
        }
    }

    /** 删除指定监听器 */
    public final boolean remListener(ICancelTokenListener listener) {
        return remListener(listener, false);
    }

    /**
     * 删除监听器
     * 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
     *
     * @param listener        要删除的监听器
     * @param firstOccurrence 是否强制正向查找删除
     * @return 存在匹配的监听器则返回true
     */
    public final boolean remListener(ICancelTokenListener listener, boolean firstOccurrence) {
        int index = firstOccurrence
                ? CollectionUtils.indexOfRef(listeners, listener)
                : CollectionUtils.lastIndexOfRef(listeners, listener);
        if (index < 0) {
            return false;
        }
        if (firing) {
            listeners.set(index, TOMBSTONE);
        } else {
            listeners.remove(index);
        }
        return true;
    }

    /** 查询是否存在给定的监听器 */
    public final boolean hasListener(ICancelTokenListener listener) {
        return CollectionUtils.lastIndexOfRef(listeners, listener) >= 0;
    }

    //endregion

    /** 收到其它地方的取消信号 */
    @Override
    public final void onCancelRequested(CancelToken cancelToken) {
        cancel(cancelToken.cancelCode());
    }

    /** 在派发监听器时先置为该值，避免用户在回调中主动删除自己时产生异常 */
    private static final ICancelTokenListener TOMBSTONE = cancelToken -> {};

}