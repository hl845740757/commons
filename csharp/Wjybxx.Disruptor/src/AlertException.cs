#region LICENSE

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

using System;

namespace Wjybxx.Disruptor
{
/// <summary>
/// 表示屏障关联的生产者或消费者收到了特殊信号 - 其作用类似于中断。
/// ps: 由于性能原因，该异常不会获取堆栈信息。
/// </summary>
public sealed class AlertException : Exception
{
    /// <summary>
    /// 全局单例
    /// </summary>
    public static AlertException Inst { get; } = new AlertException();

    private AlertException() {
    }

    public override string? StackTrace => null;
}
}