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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于创建事件循环选择器。
/// ps：实现为接口是不必要的。
/// </summary>
public class EventLoopChooserFactory
{
    /// <summary>
    /// 创建一个EventLoop选择器
    /// </summary>
    /// <param name="children">管理的子节点</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public virtual IEventLoopChooser NewChooser(IEventLoop[] children) {
        if (children.Length == 0) throw new ArgumentException("children empty");
        if (children.Length == 1) {
            return new SingleEventLoopChooser(children[0]);
        }
        if (IsPowerOfTwo(children.Length)) {
            return new PowerOfTwoEventLoopChooser(children);
        }
        return new RoundRobinEventLoopChooser(children);
    }

    private static bool IsPowerOfTwo(int x) {
        return x > 0 && (x & (x - 1)) == 0;
    }

    public class SingleEventLoopChooser : IEventLoopChooser
    {
        private readonly IEventLoop _eventLoop;

        public SingleEventLoopChooser(IEventLoop child) {
            this._eventLoop = child ?? throw new ArgumentNullException(nameof(child));
        }

        public IEventLoop Select() {
            return _eventLoop;
        }

        public IEventLoop Select(int key) {
            return _eventLoop;
        }
    }

    public class PowerOfTwoEventLoopChooser : IEventLoopChooser
    {
        private readonly IEventLoop[] _eventLoops;
        private uint idx = 0;

        public PowerOfTwoEventLoopChooser(IEventLoop[] children) {
            this._eventLoops = children ?? throw new ArgumentNullException(nameof(children));
        }

        private uint NextIndex() => Interlocked.Increment(ref idx) - 1;

        public IEventLoop Select() {
            uint key = NextIndex();
            return _eventLoops[key & (_eventLoops.Length - 1)];
        }

        public IEventLoop Select(int key) {
            return _eventLoops[key & (_eventLoops.Length - 1)];
        }
    }

    public class RoundRobinEventLoopChooser : IEventLoopChooser
    {
        private readonly IEventLoop[] _eventLoops;
        private uint idx = 0;

        public RoundRobinEventLoopChooser(IEventLoop[] children) {
            this._eventLoops = children ?? throw new ArgumentNullException(nameof(children));
        }

        private uint NextIndex() => Interlocked.Increment(ref idx) - 1;

        public IEventLoop Select() {
            uint key = NextIndex();
            return _eventLoops[key % _eventLoops.Length];
        }

        public IEventLoop Select(int key) {
            return _eventLoops[key % _eventLoops.Length];
        }
    }
}
}