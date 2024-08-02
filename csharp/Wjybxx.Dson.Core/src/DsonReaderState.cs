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
/// DsonReader的状态枚举
/// </summary>
public enum DsonReaderState
{
    /** 顶层上下文的初始状态 */
    Initial,

    /**
     * 已确定是一个Array/Object，等待用户调用readStartXXX方法
     * 正常情况下不会出现该状态，Object/Array由于存在Header，我们通常需要读取header之后才能正确解码，
     * 我们需要能在读取header之后重新恢复到需要调用readStartXXX的方法，才不会影响业务代码，也避免数据流回退
     *
     * 简单说，用于peek一部分数据之后，重新设置为等待readStartXXX状态，避免数据流的回滚
     */
    WaitStartObject,
    /** 等待读取类型 */
    Type,
    /** 等待用户读取name(fullNumber) */
    Name,
    /** 等待读取value */
    Value,
    /** 当前对象读取完毕；等待用户调用writeEndXXX */
    WaitEndObject,

    /** 到达输入的尾部 */
    EndOfFile,
}
}