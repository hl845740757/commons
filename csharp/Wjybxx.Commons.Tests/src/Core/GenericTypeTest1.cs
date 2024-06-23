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

using System;
using System.Reflection;
using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

/// <summary>
/// 了解信息的存储方式后，我们才可以正确生成Codec代码。
/// </summary>
public class GenericTypeTest1
{
    [Test]
    public void TestVoidInfo() {
        Type type = typeof(void);
        Console.WriteLine("void is primitive == " + type.IsPrimitive); // false
        Console.WriteLine("void.ToString == " + type); // System.Void
    }

    [Test]
    public void TestRefReturnMethod() {
        var type = typeof(Span<byte>);
        MethodInfo methodInfo = type.GetMethod("GetPinnableReference");
        Console.WriteLine(methodInfo!.ReturnType); // System.Byte&
        Console.WriteLine(FormatTypeInfo(methodInfo!.ReturnType)); // IsByRef: True, IsByRefLike: False, IsPointer: False        
    }

    [Test]
    public void TestRefArgumentMethod() {
        var type = typeof(Array);
        MethodInfo methodInfo = type.GetMethod("Resize");
        ParameterInfo parameterInfo = methodInfo!.GetParameters()[0];
        Console.WriteLine(parameterInfo); // T[]& array
        Console.WriteLine(FormatTypeInfo(parameterInfo.ParameterType)); // IsByRef: True, IsByRefLike: False, IsPointer: False
    }

    [Test]
    public void TestInArgumentMethod() {
        var type = typeof(GenericTypeTest1);
        MethodInfo methodInfo = type.GetMethod("InArgMethod", BindingFlags.NonPublic | BindingFlags.Static);
        ParameterInfo parameterInfo = methodInfo!.GetParameters()[0];
        Console.WriteLine(parameterInfo); // Int32& val
        Console.WriteLine(FormatTypeInfo(parameterInfo.ParameterType)); // IsByRef: True, IsByRefLike: False, IsPointer: False
    }

    [Test]
    public void TestOutArgumentMethod() {
        var type = typeof(GenericTypeTest1);
        MethodInfo methodInfo = type.GetMethod("OutArgMethod", BindingFlags.NonPublic | BindingFlags.Static);
        ParameterInfo parameterInfo = methodInfo!.GetParameters()[0];
        Console.WriteLine(parameterInfo); // Int32& val
        Console.WriteLine(FormatTypeInfo(parameterInfo.ParameterType)); // IsByRef: True, IsByRefLike: False, IsPointer: False
    }

    [Test]
    public void TestPointReturnMethod() {
        var type = typeof(GenericTypeTest1);
        MethodInfo methodInfo = type.GetMethod("PointReturnMethod", BindingFlags.NonPublic | BindingFlags.Static);
        Console.WriteLine(methodInfo!.ReturnType); // System.Int32*
        Console.WriteLine(FormatTypeInfo(methodInfo!.ReturnType)); // IsByRef: False, IsByRefLike: False, IsPointer: True
    }

    [Test]
    public void TestPointArgumentMethod() {
        var type = typeof(GenericTypeTest1);
        MethodInfo methodInfo = type.GetMethod("PointArgMethod", BindingFlags.NonPublic | BindingFlags.Static);
        ParameterInfo parameterInfo = methodInfo!.GetParameters()[0];
        Console.WriteLine(parameterInfo); // Int32* val
        Console.WriteLine(FormatTypeInfo(parameterInfo.ParameterType)); // IsByRef: False, IsByRefLike: False, IsPointer: True
    }

    [Test]
    public void TestRefPointArgumentMethod() {
        var type = typeof(GenericTypeTest1);
        MethodInfo methodInfo = type.GetMethod("RefPointArgMethod", BindingFlags.NonPublic | BindingFlags.Static);
        ParameterInfo parameterInfo = methodInfo!.GetParameters()[0];
        Console.WriteLine(parameterInfo); // Int32*& val -- 引用类型排在后面
        Console.WriteLine(FormatTypeInfo(parameterInfo.ParameterType)); // IsByRef: True, IsByRefLike: False, IsPointer: False
    }

    private static void InArgMethod(in int val) {
    }

    private static void OutArgMethod(out int val) {
        val = 0;
    }

    private static unsafe int* PointReturnMethod(in int val) {
        return (int*)val;
    }

    private static unsafe void PointArgMethod(int* val) {
    }

    // 还能这样吗...指针*可连续N个
    private static unsafe void RefPointArgMethod(ref int* val) {
    }

    private static string FormatTypeInfo(Type type) {
        return $"IsByRef: {type.IsByRef}, IsByRefLike: {type.IsByRefLike}, IsPointer: {type.IsPointer}";
    }
}