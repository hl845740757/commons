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

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 绑定范围
/// </summary>
public enum InjectScope
{
    /// <summary>
    /// 单例类型
    /// 
    /// 注意：单例是<see cref="InjectBeanConfig"/>级别。
    /// </summary>
    Singleton = 0,

    /// <summary>
    /// 多例
    /// </summary>
    Prototype = 1,
}
}