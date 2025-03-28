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
    /**
     * 刚刚创建，尚未添加到实体
     */
    New = 0,
    /**
     * 已添加到实体，等待启动；非脚本组件会直接进入{@link #STOPPED}的状态
     */
    Ready = 1,
    /**
     * 正在启动中，脚本组件在调用Start方法前进入该状态
     */
    Starting = 2,
    /**
     * 运行状态，脚本组件在调用Start成功后会进入该状态。
     */
    Running = 3,
    /**
     * 停止中，脚本组件在调用Stop方法前进入该状态
     */
    Stopping = 4,
    /**
     * 成功停止，非脚本组件会直接到该状态，脚本组件在调用Stop方法后会进入该状态
     */
    Stopped = 5,
    /**
     * 已从实体上删除
     */
    Destroyed = 6
}
}