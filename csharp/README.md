# csharp-commons

csharp公共库，包含集合等基础组件;nuget搜索'wjybxx'即可查看到相关库。

## ReleaseNotes

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