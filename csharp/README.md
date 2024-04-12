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

## ReleaseNotes

### 1.0.12

1. 统一命名规范，具体可见[C#命名规范](https://github.com/hl845740757/commons/blob/dev/csharp/NameRules.md)

### 1.0.11

1. bugfix - 修复BoundedArrayDeque溢出时索引更新错误，修复队列迭代可能无法退出的问题。

### 1.0.8~1.0.10

concurrent库初版

### 1.0.7

1. bugfix - LinkedDictionary/LinkedHashSet移动元素到首尾时未更新version
2. DateTime工具类

### 1.0.6

1. 增加ArrayPool，变更ObjectPool的Clear方法为FreeAll
2. 增加环境变量工具类
3. 增加BufferUtil

### 1.0.5

1. 添加StringBuilderPool，为Dson仓库服务
2. ObjectPool接口删除不必要方法

### 1.0.4

1. 迁移仓库，和Java Commons仓库合并。
2. Build更改为Release

### 1.0.3

1. 移植Time工具包
2. 增加部分常用异常
3. 增加轻量级对象池

### 1.0.2

1. Collection增加IsEmpty方法
2. 增加少量DateTime工具方法

### 1.0.1

1. BoundedArrayDeque fix Count为0时SetCapacity导致的head下标错误。