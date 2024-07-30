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

namespace Wjybxx.Commons.IO
{
/// <summary>
/// 字节序
/// </summary>
public enum ByteOrder : byte
{
    /// <summary>
    /// 小端（C#默认是小端）
    /// </summary>
    LittleEndian = 0,

    /// <summary>
    /// 大端（网络字节序）
    /// </summary>
    BigEndian = 1,
}
}