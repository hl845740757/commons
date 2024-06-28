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
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Wjybxx.Commons.Apt;

namespace Commons.Tests.Apt;

/// <summary>
/// 测试<see cref="TypeName"/>的解析算法
/// </summary>
public class TypeNameTest
{
    [Test]
    public void TestGeneric() {
        // 测试内部类 -- 内部类包含了外部类的泛型参数
        {
            Console.WriteLine("-----------------------------------");
            Type type = typeof(Dictionary<string, object>.KeyCollection);
            Console.WriteLine(type);                    // System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.Object]
            ClassName typeName = ClassName.Get(type);   // System.Collections.Generic.Dictionary`2+KeyCollection[System.String,System.Object]
            Console.WriteLine(typeName);
        }
        {
            Console.WriteLine("-----------------------------------"); // 测试未构造泛型
            Type type = typeof(Dictionary<,>.KeyCollection);
            Console.WriteLine(type);                    // System.Collections.Generic.Dictionary`2+KeyCollection[TKey,TValue]
            ClassName typeName = ClassName.Get(type);   // System.Collections.Generic.Dictionary`2+KeyCollection[TKey,TValue]
            Console.WriteLine(typeName);
        }
    }
    
    /// <summary>
    /// 测试内部泛型类
    ///
    /// 现象：
    /// 1.反引号后面的数字只是自己新增的泛型变量个数。
    /// 2.反射拿到的类型信息是最终的泛型参数个数（3个），type.GetGenericArguments() 和 type.GetGenericTypeDefinition().GetGenericArguments() 都是3个。
    /// 3.内部类上的所有泛型参数的定义类都是内部类自身。
    /// 
    /// 结论：
    /// 1.反引号后面的数字只是自己新增的泛型变量个数。
    /// 2.泛型参数是通过【拷贝传递】的，拷贝以后独立。
    /// 3.内部类虽然定义在外部类中，但运行时并不依赖于外部类。
    /// </summary>
    [Test]
    public void TestNestedGenericType() {
        {
            Console.WriteLine("-----------------------------------");
            Type type = typeof(MyDic<string, object>.NestedGenericType<int>);
            Console.WriteLine(type);                    // TypeNameTest+MyDic`2+NestedGenericType`1[System.String,System.Object,System.Int32]
            ClassName typeName = ClassName.Get(type);   // TypeNameTest+MyDic`2+NestedGenericType`1[System.String,System.Object,System.Int32]
            Console.WriteLine(typeName);
        }
        {
            Console.WriteLine("-----------------------------------");
            Type type = typeof(MyDic<,>.NestedGenericType<>);
            Console.WriteLine(type);                    // TypeNameTest+MyDic`2+NestedGenericType`1[TKey,TValue,TOut]
            ClassName typeName = ClassName.Get(type);   // TypeNameTest+MyDic`2+NestedGenericType`1[TKey,TValue,TOut]
            Console.WriteLine(typeName);

            // 表达式非法 -- Partially opened type is not allowed in 'typeof' expression
            // 因为嵌套类实际上是3个泛型参数，表达式只定义了两个
            // Type type = typeof(MyDic<string, object>.NestedGenericType<>);
        } 
    }

    /// <summary>
    /// 测试泛型变量包含约束时的输出
    /// </summary>
    [Test]
    public void TestTypeVariable() {
        {
            Console.WriteLine("-----------------------------------");
            Type type = typeof(Nullable<>);
            Console.WriteLine(type);                    // System.Nullable`1[T]
            ClassName typeName = ClassName.Get(type);   // System.Nullable`1[T] where T : struct
            Console.WriteLine(typeName);                
        }
        // 测试未构造泛型
        {
            Console.WriteLine("-----------------------------------");
            Type type = typeof(GenericClass<,>);
            Console.WriteLine(type);                    // TypeNameTest+GenericClass`2[TKey,TValue]
            ClassName typeName = ClassName.Get(type);   // TypeNameTest+GenericClass`2[TKey,TValue] where TKey : struct where TValue : new()
            Console.WriteLine(typeName);                
        }
        // 测试已构造泛型
        {
            Console.WriteLine("-----------------------------------");
            Type type = typeof(GenericClass<int, Vector3>);
            Console.WriteLine(type);                    // TypeNameTest+GenericClass`2[System.Int32,System.Numerics.Vector3]
            ClassName typeName = ClassName.Get(type);   // TypeNameTest+GenericClass`2[System.Int32,System.Numerics.Vector3]
            Console.WriteLine(typeName);               
        }
    }

    private class MyDic<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public class NestedGenericType<TOut>
        {
            private TOut val;

            public NestedGenericType(TOut val) {
                this.val = val;
            }
        }
    }

    internal class GenericClass<TKey, TValue>
        where TKey : struct
        where TValue : new()
    {
        
    }
}