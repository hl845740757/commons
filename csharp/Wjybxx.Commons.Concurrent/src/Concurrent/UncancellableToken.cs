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

/// <summary>
/// 不可取消的令牌
/// </summary>
sealed class UncancellableToken : ICancelToken
{
    public static readonly UncancellableToken Inst = new UncancellableToken();

    public ICancelToken AsReadonly => this;

    #region token

    public int CancelCode => 0;

    public bool IsCancelling() => false;

    public int Reason() => 0;

    public int Degree() => 0;

    public bool IsInterruptible() => false;

    public bool IsWithoutRemove => false;

    public void CheckCancel() {
    }

    #endregion

    #region 监听器

    public IRegistration ThenAccept(Action<ICancelToken> action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenAccept(Action<ICancelToken, object?> action, object? ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenAcceptAsync(IExecutor executor, Action<ICancelToken, object?> action, object? ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenRun(Action action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenRunAsync(IExecutor executor, Action action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenRun(Action<object?> action, object? ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenRunAsync(IExecutor executor, Action<object?> action, object? ctx, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenNotify(ICancelTokenListener action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenNotifyAsync(IExecutor executor, ICancelTokenListener action, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenTransferTo(ICancelTokenSource child, int options = 0) {
        return TOMBSTONE;
    }

    public IRegistration ThenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        return TOMBSTONE;
    }

    #endregion

    private static readonly IRegistration TOMBSTONE = new MockRegistration();

    private class MockRegistration : IRegistration
    {
        public void Dispose() {
        }
    }
}