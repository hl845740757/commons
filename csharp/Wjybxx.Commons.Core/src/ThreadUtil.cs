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
using System.Threading;

namespace Wjybxx.Commons;

/// <summary>
/// 线程工具类
/// </summary>
public static class ThreadUtil
{
    /** 如果是中断异常，则恢复线程中断状态，否则不产生效用 */
    public static void RecoveryInterrupted(Exception t) {
        if (t is ThreadInterruptedException) {
            Thread.CurrentThread.Interrupt();
        }
    }

    /** 检查线程中断状态 -- 如果线程被中断，则抛出中断异常。 */
    public static void CheckInterrupted() {
        // c# 居然不支持查询线程的中断信号...
    }

    /** 清除线程中断状态 */
    public static bool ClearInterrupt() {
        Thread currentThread = Thread.CurrentThread;
        try {
            Thread.Sleep(0);
            return false;
        }
        catch (ThreadInterruptedException) {
            return true;
        }
    }
}