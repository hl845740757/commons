# commons

Wjybxx的公共模块，抽取以方便我的其它开源项目依赖这里的部分组件。

1. Java 相关模块在Maven仓库中搜索`wjybxx`即可看见。
2. Csharp 相关模块在Nuget仓库中搜索`wjybxx`即可看见。

## 重要组件一览

1. Dson文本格式 -- [Dson文本格式](docs/Dson.md).
2. 基于Dson的序列化 -- [Java实现](java/Dson-Codec/README.md)、[c#实现](csharp/Wjybxx.Dson.Codec/README.md).
3. 通用任务树(行为树) -- [行为树](docs/BTree.md)、[Java实现](java/BTree-Core)、[c#实现](csharp/Wjybxx.BTree.Core)
4. 改进的Disruptor实现 -- [Java实现](java/disruptor)、[C#实现](csharp/Wjybxx.Disruptor)
5. 改进的并发库 -- [Java核心并发库](java/Commons-Concurrent)[C#核心并发库](csharp/Wjybxx.Commons.Concurrent)
6. [参考javapoet的C#代码生成工具](csharp/Wjybxx.Commons.Apt)

## 源码Unity兼容

由于无法简单打出dll引入到unity，所以在unity中使用该项目的代码时，请直接下载源码。

## 个人公众号(游戏开发)

![写代码的诗人](https://github.com/hl845740757/commons/blob/dev/docs/res/qrcode_for_wjybxx.jpg)