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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 隐藏tokenSource的接口
/// </summary>
public sealed class ReadonlyCancelToken : ICancelToken
{
    private readonly ICancelToken cancelToken;

    public ReadonlyCancelToken(ICancelToken cancelToken) {
        this.cancelToken = cancelToken ?? throw new ArgumentNullException(nameof(cancelToken));
    }

    public ICancelToken AsReadonly() {
        return this;
    }

    public bool CanBeCancelled => cancelToken.CanBeCancelled;

    public int CancelCode => cancelToken.CancelCode;

    public bool IsCancelling => cancelToken.IsCancelling;

    public int Reason => cancelToken.Reason;

    public int Degree => cancelToken.Degree;

    public bool IsInterruptible => cancelToken.IsInterruptible;

    public bool IsWithoutRemove => cancelToken.IsWithoutRemove;

    public void CheckCancel() {
        cancelToken.CheckCancel();
    }

    public IRegistration ThenAccept(Action<ICancelToken> action, int options = 0) {
        return cancelToken.ThenAccept(action, options);
    }

    public IRegistration ThenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        return cancelToken.ThenAcceptAsync(executor, action, options);
    }

    public IRegistration ThenAccept(Action<ICancelToken, object> action, object? state, int options = 0) {
        return cancelToken.ThenAccept(action, state, options);
    }

    public IRegistration ThenAcceptAsync(IExecutor executor, Action<ICancelToken, object> action, object? state, int options = 0) {
        return cancelToken.ThenAcceptAsync(executor, action, state, options);
    }

    public IRegistration ThenRun(Action action, int options = 0) {
        return cancelToken.ThenRun(action, options);
    }

    public IRegistration ThenRunAsync(IExecutor executor, Action action, int options = 0) {
        return cancelToken.ThenRunAsync(executor, action, options);
    }

    public IRegistration ThenRun(Action<object> action, object? state, int options = 0) {
        return cancelToken.ThenRun(action, state, options);
    }

    public IRegistration ThenRunAsync(IExecutor executor, Action<object> action, object? state, int options = 0) {
        return cancelToken.ThenRunAsync(executor, action, state, options);
    }

    public IRegistration ThenNotify(ICancelTokenListener action, int options = 0) {
        return cancelToken.ThenNotify(action, options);
    }

    public IRegistration ThenNotifyAsync(IExecutor executor, ICancelTokenListener action, int options = 0) {
        return cancelToken.ThenNotifyAsync(executor, action, options);
    }

    public IRegistration ThenTransferTo(ICancelTokenSource child, int options = 0) {
        return cancelToken.ThenTransferTo(child, options);
    }

    public IRegistration ThenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        return cancelToken.ThenTransferToAsync(executor, child, options);
    }
}