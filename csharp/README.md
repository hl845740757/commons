# csharp-commons

csharp公共库，包含集合等基础组件;nuget搜索'wjybxx'即可查看到相关库。

注意： 1.0.x 尚属于快速迭代版本，等**基础工具基本完备的时候，会更新到 1.1.x 版本**。因为我还有几个项目没有完成，会把遇见的一些基础工具迁移到这里，或在这里实现。

## 命名规则

由于我频繁在Java和C#之间切换，因此统一的命名规则对我来说很必要，但这可能让仅写C#的开发者不适应。具体的命名规则可见：[C#命名规范](https://github.com/hl845740757/commons/blob/dev/csharp/NameRules.md)

---

## Collections

但凡C#的基础集合库好用一点，我也不至于自己造轮子，实现的集合类：

1. LinkedDictionary 保持插入顺序的字典，并提供大量的有用方法。
2. LinkedHashSet 保持插入顺序的Set，并提供大量的有用方法。
3. IndexedPriorityQueue 含索引的优先级队列，高查询和删除效率。
4. BoundedArrayDeque 基于数组的有界双端队列，允许手动调整容量。
5. MultiChunkDeque 分块无界双端队列。

LinkedDictionary特殊接口示例：

```csharp

    public class LinkedDictionary<TKey,TValue> {
        TKey PeekFirstKey();
        TKey PeekLastKey();
        void AddFirst(TKey key, TValue value);
        void AddLast(TKey key, TValue value);
        KeyValuePair<TKey, TValue> RemoveFirst();
        KeyValuePair<TKey, TValue> RemoveLast();
        TValue GetAndMoveToFirst(TKey key);        
        TValue GetAndMoveToLast(TKey key);
    }
    
```

LinkedDictionary采用线性探测法解决hash冲突，通过在GetNode方法中记录线性探测次数，统计查询数据如下：

1. 1W个int类型key，查询所有key，线性探测总次数 4000~5000， 平均值小于1
2. 10W个int类型key，查询所有key，线性探测总次数 11000~12000，平均值小于1
3. 1W个string类型key，长度24，查询所有key，线性探测总次数 4000~5000，平均值小于1 -- 与int相似，且调整长度几无变化。
4. 10W个string类型key，长度24，查询所有key，线性探测总次数 11000~12000，平均值小于1 -- 与int相似，且调整长度几无变化。

---

## Concurrent包

1. 提供了Java的Executor和Future框架，并提供了对应的await语法支持。
2. 提供了默认的EventLoop实现。

PS：c#端不打算再实现Disruptor库。

### await语法的讨论

C#的Concurrent包，个人用得非常难受。究其原因：上下文(sync/execution)的传递是隐式的。这导致我们对线程和任务上下文的控制力度较弱，部分功能编写起来十分难受。  
以我的使用经验来看，C#的await/async语法会导致这样的结果：**使简单的问题更简单，使复杂的问题更复杂**。

多线程编程从来都不是个简单问题，单线程语言的`await/async`不涉及复杂的线程和上下文切换问题，因此使用起来简单；
在多线程下，存在上下文和线程的控制问题，按照单线程语言`async/await`语法进行设计，只是让代码看起来简单了，实际上不论是语言的开发者，还是语言的使用者，面临的问题都更多了。

个人认为， 多线程下的`await`最佳实现应当允许传参，允许指定`await`后续操作的线程，以及其它调度选项。

```csharp
    await future executor;    
    await future executor options;
```

---

## APT包

APT包是[javapoet](https://github.com/square/javapoet)仓库的移植版。
我在java端使用javapoet生成各类辅助类已有5年左右的历史，这是个非常好用的轮子的，但C#端没有合适的等价物，于是自己移植了一版。

用言语解释javapoet不够直观，我们直接看生成代码生成器和生成的代码（可运行测试用例）。

`GeneratorTest`测试类（生成器）。

```csharp
    private static TypeSpec BuildClassType() {
        TypeName dictionaryTypeName = TypeName.Get(typeof(LinkedDictionary<string, object>));
        AttributeSpec processorAttribute = AttributeSpec.NewBuilder(ClassName.Get(typeof(GeneratedAttribute)))
            .Constructor(CodeBlock.Of("$S", "GeneratorTest")) // 字符串$S
            .Build();

        TypeSpec classType = TypeSpec.NewClassBuilder("ClassBean")
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorAttribute)
            // 字段
            .AddField(TypeName.INT, "age", Modifiers.Private)
            .AddField(TypeName.STRING, "name", Modifiers.Private)
            .AddSpec(FieldSpec.NewBuilder(dictionaryTypeName, "blackboard", Modifiers.Public | Modifiers.Readonly)
                .Initializer("new $T()", dictionaryTypeName)
                .Build())
            // 属性
            .AddSpec(PropertySpec.NewBuilder(TypeName.INT, "Age", Modifiers.Public)
                .Getter(CodeBlock.Of("age").WithExpressionStyle(true))
                .Setter(CodeBlock.Of("age = value").WithExpressionStyle(true))
                .Build())
            .AddSpec(PropertySpec.NewBuilder(TypeName.BOOL, "IsOnline", Modifiers.Private)
                .Initializer("$L", false)
                .Build()
            )
            // 构造函数
            .AddSpec(MethodSpec.NewConstructorBuilder()
                .AddModifiers(Modifiers.Public)
                .ConstructorInvoker(CodeBlock.Of("this($L, $S)", 29, "wjybxx"))
                .Build())
            .AddSpec(MethodSpec.NewConstructorBuilder()
                .AddModifiers(Modifiers.Public)
                .AddParameter(TypeName.INT, "age")
                .AddParameter(TypeName.STRING, "name")
                .Code(CodeBlock.NewBuilder()
                    .AddStatement("this.age = age")
                    .AddStatement("this.name = name")
                    .Build())
                .Build())
            // 普通方法
            .AddSpec(MethodSpec.NewMethodBuilder("SumNullable")
                .AddDocument("求空int的和")
                .AddModifiers(Modifiers.Public | Modifiers.Extern)
                .Returns(TypeName.INT.MakeNullableType())
                .AddParameter(TypeName.INT.MakeNullableType(), "a")
                .AddParameter(TypeName.INT, "b")
                .Build())
            .AddSpec(MethodSpec.NewMethodBuilder("SumRef")
                .AddDocument("求ref int的和")
                .AddModifiers(Modifiers.Public | Modifiers.Extern)
                .Returns(TypeName.INT)
                .AddParameter(TypeName.INT.MakeByRefType(), "a")
                .AddParameter(TypeName.INT.MakeByRefType(ByRefTypeName.Kind.In), "b")
                .Build())
            .Build();
        return classType;
    }
```

以下是测试`GeneratorTest`类生成的代码。

```csharp
    using Wjybxx.Commons.Attributes;
    using Wjybxx.Commons.Collections;
    
    namespace Wjybxx.Commons.Apt;
    
    [GeneratedAttribute("GeneratorTest")]
    public class ClassBean 
    {
      private int age;
      private string name;
      public readonly LinkedDictionary<string, object> blackboard = new LinkedDictionary<string, object>();
      public int Age {
        get => age;
        set => age = value;
      }
      private bool IsOnline { get; set; } = false;
    
      public ClassBean()
       : this(29, "wjybxx") {
      }
      public ClassBean(int age, string name)  {
        this.age = age;
        this.name = name;
      }
    
      /// <summary>
      /// 求空int的和
      /// </summary>
      public extern int? SumNullable(int? a, int b);
    
      /// <summary>
      /// 求ref int的和
      /// </summary>
      public extern int SumRef(ref int a, in int b);
    }
```

## 个人公众号(游戏开发)

![写代码的诗人](https://github.com/hl845740757/commons/blob/dev/docs/res/qrcode_for_wjybxx.jpg)

## ReleaseNotes

### 1.0.15

1. APT库相关支持（部分工具方法）
2. 集合的默认迭代器修改为结构体类型，并对外开放