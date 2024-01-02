#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 如果要插入的元素已存在，要执行的行为枚举
/// (insert一定不覆盖旧值)
/// </summary>
internal enum InsertionBehavior : byte
{
    /// <summary>
    /// 什么也不做（放弃）
    /// </summary>
    None = 0,

    /// <summary>
    /// 抛出异常
    /// </summary>
    ThrowOnExisting = 1
}