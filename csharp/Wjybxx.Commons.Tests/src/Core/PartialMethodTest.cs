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

namespace Commons.Tests.Core;

/// <summary>
/// 测试分部方法的规则
///
/// 显式声明访问权限后，必须提供实现。
/// </summary>
public partial class PartialMethodTest
{
    partial void AfterReload();
}

public partial class PartialMethodTest
{
    partial void AfterReload() {
        
    }
}