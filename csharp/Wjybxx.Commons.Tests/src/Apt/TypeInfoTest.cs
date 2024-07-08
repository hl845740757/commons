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
using System.Threading.Tasks;
using NUnit.Framework;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Poet;

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
    ///
    /// IsSpecialName = true
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

    [Test]
    public void TestAsyncMethod() {
        Type type = typeof(TypeInfoTest);
        MethodInfo methodInfo = type.GetMethod("GetValueAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodSpec.Builder builder = MethodSpec.Overriding(methodInfo);

        Assert.True((builder.modifiers & Modifiers.Async) != 0, "async");
        Assert.True((builder.varargs), "varargs");
    }

    [Test]
    public void TestUnsafeMethod() {
        Type type = typeof(TypeInfoTest);
        MethodInfo methodInfo = type.GetMethod("GetUnsafeValue", BindingFlags.NonPublic | BindingFlags.Static);
        Console.WriteLine(methodInfo);
    }

    protected virtual async Task<int> GetValueAsync(params int[] args) {
        return await Promise<int>.FromResult(0);
    }

    /** extern方法将导致类不完整无法运行 */
    protected static unsafe int* GetUnsafeValue(long addr) {
        throw new Exception();
    }
}