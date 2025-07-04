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

using Wjybxx.Commons.Fx;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 事件循环的模块，亦即EventLoop的组件
/// 1.只有为<see cref="ComponentKind.Script"/>类型时才会被事件循环特殊调度，
/// 否则只调用<see cref="IComponent.OnReady"/>、<see cref="IComponent.OnDestroy"/>和<see cref="ResolveDependence"/>方法。
/// 2.执行顺序为
/// <see cref="IComponent.OnReady"/>、<see cref="ResolveDependence"/>
/// <see cref="Start"/>、
/// <see cref="EarlyUpdate"/><see cref="Update"/><see cref="LateUpdate"/>
/// <see cref="Stop"/>、
/// <see cref="IComponent.OnDestroy"/>。
/// 3.如果支持<see cref="IAgentEvent"/>，<see cref="IAgentEventHandler{T}"/>
///
/// 注意：这里的Update和游戏业务中的Update概念并不相同，游戏World中的FixedUpdate、Update、LateUpdate应当在Update场景的时候自行封装；
/// 但服务器通常可以直接使用这三个方法...
/// </summary>
public interface IEventLoopModule : IComponent
{
    /** 事件循环的全局组件id池 */
    public static readonly ComponentIdPool GLOBAL = ComponentIdPool.NewPool();
#nullable disable

    /** 修正返回值类型 */
    new IEventLoop Entity { get; }

    /// <summary>
    /// 处理依赖问题
    /// 事件循环会在启动所有的模块之前调用该方法，此时所有的模块已执行<see cref="IComponent.OnReady"/>
    /// </summary>
    void ResolveDependence() {
    }

    /// <summary>
    /// worker会在启动时执行所有模块的start方法
    /// </summary>
    void Start() {
    }

    /// <summary>
    /// 该方法在所有Module的<see cref="Update"/>之前调用。
    ///
    /// Worker每帧会调用调用所有模块的EarlyUpdate方法
    /// 注意：只有重写了该方法的类才会被每帧调用。
    /// </summary>
    void EarlyUpdate() {
    }

    /// <summary>
    /// Worker每帧会调用调用所有模块的Update方法
    /// 注意：只有重写了该方法的类才会被每帧调用。
    /// </summary>
    void Update() {
    }

    /// <summary>
    /// 该方法在所有Module的<see cref="Update"/>之后调用。
    /// 
    /// Worker每帧会调用调用所有模块的LateUpdate方法
    /// 注意：只有重写了该方法的类才会被每帧调用。
    /// </summary>
    void LateUpdate() {
    }

    /// <summary>
    /// Worker在停止时会调用所有模块的Stop方法
    /// 注意：默认按照启动顺序的逆顺序停止。
    /// </summary>
    void Stop() {
    }

    #region 接口适配

    IEntity IComponent.Entity => Entity;

    #endregion
}
}