# Csharp-Poet

Poet包是[javapoet](https://github.com/square/javapoet)仓库的移植版。
我在java端使用javapoet生成各类辅助类已有5年左右，这是个非常好用的轮子的，但C#端没有合适的等价物，于是自己移植了一版。

用言语描述Poet包不够直观，我们直接看代码生成器和生成的代码（可运行测试用例）（代码生成器的最佳示例可见Dson和BTree库）。

`GeneratorTest`测试类（生成器）代码如下：

```csharp
    private static TypeSpec BuildClassType() {
        TypeName dictionaryTypeName = TypeName.Get(typeof(LinkedDictionary<string, object>));
        AttributeSpec processorAttribute = AttributeSpec.NewBuilder(ClassName.Get(typeof(GeneratedAttribute)))
            .Constructor(CodeBlock.Of("$S", "GeneratorTest")) // 字符串$S
            .Build();

        AttributeSpec attributeSpec = AttributeSpec.NewBuilder(ClassName.Get(typeof(MyCodeAttribute)))
            .AddMember("Name", CodeBlock.Of("$S", "wjybxx"))
            .AddMember("Age", CodeBlock.Of("29"))
            .Build();

        TypeSpec classType = TypeSpec.NewClassBuilder("ClassBean")
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorAttribute)
            .AddAttribute(attributeSpec)
            // 字段
            .AddField(TypeName.INT, "age", Modifiers.Private)
            .AddField(TypeName.STRING, "name", Modifiers.Private)
            .AddSpec(FieldSpec.NewBuilder(dictionaryTypeName, "blackboard", Modifiers.Public | Modifiers.Readonly)
                .Initializer("new $T()", dictionaryTypeName)
                .Build())
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
            // 属性
            .AddSpec(PropertySpec.NewBuilder(TypeName.INT, "Age", Modifiers.Public)
                .Getter(CodeBlock.Of("age").WithExpressionStyle(true))
                .Setter(CodeBlock.Of("age = value").WithExpressionStyle(true))
                .Build())
            .AddSpec(PropertySpec.NewBuilder(TypeName.BOOL, "IsOnline", Modifiers.Private)
                .Initializer("$L", false)
                .Build()
            )
            // 普通方法
            .AddSpec(MethodSpec.NewMethodBuilder("Sum")
                .AddDocument("求int的和")
                .AddModifiers(Modifiers.Public)
                .Returns(TypeName.INT)
                .AddParameter(TypeName.INT, "a")
                .AddParameter(TypeName.INT, "b")
                .Code(CodeBlock.NewBuilder()
                    .AddStatement("return a + b")
                    .Build())
                .Build())
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

下面是测试`GeneratorTest`类生成的代码：

```csharp
    using Wjybxx.Commons.Attributes;
    using Commons.Tests.Apt;
    using Wjybxx.Commons.Collections;
    
    namespace Wjybxx.Commons.Apt;
    
    [Generated("GeneratorTest")]
    [MyCode(Name = "wjybxx", Age = 29)]
    public class ClassBean 
    {
      private int age;
      private string name;
      public readonly LinkedDictionary<string, object> blackboard = new LinkedDictionary<string, object>();
    
      public ClassBean()
       : this(29, "wjybxx") {
      }
    
      public ClassBean(int age, string name) {
        this.age = age;
        this.name = name;
      }
    
      public int Age {
        get => age;
        set => age = value;
      }
    
      private bool IsOnline { get; set; } = false;
    
      /// <summary>
      /// 求int的和
      /// </summary>
      public int Sum(int a, int b) {
        return a + b;
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

## 版本号同步

注意：该程序集总是和`Wjybxx.Commons.Apt`版本号保持一致，不论该程序集是否发生变化。

## ReleaseNotes

### 1.5.0

1. 增加了元组和记录类型支持

```csharp
  /// <summary>
  /// 测试返回元组
  /// </summary>
  public (int min, int max) MinMax(int a, int b) => a < b ? (a, b) : (b, a);
```

### 1.4.0

1. 删除`TypeVariableName`，新增`TypeParameterSpec`和`TypeParameterName`，新的Name仅表达对泛型变量的引用，不包含泛型约束条件。
2. `TypeName`变更为抽象类，基础类型也由`ClassName`表达。

在之前的版本中，`TypeName`是参照JavaPoet实现的；但随着使用的增多，发现JavaPoet的`TypeVariableName`设计问题很大。
`TypeName`的初衷是对目标类型的引用，但`TypeVariableName`却包含了对泛型变量的约束，这导致了一些复杂的问题。

### 1.3.1

bug fix

1.修复attribute无构造器时，生成代码会多一个逗号问题

### 1.3.0

1. Poet从Commons.Apt中拆分出来，成为独立的无依赖的程序集 -- 不再依赖Commons.Core项目。
2. 修改了属性`PropertySepc`的Modifier管理，拆分为明确的`getter`和`setter`
3. 统一工厂方法为`Get`
4. TypeName为Roslyn做了一些兼容支持