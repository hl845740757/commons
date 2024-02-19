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

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 并发工具类
/// </summary>
public static class Executors
{
    public static ITask BoxAction(Action action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper1(action, options);
    }

    public static ITask BoxAction(Action action, CancellationToken cancelToken, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper2(action, cancelToken, options);
    }

    public static ITask BoxAction(Action action, ICancelToken cancelToken, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (cancelToken == null) throw new ArgumentNullException(nameof(cancelToken));
        return new ActionWrapper3(action, cancelToken, options);
    }

    public static ITask BoxAction(Action<IContext> action, IContext context, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        return new ActionWrapper4(action, context, options);
    }

    private class ActionWrapper1 : ITask
    {
        private readonly Action action;
        private readonly int options;

        public ActionWrapper1(Action action, int options) {
            this.action = action;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            action();
        }
    }

    private class ActionWrapper2 : ITask
    {
        private readonly Action action;
        private readonly CancellationToken cancelToken;
        private readonly int options;

        public ActionWrapper2(Action action, CancellationToken cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken.IsCancellationRequested) {
                return;
            }
            action();
        }
    }

    private class ActionWrapper3 : ITask
    {
        private readonly Action action;
        private readonly ICancelToken cancelToken;
        private readonly int options;

        public ActionWrapper3(Action action, ICancelToken cancelToken, int options) {
            this.action = action;
            this.cancelToken = cancelToken;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (cancelToken.IsCancelling()) {
                return;
            }
            action();
        }
    }

    private class ActionWrapper4 : ITask
    {
        private readonly Action<IContext> action;
        private readonly IContext context;
        private readonly int options;

        public ActionWrapper4(Action<IContext> action, IContext context, int options) {
            this.action = action;
            this.context = context;
            this.options = options;
        }

        public int Options => options;

        public void Run() {
            if (context.CancelToken.IsCancelling()) {
                return;
            }
            action(context);
        }
    }
}