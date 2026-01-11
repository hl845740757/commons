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

import java.util.ArrayList;
import java.util.Objects;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

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
    /** 用于检测复用 */
    private int reentryId;
    /** 监听器列表 -- 通知期间可能会被重用 */
    private final ArrayList<CallbackInfo> callbacks = new ArrayList<>();

    public CancelToken() {
    }

    public CancelToken(int code) {
        if (code != 0) {
            CancelCodes.checkCode(code);
        }
        this.code = code;
    }

    /** 收到其它地方的取消信号 -- 用户不应该调用该方法 */
    @Deprecated
    @Override
    public final void onCancelRequested(CancelToken cancelToken, Object ctx) {
        cancel(cancelToken.cancelCode());
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
        callbacks.clear();
    }

    /** 重入id，允许外部捕获 */
    public final int getReentryId() {
        return reentryId;
    }

    //region query

    /** 是否支持取消 */
    public final boolean canBeCancelled() {
        return true;
    }

    /** 取消码 */
    public final int cancelCode() {
        return code;
    }

    /** 是否已收到取消信号 */
    public final boolean isRequested() {
        return code != 0;
    }

    /** 取消的原因 */
    public final int reason() {
        return CancelCodes.getReason(code);
    }

    /** 取消的紧急程度 */
    public final int degree() {
        return CancelCodes.getDegree(code);
    }

    //endregion

    //region cancel

    public final boolean cancel() {
        return cancel(CancelCodes.REASON_DEFAULT);
    }

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
        ArrayList<CallbackInfo> callbacks = cancelToken.callbacks;
        if (callbacks.size() == 0) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        for (int idx = 0, len = callbacks.size(); idx < len; idx++) {
            var callbackInfo = callbacks.set(idx, null);
            if (callbackInfo == null) {
                continue;
            }
            try {
                invoke(cancelToken, callbackInfo);
            } catch (Throwable e) {
                Task.logger.info("listener caught exception", e);
            }
            if (reentryId != cancelToken.reentryId) {
                return; // 在通知期间被Reset
            }
        }
        callbacks.clear();
    }

    @SuppressWarnings("unchecked")
    private static void invoke(CancelToken cancelToken, CallbackInfo callbackInfo) {
        if (callbackInfo.action instanceof ICancelTokenListener listener) {
            listener.onCancelRequested(cancelToken, callbackInfo.state);
            return;
        }
        if (callbackInfo.action instanceof BiConsumer) {
            BiConsumer<CancelToken, Object> action = (BiConsumer<CancelToken, Object>) callbackInfo.action;
            action.accept(cancelToken, callbackInfo.state);
            return;
        }
        {
            Consumer<CancelToken> action = (Consumer<CancelToken>) callbackInfo.action;
            action.accept(cancelToken);
        }
    }

    //endregion

    //region 监听器

    /** 添加监听器 */
    public final void registerCallback(ICancelTokenListener listener) {
        registerCallback(listener, null);
    }

    /** 添加监听器 */
    public final void registerCallback(ICancelTokenListener listener, Object ctx) {
        Objects.requireNonNull(listener);
        if (listener == this) throw new IllegalArgumentException("add self");
        if (code != 0) {
            try {
                listener.onCancelRequested(this, null);
            } catch (Throwable e) {
                Task.logger.info("listener caught exception", e);
            }
        } else {
            callbacks.add(new CallbackInfo(listener, ctx));
        }
    }

    /** 添加监听器 */
    public final void registerCallback(BiConsumer<CancelToken, Object> callback, Object ctx) {
        Objects.requireNonNull(callback);
        if (code != 0) {
            try {
                callback.accept(this, ctx);
            } catch (Throwable e) {
                Task.logger.info("listener caught exception", e);
            }
        } else {
            callbacks.add(new CallbackInfo(callback, ctx));
        }
    }

    public final void registerCallback(Consumer<CancelToken> callback) {
        Objects.requireNonNull(callback);
        if (code != 0) {
            try {
                callback.accept(this);
            } catch (Throwable e) {
                Task.logger.info("listener caught exception", e);
            }
        } else {
            callbacks.add(new CallbackInfo(callback, null));
        }
    }

    /** 删除监听器 */
    public final boolean unregisterCallback(Object callback) {
        return unregisterCallback(callback, false);
    }

    /**
     * 删除监听器
     * 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
     *
     * @param callback        要删除的监听器
     * @param firstOccurrence 是否强制正向查找删除
     * @return 存在匹配的监听器则返回true
     */
    public final boolean unregisterCallback(Object callback, boolean firstOccurrence) {
        int index = indexOfCallback(callback, firstOccurrence);
        if (index < 0) {
            return false;
        }
        if (code != 0) { // 正在通知
            callbacks.set(index, null);
        } else {
            callbacks.remove(index);
        }
        return true;
    }

    private int indexOfCallback(Object action, boolean firstOccurrence) {
        if (firstOccurrence) {
            for (int idx = 0; idx < callbacks.size(); idx++) {
                CallbackInfo callbackInfo = callbacks.get(idx);
                if (callbackInfo != null && Objects.equals(callbackInfo.action, action)) return idx;
            }
        } else {
            for (int idx = callbacks.size() - 1; idx >= 0; idx--) {
                CallbackInfo callbackInfo = callbacks.get(idx);
                if (callbackInfo != null && Objects.equals(callbackInfo.action, action)) return idx;
            }
        }
        return -1;
    }

    private static class CallbackInfo {
        public final Object action;
        public final Object state;

        public CallbackInfo(Object action, Object state) {
            this.action = action;
            this.state = state;
        }
    }
    //endregion
}