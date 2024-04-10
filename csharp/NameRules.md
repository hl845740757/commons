# C# 命名规范

公共认知：

1. **小写表示变量**，下划线强调私有。
2. **大写表示方法**，以及实质为方法的属性。
3. 下划线可用于方法分组。

指导思想：

1. **不同概念的东西就应该使用不同的命名规则** —— 字段和方法。
2. 相似概念的东西就应该使用类似的命名规则 —— 属性和方法。

---

## 实例私有变量\(private)

1. _lowerCamelCase \(强调私有)
2. lowerCamelCase

私有变量推荐使用下划线前缀，开放给其它类的internal变量建议使用小驼峰。

## 实例开放变量\(not private)

1. lowerCamelCase

这里不遵循C#的推荐风格，因为变量和属性的本质不同，属性并不能真的兼容字段；另外，使用大驼峰会破坏我们的公共认知，即：小驼峰表示变量，大驼峰表示方法。

```csharp
    public class Person {
        // 私有变量
        private int _age;
        // 程序集变量
        internal int pwd;
        // 可继承变量
        protected int flags;
        // 共有变量
        public Vector3 pos;
    }
```

## 静态私有变量\(private)

1. lowerCamelCase
2. ALL_UPPER \(类似常量)
3. _lowerCamelCase \(强调私有)

## 静态开放变量\(not private)

1. lowerCamelCase
2. ALL_UPPER \(类似常量)

Q：为什么不使用C#推荐的大驼峰风格？  
A：对于静态字段和非静态字段，我们已经在使用方式上存在了明显区别，因此仍建议命名突显方法和字段的区别，即：**小驼峰表示字段，大驼峰表示方法和属性**。

```csharp
    // 普通静态字段
    public static int count = 0;
    // 类似常量
    public static readonly Object NIL = new Object();
    
```

## 常量

1. ALL_UPPER \(和其它语言一致)
2. UpperCamelCase

全大写加下划线风格主要为保持和其它语言一致，避免切换语言时带来的额外负担。

```csharp
    // 全大写，下划线分割
    public const int MASK_PRIORITY = 0X0F;
    // 大驼峰
    public const int MaskPriority = 0X0F;
```

## 枚举

1. UpperCamelCase

```csharp
    public enum Color {
        Red,
        Green
    }
```

---

## 方法

1. UpperCamelCase
2. UpperCamelCase_UpperCamelCase （强调分组）

一般情况下，方法和属性都应该遵循大驼峰命名规则。

## 属性

1. UpperCamelCase

属性是特殊的方法，因此其命名应接近方法。由于属性不应该出现特殊的分组等功能，因此属性只使用大驼峰规则。

PS：如果你觉得你的属性需要分组，最好的方式是将其修改为方法。

```csharp
    // 普通情况
    public void ChangeState(State state) {}
    // 强调分组
    internal void Internal_ChangeState(State state) {}
    // 强调分组
    public void Template_CheckCondition(Condition condition) {}
    
    // 属性统一大驼峰
    public int Count {get;}
    private string Name {get; set;}
```

---

## IDE设置图例

![naming rules](https://github.com/hl845740757/commons/blob/dev/docs/res/csharp_namerules.png)