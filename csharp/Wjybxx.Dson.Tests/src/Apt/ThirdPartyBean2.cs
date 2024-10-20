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

using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Tests.Apt;

/// <summary>
/// 假设为一个外部类，测试<see cref="DsonCodecLinkerBeanAttribute"/>
/// </summary>
public class ThirdPartyBean2
{
    private int age;
    private string? name;

    public int Age {
        get => age;
        set => age = value;
    }
    public string? Name {
        get => name;
        set => name = value;
    }

    /// <summary>
    /// 测试自动属性
    /// </summary>
    public int Sex { get; set; }
}