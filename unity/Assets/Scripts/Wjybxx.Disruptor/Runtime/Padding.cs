#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using System.Runtime.InteropServices;

namespace Wjybxx.Disruptor
{
/// <summary>
/// 56位填充
/// 默认情况下，C#和C++编译器会将<see cref="LayoutKind.Sequential"/>布局值应用于结构。 对于类，必须显式应用<see cref="LayoutKind.Sequential"/>值.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal readonly struct Padding56
{
    [FieldOffset(48)]
    private readonly long _padding;
}

/// <summary>
/// 64位填充
/// 默认情况下，C#和C++编译器会将<see cref="LayoutKind.Sequential"/>布局值应用于结构。 对于类，必须显式应用<see cref="LayoutKind.Sequential"/>值.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal readonly struct Padding64
{
    [FieldOffset(56)]
    private readonly long _padding;
}
}