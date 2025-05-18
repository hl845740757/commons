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

using System;

namespace Wjybxx.Dson.Codec.Attributes
{
/// <summary>
/// 该注解用于告知扫描器扫描codec实现时进行忽略
/// 这使得生成的类不自动实例化，因此你可以自行实例化，以对其进行封装等。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DsonCodecScanIgnoreAttribute : Attribute
{
    
}
}