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

    public int CancelCode => cancelToken.CancelCode;

    public bool IsCancelling() => cancelToken.IsCancelling();

    public int Reason() => cancelToken.Reason();

    public int Degree() => cancelToken.Degree();

    public bool IsInterruptible() => cancelToken.IsInterruptible();

    public bool IsWithoutRemove() {
        return cancelToken.IsWithoutRemove();
    }

    public void CheckCancel() {
        cancelToken.CheckCancel();
    }

    public IRegistration thenAccept(Action<ICancelToken> action, int options = 0) {
        return cancelToken.thenAccept(action, options);
    }

    public IRegistration thenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        return cancelToken.thenAcceptAsync(executor, action, options);
    }

    public IRegistration thenAccept(Action<ICancelToken, object> action, object? state, int options = 0) {
        return cancelToken.thenAccept(action, state, options);
    }

    public IRegistration thenAcceptAsync(IExecutor executor, Action<ICancelToken, object> action, object? state, int options = 0) {
        return cancelToken.thenAcceptAsync(executor, action, state, options);
    }

    public IRegistration thenRun(Action action, int options = 0) {
        return cancelToken.thenRun(action, options);
    }

    public IRegistration thenRunAsync(IExecutor executor, Action action, int options = 0) {
        return cancelToken.thenRunAsync(executor, action, options);
    }

    public IRegistration thenRun(Action<object> action, object? state, int options = 0) {
        return cancelToken.thenRun(action, state, options);
    }

    public IRegistration thenRunAsync(IExecutor executor, Action<object> action, object? state, int options = 0) {
        return cancelToken.thenRunAsync(executor, action, state, options);
    }

    public IRegistration thenNotify(CancelTokenListener action, int options = 0) {
        return cancelToken.thenNotify(action, options);
    }

    public IRegistration thenNotifyAsync(IExecutor executor, CancelTokenListener action, int options = 0) {
        return cancelToken.thenNotifyAsync(executor, action, options);
    }

    public IRegistration thenTransferTo(ICancelTokenSource child, int options = 0) {
        return cancelToken.thenTransferTo(child, options);
    }

    public IRegistration thenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        return cancelToken.thenTransferToAsync(executor, child, options);
    }
}