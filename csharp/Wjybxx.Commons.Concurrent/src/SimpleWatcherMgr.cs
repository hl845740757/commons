#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
#pragma warning disable CS0169

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 一个简单的Watcher管理器
/// 由于多用在多线程环境下，因此提供了缓存行填充特性
/// (泛型类不能显式定于内存布局)
/// </summary>
/// <typeparam name="E"></typeparam>
public sealed class SimpleWatcherMgr<E> : WatcherMgr<E>
{
    // 填充
    private long p1, p2, p3, p4, p5, p6, p7, p8;

    /** 常见方案：synchronized写，volatile读 */
    private volatile Watcher<E>? _watcher;

    // 填充
    private long p11, p12, p13, p14, p15, p16, p17, p18;

    public void Watch(Watcher<E> watcher) {
        if (watcher == null) throw new ArgumentNullException(nameof(watcher));
        lock (this) {
            this._watcher = watcher;
        }
    }

    public bool CancelWatch(Watcher<E>? watcher) {
        lock (this) {
            if (watcher != null && watcher == this._watcher) {
                this._watcher = null;
                return true;
            }
            return false;
        }
    }

    public bool OnEvent(in E evt) {
        Watcher<E> watcher = this._watcher;
        if (watcher == null) {
            return false;
        }
        // 取消成功才处理事件，考虑竞争的情况
        // 取消失败，证明当前监听器失效；但取消成功，不能证明当前监听器有效！目标线程可能已醒来，正准备取消监听器
        bool r = false;
        try {
            if (watcher.Test(evt) && CancelWatch(watcher)) {
                r = true;
                watcher.OnEvent(evt);
            }
        }
        catch (Exception ex) {
            ThreadUtil.RecoveryInterrupted(ex);
            if (!r) {
                FutureLogger.LogCause(ex, "Fatal Error! watcher.test caught exception");
            } else {
                FutureLogger.LogCause(ex, "watcher.onEvent caught exception");
            }
        }
        return r;
    }
}
}