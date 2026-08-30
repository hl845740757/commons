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
    /// 运行状态，脚本组件在调用Start成功后会进入该状态。
    /// </summary>
    Running = 2,
    /// <summary>
    /// 挂起状态，挂起状态下不会被Update
    /// (是否支持取决于实体对组件的调度策略)
    /// </summary>
    Suspended = 3,
    /// <summary>
    /// 关闭中，正在执行关闭前的清工作
    /// </summary>
    Shutdown = 4,
    /// <summary>
    /// 运行结束
    /// </summary>
    Terminated = 5,

    /// <summary>
    /// 已销毁，即已从实体上删除
    /// </summary>
    Destroyed = 6
}
}