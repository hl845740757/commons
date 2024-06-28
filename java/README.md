# Java模块

## agent模块

agent是基于Java Instrumentation 的热更新模块，仅包含一个Agent类；由于Agent需要以Jar的方式被加载，因此作为独立的模块打包是有利的。

ps：Agent不太会产生变化，将其放在base项目下，仅是为了方便一起打包和发布。

## base模块

base模块包括一些基础的工具类和注解，这些基础工具和注解被其它commons模块依赖，也被apt模块依赖。

1. base是个人所有开源项目都可能依赖的基础模块
2. base模块无任何外部依赖。

ps：不引入commons-lang3和guava，是因为这些基础库的类文件实在太多。

## apt-base模块

所有apt模块都依赖的基础模块，这里实现了apt的基础流程管理，和apt的基础工具类。

这里的指导是：**注解处理器永远是可选模块！业务逻辑可以在不依赖注解处理器的执行，可以手动编写代码代替APT生成。**
因此，编写apt时应当反转依赖，即避免apt代码直接引用业务模块的注解，而是通过全限定名字符串的方式引用，这可以避免apt组件成为必须组件。

## disruptor模块

为了解决Disruptor的一些问题，重写了LMAX的Disruptor，实现了自己的版本。我的版本少了许多不必要的抽象，
也拥有更好更容易理解的抽象 -- 后面可能发布为单独的库。

## concurrent模块

1. 重写了Future和Promise，优化了死锁检测和上下文传递的问题。
2. 得益于新的Disruptor框架，EventLoop无需因为队列的问题而重复实现。

## 个人公众号(游戏开发)

![写代码的诗人](https://github.com/hl845740757/commons/blob/dev/docs/res/qrcode_for_wjybxx.jpg)
