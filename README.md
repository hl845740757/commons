# commons

个人公共模块

## base模块

base模块包括一些基础的工具类和注解，这些基础工具和注解被其它commons模块依赖，也被apt模块依赖。
base模块无任何外部依赖。

## 逻辑模块

### core

对base模块进行一定丰富后的模块，存在一些外部依赖。

### concurrent

并发工具库，提供事件循环，Future等实现。

### rpc

rpc基础模块，提供基本的Rpc实现

## 注解处理器

### apt-base模块

所有apt模块都依赖的基础模块，这里实现了apt的基础流程管理，和apt的基础工具类。

### apt-commons

apt-commons是commons下所有模块的注解处理器。