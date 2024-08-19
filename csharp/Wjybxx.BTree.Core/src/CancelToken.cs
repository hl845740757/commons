#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BTree
{
/// <summary>
/// 行为树的模块的默认取消令牌实现
/// </summary>
public sealed class CancelToken : ICancelToken
{
    /** 取消码 -- 0表示未收到信号 */
    private int code;
    /** 监听器列表 -- 通知期间可能会被重用 */
    private readonly List<ICancelTokenListener> listeners = new List<ICancelTokenListener>();
    /** 是否正在通知 -- 处理通知期间删除监听器问题 */
    private bool firing = false;
    /** 用于检测复用 -- short应当足够 */
    private short reentryId = 0;

    public CancelToken() {
    }

    public CancelToken(int code) {
        this.code = code;
    }

    /** 重置状态，以供复用 */
    public void Reset() {
        reentryId++;
        code = 0;
        listeners.Clear();
        firing = false;
    }

    #region code

    public int CancelCode => code;

    public bool IsCancelling => code != 0;

    public int Reason => CancelCodes.GetReason(code);

    public int Degree => CancelCodes.GetDegree(code);

    #endregion

    #region tokenSource

    public int Cancel(int cancelCode = CancelCodes.REASON_DEFAULT) {
        CancelCodes.CheckCode(cancelCode);
        int r = this.code;
        if (r == 0) {
            this.code = cancelCode;
            PostComplete(this);
        }
        return r;
    }

    /** 不能优化递归 -- 因为在通知期间用户可能会请求删除 */
    private static void PostComplete(CancelToken cancelToken) {
        List<ICancelTokenListener> listeners = cancelToken.listeners;
        if (listeners.Count == 0) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        cancelToken.firing = true;
        {
            for (int idx = 0; idx < listeners.Count; idx++) {
                var listener = listeners[idx];
                listeners[idx] = TOMBSTONE; // 标记为删除，HasListener将返回false
                try {
                    listener.OnCancelRequested(cancelToken);
                }
                catch (Exception e) {
                    TaskLogger.Info(e, "listener caught exception");
                }
                // 在通知期间被Reset
                if (reentryId != cancelToken.reentryId) {
                    return;
                }
            }
        }
        listeners.Clear();
        cancelToken.firing = false;
    }

    #endregion

    #region 监听器

    public void AddListener(ICancelTokenListener listener) {
        if (listener == this) {
            throw new ArgumentException();
        }
        if (code != 0) {
            try {
                listener.OnCancelRequested(this);
            }
            catch (Exception e) {
                TaskLogger.Info(e, "listener caught exception");
            }
        } else {
            listeners.Add(listener);
        }
    }

    public bool RemListener(ICancelTokenListener listener, bool firstOccurrence = false) {
        int index = firstOccurrence
            ? listeners.IndexOfRef(listener)
            : listeners.LastIndexOfRef(listener);
        if (index < 0) {
            return false;
        }
        if (firing) {
            listeners[index] = TOMBSTONE;
        } else {
            listeners.RemoveAt(index);
        }
        return true;
    }

    public bool HasListener(ICancelTokenListener listener) {
        return listeners.LastIndexOfRef(listener) >= 0;
    }

    #endregion

    ICancelToken ICancelToken.NewInstance(bool copyCode) => NewInstance(copyCode);

    public CancelToken NewInstance(bool copyCode = false) {
        return new CancelToken(copyCode ? code : 0);
    }

    void ICancelTokenListener.OnCancelRequested(ICancelToken parent) {
        Cancel(parent.CancelCode);
    }

    /** 在派发监听器时先置为该值，避免用户在回调中主动删除自己时产生异常 */
    private static readonly ICancelTokenListener TOMBSTONE = new MockCompletion();

    private class MockCompletion : ICancelTokenListener
    {
        public void OnCancelRequested(ICancelToken cancelToken) {
        }
    }
}
}