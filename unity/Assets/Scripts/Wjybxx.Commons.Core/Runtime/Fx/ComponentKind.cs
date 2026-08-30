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
/// 组件类型
/// </summary>
public enum ComponentKind
{
    /** 数据组件 */
    Data = 0,

    /** 普通行为组件；即使有可以被框架调度的方法，也不会被调度 */
    Behavior = 1,

    /** 脚本行为组件；需要被框架调度的组件；具体接口取决于具体业务 */
    Script = 2
}
}