# commons

Wjybxx的公共模块，抽取以方便我的其它开源项目依赖这里的部分组件。

## 项目划分

项目在最高层分为3个子项目，分别为：base、apt、commons。

1. base是所有模块依赖的基础模块，无任何外部依赖，且apt和commons项目都依赖base项目。
2. apt是注解处理器模块，apt不能和其它项目同时打开编译，会产生错误。
3. commons是普通工具包，存在外部依赖；Commons分为多个模块，可选择性依赖。

### 如何编译该项目

1. 该项目的3个子项目需要分别独立编译。
2. 进入base项目，clean install 安装base模块到本地maven仓库；安装后可卸载 -- 后期发布到Maven仓库后，无需安装。
3. 进入apt项目，clean install 安装apt到本地maven仓库，卸载apt项目，不可与其它项目一开编译。
4. 进入commons项目，可正常开始编译 -- 如果之前出错导致无法编译，请先clean清理缓存。

PS：我现在是在根目录下打开项目，编写apt时将apt项目加载进来，安装apt以后就卸载(unlink)。

Q：编译报生成的XXX文件不存在？  
A：请先确保apt项目安装成功，如果已安装成功，请仔细检查编译输出的错误信息，通常是忘记getter等方法，修改错误后先clean，然后再编译。

Q：编译成功，但文件曝红，找不到文件？  
A：请将各个模块 target/generated-sources/annotations 设置为源代码目录（mark directory as generated source root）;   
将各个模块 target/generated-test-sources/test-annotations 设置为测试代码目录（mark directory as test source root）。

## 逻辑模块(commons-parent)

### core

对base模块进行一定丰富后的模块，存在一些外部依赖。

### concurrent

并发工具库，提供事件循环，Future等实现。

### rpc

rpc基础模块，提供基本的Rpc实现

## 支持模块(support-parent)

support-parent是commons依赖的模块，主要是base模块apt(注解处理器)模块。

### base模块

base模块包括一些基础的工具类和注解，这些基础工具和注解被其它commons模块依赖，也被apt模块依赖。
base模块无任何外部依赖。

### apt-base模块

所有apt模块都依赖的基础模块，这里实现了apt的基础流程管理，和apt的基础工具类。

### apt-commons

apt-commons是commons下所有模块的注解处理器。  
commons和apt中的注解处理器的依赖关系是被反转的，apt的编写去除了注解的直接依赖，这可以避免将注解放在support模块。

这里的指导是：**注解处理器永远是可选模块！**  
业务逻辑可以在不依赖注解处理器的执行，可以手动编写代码代替APT生成。