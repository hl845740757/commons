#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BTree
{
/// <summary>
/// 行为树对外的选项
///
/// 通过静态导入引入到Task类，以避免重复定义
/// </summary>
public static class TaskAttributes
{
    // Ctl属性--运行时属性
    public const int MASK_SLOW_START = 1 << 24;
    public const int MASK_AUTO_RESET_CHILDREN = 1 << 25;
    public const int MASK_MANUAL_CHECK_CANCEL = 1 << 26;
    public const int MASK_AUTO_LISTEN_CANCEL = 1 << 27;
    public const int MASK_BREAK_INLINE = 1 << 28;
    // Flags属性--静态属性
    public const int MASK_INVERTED_GUARD = 1 << 29;
    public const int MASK_HOOK_TASK = 1 << 30;
    /** 高8位为流程控制特征值（对外开放）*/
    public const int MASK_CONTROL_FLOW_OPTIONS = (-1) << 24;
}
}