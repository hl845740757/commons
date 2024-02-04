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

namespace Wjybxx.Commons.Concurrent;

sealed class UncancellableToken : ICancelToken
{
    public static readonly UncancellableToken Inst = new UncancellableToken();

    public ICancelToken asReadonly() {
        return this;
    }

    #region token

    public int cancelCode() {
        return 0;
    }

    public bool isCancelling() {
        return false;
    }

    public int reason() {
        return 0;
    }

    public int degree() {
        return 0;
    }

    public bool isInterruptible() {
        return false;
    }

    public bool isWithoutRemove() {
        return false;
    }

    public void checkCancel() {
    }

    #endregion

    public IRegistration thenAccept(Action<ICancelToken> action, int options = 0) {
        throw new NotImplementedException();
    }

    public IRegistration thenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenAccept(Action<IContext, ICancelToken> action, IContext ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenAcceptAsync(IExecutor executor, Action<IContext, ICancelToken> action, IContext ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenRun(Action action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenRunAsync(IExecutor executor, Action action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenRun(Action<IContext> action, IContext ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenRunAsync(IExecutor executor, Action<IContext> action, IContext ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenNotify(CancelTokenListener action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenNotifyAsync(IExecutor executor, CancelTokenListener action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenTransferTo(ICancelTokenSource child, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration thenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        return TOMBSTONE;
    }

    private static readonly IRegistration TOMBSTONE = new MockRegistration();

    private class MockRegistration : IRegistration
    {
        public void Dispose() {
        }
    }
}