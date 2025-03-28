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
/// 事件循环的模块
/// 该接口在最抽象层仅仅作为标记接口
/// </summary>
public interface IEventLoopModule : IComponent
{
    /** 修正返回值类型 */
    new IEventLoop Entity { get; }

    /**
    * worker会在启动时执行所有模块的start方法
    * 注意：不要假设start方法的执行时机
    */
    void Start() {
    }

    /**
     * Worker每帧会调用调用所有模块的Update方法
     * 注意：只有重写了该方法的类才会被每帧调用。
     */
    void Update() {
    }

    /**
     * Worker每帧会调用调用所有模块的LateUpdate方法
     * 注意：只有重写了该方法的类才会被每帧调用。
     */
    void LateUpdate() {
    }

    /**
     * Worker在停止时会调用所有模块的Stop方法，
     * 注意：默认按照启动顺序的逆顺序停止。
     */
    void Stop() {
    }

    #region 接口适配

    IEntity IComponent.Entity => Entity;

    #endregion
}
}