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

namespace Wjybxx.Commons.Fx
{
/// <summary>
/// 组件的状态
/// </summary>
public enum ComponentStatus
{
    /// <summary>
    /// 刚刚创建，尚未添加到实体
    /// </summary>
    New = 0,
    /// <summary>
    /// 已添加到实体，即已完成初始化
    /// </summary>
    Initialized = 1,

    /// <summary>
    /// 启动中（慎用）
    /// </summary>
    Starting = 2,
    /// <summary>
    /// 运行状态，脚本组件在调用Start成功后会进入该状态。
    /// </summary>
    Running = 3,
    /// <summary>
    /// 挂起状态，挂起状态下不会被Update
    /// (是否支持取决于实体对组件的调度策略)
    /// </summary>
    Suspended = 4,
    /// <summary>
    /// 停止中（慎用）
    /// </summary>
    Stopping = 5,
    /// <summary>
    /// 运行结束
    /// </summary>
    Stopped = 6,

    /// <summary>
    /// 已销毁，即已从实体上删除
    /// </summary>
    Destroyed = 7
}
}