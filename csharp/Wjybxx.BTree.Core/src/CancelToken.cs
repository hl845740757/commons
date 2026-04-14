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
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Pool;

namespace Wjybxx.BTree
{
public interface ICancelTokenListener
{
#nullable disable
    /// <summary>
    /// 该方法在取消令牌收到取消信号时执行
    /// 
    /// </summary>
    /// <param name="cancelToken">收到取消信号的令牌</param>
    /// <param name="ctx">回调上下文</param>
    void OnCancelRequested(CancelToken cancelToken, object ctx);
#nullable restore
}

/// <summary>
/// 行为树模块使用的取消令牌
///
/// 1.行为树模块需要的功能不多，且需要进行一些特殊的优化，因此去除对Concurrent模块的依赖。
/// 2.关于取消码的设计，可查看<see cref="CancelCodes"/>类。
/// 3.继承<see cref="ICancelTokenListener"/>是为了方便通知子Token。
/// 4.在行为树模块，Task在运行期间最多只应该添加一次监听。
/// 5.Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
/// </summary>
[NotThreadSafe]
public class CancelToken : ICancelTokenListener
{
    /** 取消码 -- 0表示未收到信号，大于0表示收到取消信号 */
    private int code;
    /** 用于检测复用 */
    private int reentryId;
    /** 监听器列表 */
    private readonly List<CallbackInfo> callbacks = new();

    public CancelToken() {
    }

    public CancelToken(int code) {
        if (code != 0) {
            CancelCodes.CheckCode(code);
        }
        this.code = code;
    }

    void ICancelTokenListener.OnCancelRequested(CancelToken cancelToken, object ctx) {
        Cancel(cancelToken.CancelCode);
    }

    /// <summary>
    /// 创建一个同类型实例(默认只拷贝环境数据)
    /// </summary>
    /// <param name="copyCode">是否拷贝当前取消码</param>
    public virtual CancelToken NewInstance(bool copyCode = false) {
        return new CancelToken(copyCode ? code : 0);
    }

    /// <summary>
    /// 重置状态(行为树模块取消令牌需要复用)
    /// 注意：该方法会静默删除监听器，可能导致监听器丢失信号。
    /// </summary>
    public virtual void Reset() {
        reentryId++;
        code = 0;
        callbacks.Clear();
    }

    /// <summary>
    /// 重入id，允许外部捕获
    /// </summary>
    public int ReentryId => reentryId;

    #region query

    /// <summary>
    /// 是否支持取消
    /// </summary>
    public bool CanBeCancelled => true;

    /// <summary>
    /// 取消码
    /// </summary>
    public int CancelCode {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => code;
    }

    /// <summary>
    /// 当前是否收到了取消信号
    /// </summary>
    public bool IsRequested {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => code != 0;
    }

    /// <summary>
    /// 取消任务的原因
    /// </summary>
    public int Reason => CancelCodes.GetReason(code);

    /// <summary>
    /// 取消的紧急程度
    /// </summary>
    public int Degree => CancelCodes.GetDegree(code);

    /// <summary>
    /// 检查当前是否收到了取消信号
    /// </summary>
    public void CheckCancel() {
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    #endregion

    #region cancel

    public bool Cancel(int cancelCode = CancelCodes.REASON_DEFAULT) {
        CancelCodes.CheckCode(cancelCode);
        int r = this.code;
        if (r == 0) {
            this.code = cancelCode;
            PostComplete(this);
            return true;
        }
        return false;
    }

    private static void PostComplete(CancelToken cancelToken) {
        List<CallbackInfo> callbacks = cancelToken.callbacks;
        if (callbacks.Count == 0) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        for (int idx = 0, len = callbacks.Count; idx < len; idx++) {
            var callbackInfo = callbacks[idx];
            if (callbackInfo.action == null) {
                continue;
            }
            callbacks[idx] = default;
            try {
                Invoke(cancelToken, callbackInfo);
            }
            catch (Exception e) {
                TaskLogger.Info(e, "listener caught exception");
            }
            if (reentryId != cancelToken.reentryId) {
                return; // 在通知期间被Reset
            }
        }
        callbacks.Clear();
    }

    private static void Invoke(CancelToken cancelToken, CallbackInfo callbackInfo) {
        switch (callbackInfo.action) {
            case ICancelTokenListener listener:
                listener.OnCancelRequested(cancelToken, callbackInfo.state);
                break;
            case Action<CancelToken, object> action2:
                action2(cancelToken, callbackInfo.state);
                break;
            default: {
                Action<CancelToken> action1 = (Action<CancelToken>)callbackInfo.action;
                action1(cancelToken);
                break;
            }
        }
    }

    #endregion

    #region 回调

#nullable disable
    /// <summary>
    /// 添加监听器
    /// </summary>
    public void RegisterCallback(ICancelTokenListener listener, object? state = null) {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        if (listener == this) throw new ArgumentException("add self");
        if (code != 0) {
            try {
                listener.OnCancelRequested(this, state);
            }
            catch (Exception e) {
                TaskLogger.Info(e, "listener caught exception");
            }
        } else {
            callbacks.Add(new CallbackInfo(listener, state));
        }
    }

    /// <summary>
    /// 添加回调
    /// </summary>
    /// <param name="callback">回调</param>
    /// <param name="state">回调上下文</param>
    public void RegisterCallback(Action<CancelToken, object> callback, object? state = null) {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        if (code != 0) {
            try {
                callback(this, state);
            }
            catch (Exception e) {
                TaskLogger.Info(e, "listener caught exception");
            }
        } else {
            callbacks.Add(new CallbackInfo(callback, state));
        }
    }

    /// <summary>
    /// 添加回调
    /// </summary>
    public void RegisterCallback(Action<CancelToken> callback) {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        if (code != 0) {
            try {
                callback(this);
            }
            catch (Exception e) {
                TaskLogger.Info(e, "listener caught exception");
            }
        } else {
            callbacks.Add(new CallbackInfo(callback, null));
        }
    }

    /// <summary>
    /// 删除监听器
    /// 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
    /// </summary>
    /// <param name="callback">要删除的回调</param>
    /// <param name="firstOccurrence">是否强制正向查找删除</param>
    /// <returns>存在匹配的监听器则返回true</returns>
    public bool UnregisterCallback(object callback, bool firstOccurrence = false) {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        int index = IndexOfCallback(callback, firstOccurrence);
        if (index < 0) {
            return false;
        }
        if (code != 0) { // 正在通知
            callbacks[index] = default;
        } else {
            callbacks.RemoveAt(index);
        }
        return true;
    }

    private int IndexOfCallback(object action, bool firstOccurrence) {
        if (firstOccurrence) {
            for (int idx = 0; idx < callbacks.Count; idx++) {
                if (Equals(callbacks[idx].action, action)) return idx;
            }
        } else {
            for (int idx = callbacks.Count - 1; idx >= 0; idx--) {
                if (Equals(callbacks[idx].action, action)) return idx;
            }
        }
        return -1;
    }

    private readonly struct CallbackInfo
    {
        public readonly object action;
        public readonly object state;

        public CallbackInfo(object action, object state) {
            this.action = action;
            this.state = state;
        }
    }

    #endregion
}
}