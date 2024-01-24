# disruptor2

我为了统一抽象，以及解决Disruptor的一些问题，参考Disruptor的一些设计实现了自己的版本。我的版本少了许多不必要的抽象，
也拥有更好更容易理解的抽象。

先上架构图：![Disruptor架构图](https://github.com/hl845740757/commons/blob/dev/docs/res/MyDisruptor.png)

## 与LMAX的Disruptor差异

1. RingBuffer仅是数据结构，没有额外的职责。
2. Sequencer是协调的集成，而不是生产者屏障，生产者屏障有明确的抽象。
3. 协调的基本单位是屏障`Barrier`，依赖的单位也是屏障，而不是序列`Sequence`。
4. 反转了Barrier和Consumer之间的Sequence依赖。
5. 剥离了Blocker和WaitStrategy，**消费者可以使用不同的等待策略**，但使用同一个Blocker。
6. 库只提供了核心的协调功能，并没有提供BatchEventProcessor这样的具体组件，完全由用户控制。
7. 内置了一套无界缓冲区`MpUnboundedBuffer`。

ps: 很自信地讲，我的设计更容易理解。

## VarHandle和其内存语言

关于J9的VarHandle的内存意义，建议阅读 doug
lea文章 [Using JDK 9 Memory Order Modes](https://gee.cs.oswego.edu/dl/html/j9mm.html)

## 其它

1. JDK要求JDK11
2. 测试用例暂时还没迁移，因为不是直接测试RingBuffer
3. 这套框架的应用可见我并发库的EventLoop，[传送门](https://github.com/hl845740757/commons) -- 测试用例也在commons仓库。