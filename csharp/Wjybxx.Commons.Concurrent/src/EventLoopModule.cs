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
using System.Diagnostics;
using System.Reflection;
using Wjybxx.Commons.Fx;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 第三方程序集需要自定义调度时，可使用<see cref="EventLoopModuleUtil"/>中的方法调用
/// Unity下不继承MonoBehavior，因为我们要自己调度。
/// </summary>
public abstract class EventLoopModule : IEventLoopModule
{
#nullable disable
    private IEventLoop _eventLoop;
    private ComponentId _cid;
    private ComponentStatus _status = ComponentStatus.New;
#nullable restore

    #region internal

    /** 设置模块的状态，通过该接口可以实现自定义流程 */
    internal void SetStatus(ComponentStatus status) {
        this._status = status;
    }

    /** 设置EventLoop 会触发{@link #onAwake()}事件 */
    internal void SetEventLoop(IEventLoop eventLoop) {
        if (eventLoop == null) throw new ArgumentNullException(nameof(eventLoop));
        if (this._eventLoop != null) {
            throw new IllegalStateException("already bind");
        }
        this._eventLoop = eventLoop;
        this._status = ComponentStatus.Initialized;
        this.OnAwake();
    }

    internal void InvokeDestroy() {
        _status = ComponentStatus.Destroyed;
        try {
            OnDestroy();
        }
        finally {
            _eventLoop = null;
        }
    }

    internal void InvokeStart() {
        Debug.Assert(IsScript());
        _status = ComponentStatus.Running;
        Start();
    }

    internal void InvokeStop() {
        Debug.Assert(IsScript());
        _status = ComponentStatus.Shutdown;
        try {
            Stop();
        }
        finally {
            _status = ComponentStatus.Terminated;
        }
    }

    /** 是否是脚本组件 */
    private bool IsScript() {
        return Cid.kind == ComponentKind.Script;
    }

    #endregion

#nullable disable

    #region 默认实现

    public ComponentId Cid {
        get {
            if (_cid == null) {
                _cid = ParseCid() ?? throw new InvalidOperationException();
            }
            return _cid;
        }
        set {
            if (_status != ComponentStatus.New) {
                throw new IllegalStateException();
            }
            _cid = value;
        }
    }

    /** 解析组件id -- 允许重写方法，从另外的池解析组件id  */
    protected virtual ComponentId ParseCid() {
        return IEventLoopModule.GLOBAL.ValueOf(GetType());
    }

    public IEventLoop Entity => _eventLoop;
    public ComponentStatus Status => _status;

    #endregion

    #region 接口行为

    // 不定义访问不了...

    public virtual void OnAwake() {
    }

    public virtual void OnDestroy() {
    }

    public virtual void ResolveDependence() {
    }

    public virtual void Start() {
    }

    public virtual void EarlyUpdate() {
    }

    public virtual void Update() {
    }

    public virtual void LateUpdate() {
    }

    public virtual void Stop() {
    }

    #endregion
}
}