# commons

Wjybxx的公共模块，抽取以方便我的其它开源项目依赖这里的部分组件。

## 项目划分

项目在最高层分为2个子项目，分别为：bases、commons。

1. bases是所有模块依赖的基础模块，无任何外部依赖；commons项目依赖bases项目，dson以及btree项目也将依赖bases项目。
2. commons是普通工具包，存在外部依赖；Commons分为多个模块，可选择性依赖。

注：bases项目和commons使用不同的版本号，bases需要单独发布。

### 如何编译该项目

1. 进入bases项目，clean install 安装base模块到本地maven仓库；安装后可卸载 -- 后期发布到Maven仓库后，无需安装。
2. 进入commons项目，可正常开始编译 -- 如果之前出错导致无法编译，请先clean清理缓存。

## bases项目模块

### agent模块

agent是基于Java Instrumentation 的热更新模块，仅包含一个Agent类；由于Agent需要以Jar的方式被加载，因此作为独立的模块打包是有利的。

ps：Agent不太会产生变化，将其放在base项目下，仅是为了方便一起打包和发布。

### base模块

base模块包括一些基础的工具类和注解，这些基础工具和注解被其它commons模块依赖，也被apt模块依赖。

1. base是个人所有开源项目都可能依赖的基础模块
2. base模块无任何外部依赖。

ps：不引入commons-lang3和guava，是因为这些基础库的类文件实在太多。

### apt-base模块

所有apt模块都依赖的基础模块，这里实现了apt的基础流程管理，和apt的基础工具类。

这里的指导是：**注解处理器永远是可选模块！业务逻辑可以在不依赖注解处理器的执行，可以手动编写代码代替APT生成。**
因此，编写apt时应当反转依赖，即避免apt代码直接引用业务模块的注解，而是通过全限定名字符串的方式引用，这可以避免apt组件成为必须组件。

## 工具项目(commons)

### core

对base模块进行一定丰富后的模块，存在一些外部依赖。

### concurrent(将重写)

并发工具库，提供事件循环，Future等实现。  
ps：计划重写现在的并发工具库，统一接口，去除对JCtools和Disruptor库的依赖。
