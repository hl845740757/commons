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

namespace Commons.Tests.Apt;

/// <summary>
/// 通过<see cref="GenericParameterAttributes"/>和<see cref="GenericParameterAttributes"/>来测试泛型信息，
/// 注意，只能在泛型参数的Type实例上调用。
///
/// 了解信息的存储方式后，我们才可以正确生成Codec代码。
/// </summary>
public class GenericTypeTest2
{
    /// <summary>
    /// 输出为：genericArgument: T, constraints: [System.ValueType], attributes: NotNullableValueTypeConstraint, DefaultConstructorConstraint
    ///
    /// 可见struct关键字既添加了类型约束，还添加了<see cref="GenericParameterAttributes"/>属性。
    /// 注意：<see cref="Void"/>和<see cref="ValueType"/>都不能直接当做泛型参数。
    /// </summary>
    private class GenericTypeA<T> where T : struct
    {
    }

    /// <summary>
    /// 输出为：genericArgument: T, constraints: [], attributes: ReferenceTypeConstraint
    /// </summary>
    private class GenericTypeB<T> where T : class
    {
    }

    /// <summary>
    /// 输出为：genericArgument: T, constraints: [System.Reflection.IReflect], attributes: None
    /// </summary>
    private class GenericTypeC<T> where T : IReflect
    {
    }

    /// <summary>
    /// 输出为：genericArgument: T, constraints: [], attributes: ReferenceTypeConstraint, DefaultConstructorConstraint
    ///
    /// class关键字不能和其它具体的Class或接口共用，但可以和 new() 共用，都对应了<see cref="GenericParameterAttributes"/>
    /// </summary>
    private class GenericTypeD<T> where T : class?, new()
    {
    }

    /// <summary>
    /// 输出为：genericArgument: T, constraints: [System.Reflection.IReflect], attributes: DefaultConstructorConstraint
    ///
    /// 可见：notnull关键字并没有实际的作用(反射时拿不到)，而且不能和struct、class关键字同时使用。
    /// </summary>
    private class GenericTypeE<T> where T : notnull, IReflect, new()
    {
    }

    [Test]
    public void TestGeneric() {
        Type type = typeof(GenericTypeA<>);
        PrintGenericArgInfo(type.GetGenericArguments()[0]);

        type = typeof(GenericTypeB<>);
        PrintGenericArgInfo(type.GetGenericArguments()[0]);

        type = typeof(GenericTypeC<>);
        PrintGenericArgInfo(type.GetGenericArguments()[0]);

        type = typeof(GenericTypeD<>);
        PrintGenericArgInfo(type.GetGenericArguments()[0]);

        type = typeof(GenericTypeE<>);
        PrintGenericArgInfo(type.GetGenericArguments()[0]);
    }

    private static void PrintGenericArgInfo(Type genericArgument) {
        Type[] constraints = genericArgument.GetGenericParameterConstraints(); // 类型约束
        GenericParameterAttributes attributes = genericArgument.GenericParameterAttributes; // 属性
        // 集合的默认ToString不打印元素....
        Console.WriteLine($"genericArgument: {genericArgument}, constraints: {CollectionUtil.ToString(constraints)}, attributes: {attributes}");
    }
}