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
/// 行为树模块使用的取消令牌
/// 1.行为树模块需要的功能不多，且需要进行一些特殊的优化，因此去除对Concurrent模块的依赖。
/// 2.关于取消码的设计，可查看<see cref="CancelCodes"/>类。
/// 3.继承<see cref="ICancelTokenListener"/>是为了方便通知子Token。
/// 4.在行为树模块，Task在运行期间最多只应该添加一次监听。
/// 5.Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
/// 
/// </summary>
public class CancelToken : ICancelTokenListener
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

    /// <summary>
    /// 创建一个同类型实例(默认只拷贝环境数据)
    /// </summary>
    /// <param name="copyCode">是否拷贝当前取消码</param>
    public virtual CancelToken NewInstance(bool copyCode = false) {
        return new CancelToken(copyCode ? code : 0);
    }

    /// <summary>
    /// 重置状态(行为树模块取消令牌需要复用)
    /// </summary>
    public virtual void Reset() {
        reentryId++;
        code = 0;
        listeners.Clear();
        firing = false;
    }

    protected bool IsFiring => firing;
    protected int ReentryId => reentryId;

    #region query

    /// <summary>
    /// 取消码
    /// 1.按bit位存储信息，包括是否请求中断，是否超时，紧急程度等
    /// 2.低20位为取消原因；高12位为特殊信息 <see cref="CancelCodes.MASK_REASON"/>
    /// 3.不为0表示已发起取消请求
    /// 4.取消时至少赋值一个信息，reason通常应该赋值
    /// </summary>
    /// <value></value>
    public int CancelCode => code;

    /// <summary>
    /// 是否已收到取消信号
    /// 任务的执行者将持有该令牌，在调度任务前会检测取消信号；如果任务已经开始，则由用户的任务自身检测取消和中断信号。
    /// </summary>
    public bool IsCancelling => code != 0;

    /// <summary>
    /// 取消的原因
    /// </summary>
    public int Reason => CancelCodes.GetReason(code);

    /// <summary>
    /// 取消的紧急程度
    /// </summary>
    public int Degree => CancelCodes.GetDegree(code);

    #endregion

    #region cancel

    /// <summary>
    /// 将Token置为取消状态
    /// </summary>
    /// <param name="cancelCode">取消码；reason部分需大于0；辅助类{@link CancelCodeBuilder}</param>
    /// <exception cref="ArgumentException">如果code小于等于0；或reason部分为0</exception>
    /// <returns>是否成功已给定取消码进入取消状态</returns>
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
        List<ICancelTokenListener> listeners = cancelToken.listeners;
        if (listeners.Count == 0) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        cancelToken.firing = true;
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
        listeners.Clear();
        cancelToken.firing = false;
    }

    #endregion

    #region 监听器

    /// <summary>
    /// 添加监听器
    /// </summary>
    public void AddListener(ICancelTokenListener listener) {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        if (listener == this) throw new ArgumentException("add self");
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

    /// <summary>
    /// 删除监听器
    /// 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
    /// </summary>
    /// <param name="listener">要删除的监听器</param>
    /// <param name="firstOccurrence">是否强制正向查找删除</param>
    /// <returns>存在匹配的监听器则返回true</returns>
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

    /// <summary>
    /// 查询是否存在给定的监听器
    /// </summary>
    /// <param name="listener">要查询的监听器</param>
    /// <returns>如果存在则返回true，否则返回false</returns>
    public bool HasListener(ICancelTokenListener listener) {
        return listeners.LastIndexOfRef(listener) >= 0;
    }

    #endregion

    /// <summary>
    /// 收到其它地方的取消信号
    /// </summary>
    /// <param name="parent"></param>
    void ICancelTokenListener.OnCancelRequested(CancelToken parent) {
        Cancel(parent.CancelCode);
    }

    /** 在派发监听器时先置为该值，避免用户在回调中主动删除自己时产生异常 */
    private static readonly ICancelTokenListener TOMBSTONE = new MockCompletion();

    private class MockCompletion : ICancelTokenListener
    {
        public void OnCancelRequested(CancelToken cancelToken) {
        }
    }
}
}