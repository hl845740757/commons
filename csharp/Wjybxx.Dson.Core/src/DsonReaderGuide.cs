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
/// 读操作指导
/// </summary>
public enum DsonReaderGuide
{
    /** 当前应该读取type */
    ReadType,
    /** 当前应该读取name或fullNumber */
    ReadName,
    /** 当前应该根据type决定应该怎样读值 */
    ReadValue,

    /** 当前应该读数组 */
    StartArray,
    /** 当前应该结束读数组 */
    EndArray,

    /** 当前应该读Object */
    StartObject,
    /** 当前应该结束读Object */
    EndObject,

    /** 当前应该读Object */
    StartHeader,
    /** 当前应该结束读Object */
    EndHeader,

    /** 当前应该关闭Reader */
    Close,
}
}