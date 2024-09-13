#region LICENSE

// // Copyright 2024 wjybxx(845740757@qq.com)
// //
// // Licensed under the Apache License, Version 2.0 (the "License");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

#endregion

using System;
using Wjybxx.BTree.Branch;

namespace Wjybxx.BTree
{
/// <summary>
/// 该注解用于表示一个task是可以被内联的，
/// 1.适用于<see cref="SingleRunningChildBranch{T}"/>和<see cref="Decorator{T}"/>
/// 2.该注解不继承，必须在类上显式定义才可以生效。
/// 3.不能被内联的节点应当处理子节点的内联，避免内联中断。
/// 
/// <h3>内联条件</h3>
/// 一个控制是否可被内联，需要满足以下条件：
/// 1.最多只能有一个运行中的子节点，且不可以有运行中的钩子节点。
/// 2.在子节点完成以前，当前节点没有额外的行为 -- 即心跳逻辑只是简单驱动子节点运行。
/// 3.不能有特殊的事件处理逻辑，一定是直接派发给子节点。
/// 4.当前节点收到取消信号时，子节点也一定收到取消信号。
/// 
/// Q：为什么使用注解，而不是虚方法？
/// A：强调是否可内联是类型的信息，与实例状态无关 -- 更安全。 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TaskInlinableAttribute : Attribute
{
}
}