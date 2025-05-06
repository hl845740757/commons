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

using NUnit.Framework;
using Wjybxx.Commons;

namespace Commons.Tests.Apt;

/// <summary>
/// 
/// </summary>
public class TypeInfoTest2
{
    /// <summary>
    /// 测试静态类的底层
    /// </summary>
    [Test]
    public void TestStaticModifiers() {
        // 静态类是抽象密封类
        var type = typeof(MathCommon);
        Assert.IsTrue(type.IsAbstract);
        Assert.IsTrue(type.IsSealed);
    }
    
    [Test]
    public void TestMetadataName() {
        Type unboundedType = typeof(Dictionary<,>);
        Console.WriteLine(unboundedType.Name);
        Assert.IsTrue(unboundedType.Name.EndsWith("`2"));
    }
}