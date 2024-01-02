#region LICENSE

//  Copyright 2023 wjybxx
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 字典Put操作对应的行为
/// (put一定覆盖旧值)
/// </summary>
internal enum PutBehavior : byte
{
    /// <summary>
    /// 无特殊逻辑
    /// </summary>
    None = 0,

    /// <summary>
    /// 移动至末尾
    /// </summary>
    MoveToLast = 1,

    /// <summary>
    /// 覆盖元素并移动至首部
    /// </summary>
    MoveToFirst = 2,
}