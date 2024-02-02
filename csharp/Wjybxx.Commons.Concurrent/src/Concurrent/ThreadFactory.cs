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

using System.Threading;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 线程工厂
/// </summary>
public interface ThreadFactory
{
    /// <summary>
    /// 创建一个新的线程
    /// </summary>
    /// <param name="loop">线程循环逻辑</param>
    /// <returns></returns>
    public Thread NewThread(ThreadStart loop);
}

/// <summary>
/// 默认的线程工厂实现
/// 线程名字： Worker-1:MyThread-1
/// </summary>
public class DefaultThreadFactory : ThreadFactory
{
    /** 线程命名前缀 */
    private readonly string _prefix;
    /** 是否是后台线程 */
    private readonly bool _isBackground;
    /** 线程优先级 */
    private readonly ThreadPriority _priority;

    /** 工厂内线程id分配器 -- CAS更新 */
    private long _idSequencer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefix">线程命名前缀</param>
    /// <param name="isBackground">是否是后台线程</param>
    /// <param name="priority">线程优先级</param>
    public DefaultThreadFactory(string prefix, bool isBackground = false, ThreadPriority priority = ThreadPriority.Normal) {
        _prefix = $"Pool-{NextPoolId}:{prefix}-";
        _isBackground = isBackground;
        _priority = priority;
    }

    public Thread NewThread(ThreadStart loop) {
        Thread thread = new Thread(loop);
        thread.Name = $"{_prefix}{Interlocked.Increment(ref _idSequencer)}";

        if (_isBackground) {
            thread.IsBackground = true;
        }
        if (_priority != ThreadPriority.Normal) {
            thread.Priority = _priority;
        }
        return thread;
    }

    /** 工厂id分配器 */
    private static long _poolIdSequencer;
    private static long NextPoolId => Interlocked.Increment(ref _poolIdSequencer);
}