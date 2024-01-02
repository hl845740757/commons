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
/// 有界双端队列溢出策略
/// (开放给用户的策略)
/// </summary>
public enum DequeOverflowBehavior : byte
{
    /// <summary>
    /// 抛出异常
    /// </summary>
    ThrowException = 0,

    /// <summary>
    /// 丢弃首部 -- 当尾部插入元素时，允许覆盖首部；首部插入时抛出异常。
    /// eg：undo队列
    /// </summary>
    DiscardHead = 1,

    /// <summary>
    /// 丢弃尾部 -- 当首部插入元素时，允许覆盖尾部；尾部插入时抛出异常。
    /// eg: redo队列
    /// </summary>
    DiscardTail = 2,

    /// <summary>
    /// 环形缓冲 -- 首部插入时覆盖尾部；尾部插入时覆盖首部。
    /// </summary>
    CircleBuffer = 3,
}