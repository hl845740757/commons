﻿#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// EventLoop选择器
/// </summary>
public interface IEventLoopChooser
{
    /**
     * 按默认规则分配一个{@link EventLoop}
     */
    IEventLoop Select();

    /**
     * 通过给定键选择一个{@link EventLoop}
     *
     * @apiNote 同一个key的选择结果必须是相同的
     */
    IEventLoop Select(int key);
}
}