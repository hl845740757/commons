# Java模块说明

## 如何编译该项目

由于Apt(注解处理器)必须预先打包为Jar才能被其它模块使用，因此Apt必须声明为独立的项目，因此Java的Commons分为两个子项目：commons和apts。

1. 进入`java-apts`目录，执行 `mvn clean install`，安装apt到本地。
2. 如果加载了apts项目请卸载(unlink)，apts项目不能和其它项目一块编译。
3. 进入`java`目录，可正常进行编译。

Q：编译报生成的XXX文件不存在？  
A：请先确保support项目安装成功，如果已安装成功，请仔细检查编译输出的错误信息，通常是忘记getter等方法，修改错误后先clean，然后再编译。

Q：编译成功，但文件曝红，找不到文件？  
A：请将各个模块 target/generated-sources/annotations 设置为源代码目录（mark directory as generated source root）;   
将各个模块 target/generated-test-sources/test-annotations 设置为测试代码目录（mark directory as test source root）。

## commons-agent模块

agent是基于Java Instrumentation 的热更新模块，仅包含一个Agent类；由于Agent需要以Jar的方式被加载，因此作为独立的模块打包是有利的。

ps：Agent不太会产生变化，将其放在base项目下，仅是为了方便一起打包和发布。

## commons-base模块

base模块包括一些基础的工具类和注解，这些基础工具和注解被其它commons模块依赖，也被apt模块依赖。

1. base是个人所有开源项目都可能依赖的基础模块
2. base模块无任何外部依赖。

ps：不引入commons-lang3和guava，是因为这些基础库的类文件实在太多。

## commons-apt-base模块

所有apt模块都依赖的基础模块，这里实现了apt的基础流程管理，和apt的基础工具类。

这里的指导是：**注解处理器永远是可选模块！业务逻辑可以在不依赖注解处理器的执行，可以手动编写代码代替APT生成。**
因此，编写apt时应当反转依赖，即避免apt代码直接引用业务模块的注解，而是通过全限定名字符串的方式引用，这可以避免apt组件成为必须组件。

## disruptor模块

为了解决Disruptor的一些问题，重写了LMAX的Disruptor，实现了自己的版本。我的版本少了许多不必要的抽象，
也拥有更好更容易理解的抽象。

## commons-concurrent模块

1. 重写了Future和Promise，优化了死锁检测和上下文传递的问题。
2. 得益于新的Disruptor框架，EventLoop无需因为队列的问题而重复实现。

## dson-core

Dson是我设计的文本格式，Dson.Core则是Dson文本格式的C#端实现。

了解Dson可阅读：[Dson文本](../docs/Dson.md)

## dson-codec

Dson.Codec是基于Dson文本的序列化实现，支持以下特性：

1. 有限泛型支持
2. 默认值可选写入
3. 指定数字字段的编码格式(apt)
4. 支持多态解析，指定指定默认解码类型(apt)
5. **字段级别的读写代理(核心功能)**(apt)
6. 序列化钩子方法(apt)
7. 单例支持(apt)
8. 为外部库类生成Codec(apt)
9. 外部静态代理(apt)

由于我们提供了强大灵活的Apt，因此不支持运行时反射编解码类型。

## dson-apt

Dson-Apt是为Dson-Codec提供的工具，用于生成目标类的编解码类（源代码），以避免运行时的反射开销。

PS：Dson.Apt的最佳应用是`Btree.Codec`模块，行为树的所有Codec都是通过Apt自动生成的，而非手动编写的。

## btree-core

btree-core是从bigcat中分离出来的，为保持最小依赖，核心包只依赖我个人的base包和jsr305注解包；
但行为树是需要能序列化的，这样才能在编辑器中编辑；在bigcat仓库的时候，btree模块依赖了我的dson-codec包，
但dson-codec包的类比较多，依赖也比较大(尤其是fastutil)，因此我将行为树的codec配置信息抽取为btree-codec模块，可选择性引入。

## btree-codec

btree-codec是基于dson-codec的行为树序列化实现；btree-codec模块仅有几个配置类，真正的codec是基于dson-apt注解自动生成的。
如果你需要使用基于dson的行为树序列化实现，可以添加btree-codec到项目。

## 个人公众号(游戏开发)

![写代码的诗人](https://github.com/hl845740757/commons/blob/dev/docs/res/qrcode_for_wjybxx.jpg)
