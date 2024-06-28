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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Commons.Tests.Apt;

public class TypeInfoTest
{
    // 反射方法名：op_GreaterThan
    public static bool operator >(TypeInfoTest a, TypeInfoTest b) {
        return false;
    }

    public static bool operator <(TypeInfoTest a, TypeInfoTest b) {
        return false;
    }

    public static bool operator <=(TypeInfoTest a, TypeInfoTest b) {
        return false;
    }

    public static bool operator >=(TypeInfoTest a, TypeInfoTest b) {
        return false;
    }

    public static bool operator ==(TypeInfoTest a, TypeInfoTest b) {
        return false;
    }

    public static bool operator !=(TypeInfoTest a, TypeInfoTest b) {
        return false;
    }

    /// <summary>
    /// Boolean op_GreaterThan(Commons.Tests.Core.TypeInfoTest, Commons.Tests.Core.TypeInfoTest)
    /// Boolean op_LessThan(Commons.Tests.Core.TypeInfoTest, Commons.Tests.Core.TypeInfoTest)
    /// Boolean op_LessThanOrEqual(Commons.Tests.Core.TypeInfoTest, Commons.Tests.Core.TypeInfoTest)
    /// Boolean op_GreaterThanOrEqual(Commons.Tests.Core.TypeInfoTest, Commons.Tests.Core.TypeInfoTest)
    /// Boolean op_Equality(Commons.Tests.Core.TypeInfoTest, Commons.Tests.Core.TypeInfoTest)
    /// Boolean op_Inequality(Commons.Tests.Core.TypeInfoTest, Commons.Tests.Core.TypeInfoTest)
    /// </summary>
    [Test]
    public void TestOperator() {
        Type type = typeof(TypeInfoTest);
        List<MethodInfo> methodInfos = type.GetMethods()
            .Where(e => e.Name.StartsWith("op_"))
            .ToList();
        foreach (MethodInfo methodInfo in methodInfos) {
            Console.WriteLine(methodInfo);
        }
    }
}