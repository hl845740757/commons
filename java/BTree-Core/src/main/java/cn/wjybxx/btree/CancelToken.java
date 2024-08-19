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

/**
 * @author wjybxx
 * date - 2024/7/14
 */
public final class CancelToken implements ICancelToken {

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

    /** 重置状态，以供复用 */
    public void reset() {
        reentryId++;
        code = 0;
        listeners.clear();
        firing = false;
    }

    //region code

    @Override
    public int cancelCode() {
        return code;
    }

    @Override
    public boolean isCancelling() {
        return code != 0;
    }

    @Override
    public int reason() {
        return CancelCodes.getReason(code);
    }

    @Override
    public int degree() {
        return CancelCodes.getDegree(code);
    }

    //endregion

    //region tokenSource

    @Override
    public int cancel() {
        return cancel(CancelCodes.REASON_DEFAULT);
    }

    @Override
    public int cancel(int cancelCode) {
        CancelCodes.checkCode(cancelCode);
        int r = this.code;
        if (r == 0) {
            this.code = cancelCode;
            postComplete(this);
        }
        return r;
    }

    /** 不能优化递归 -- 因为在通知期间用户可能会请求删除 */
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

    @Override
    public void addListener(ICancelTokenListener listener) {
        if (listener == this) {
            throw new IllegalArgumentException("add this");
        }
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

    @Override
    public boolean remListener(ICancelTokenListener listener) {
        return remListener(listener, false);
    }

    @Override
    public boolean remListener(ICancelTokenListener listener, boolean firstOccurrence) {
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

    public boolean hasListener(ICancelTokenListener listener) {
        return CollectionUtils.lastIndexOfRef(listeners, listener) >= 0;
    }

    //endregion


    @Override
    public ICancelToken newInstance() {
        return newInstance(false);
    }

    @Override
    public CancelToken newInstance(boolean copyCode) {
        return new CancelToken(copyCode ? code : 0);
    }

    @Override
    public void onCancelRequested(ICancelToken cancelToken) {
        cancel(cancelToken.cancelCode());
    }

    /** 在派发监听器时先置为该值，避免用户在回调中主动删除自己时产生异常 */
    private static final ICancelTokenListener TOMBSTONE = cancelToken -> {};

}