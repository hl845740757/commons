# 历史版本记录

### 1.1.0.RC

1. 增加ArrayPool和简单并发对象池
2. 优化LinkedDictionary和LinkedHashSet的Node为结构体，减少GC
3. 删除GenericDictionary中的UnsafeKeys接口
4. 增加`ImmutableList`、`ImmutableLinkedHashSet`、`ImmutableLinkedDictionary`，以兼容Unity。
5. 语法降级C#9，支持Dotnet5和Unity2021 （unity项目需下载源码编译）
6. 一些bug修复

### 1.0.14~1.0.15

1. APT库相关支持（部分工具方法）
2. 集合的默认迭代器修改为结构体类型，并对外开放

### 1.0.13

1. bugfix - MultiChunkQueue TryRemove* 接口在队列为空时抛出异常.
2. bugfix - 获取系统Tick在不同平台单位不一致问题。
3. 增加新的枚举器接口 - `ISequentialEnumerator`允许测试还有下一个元素。

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