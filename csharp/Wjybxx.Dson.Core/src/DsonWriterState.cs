#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Dson
{
/// <summary>
/// Object、Header上下文循环： [NAME-VALUE],[NAME-VALUE]...
/// Array上下文循环： VALUE...
/// 顶层上下文循环：INITIAL,VALUE,VALUE...
/// </summary>
public enum DsonWriterState
{
    /** 顶层上下文初始状态 */
    Initial,

    /** 等待写入name(fullNumber) */
    Name,

    /** 等待写入Value */
    Value,
}
}