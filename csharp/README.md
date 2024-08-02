# csharp-commons

csharp公共库，包含集合等基础组件;nuget搜索'wjybxx'即可查看到相关库。

注意： 1.0.x 尚属于快速迭代版本，等**基础工具基本完备的时候，会更新到 1.1.x 版本**。因为我还有几个项目没有完成，会把遇见的一些基础工具迁移到这里，或在这里实现。

## 命名规则

由于我频繁在Java和C#之间切换，因此统一的命名规则对我来说很必要，但这可能让仅写C#的开发者不适应。具体的命名规则可见：[C#命名规范](./NameRules.md)

## Unity源码兼容

C#工具库的主要是为游戏服务器和客户端造的，但几经尝试，发现无法简单打出dll直接拉入到unity，所以只能在源码中通过条件编译实现Unity兼容。
为避免代码差异过大，我限定语法等级为C#9，Unity版本为2021。

Unity2021尚不支持的特性:

1. 不支持文件范围命名空间(C#9) -- 为保持尽可能小的增量变化，我将文件范围namespace修改为旧式namespace时，不进行缩进。
2. override时不能修改方法的返回值类型，因此对于简单的类型转换的情况，使用new代替override实现。
3. 不能使用新的Immutable集合库 -- 我实现了自己的Immutable集合。

## C#模块说明

### Commons.Core

Core模块包含一些基础的工具类和注解，这些基础工具和注解被其它所有模块依赖。

1. Core提供了一些有用的集合实现
2. Core提供了简单的对象池实现

### Commons.APT模块(源代码生成器)

APT包是[javapoet](https://github.com/square/javapoet)仓库的移植版。
我在java端使用javapoet生成各类辅助类已有5年左右，这是个非常好用的轮子的，但C#端没有合适的等价物，于是自己移植了一版。

### Disruptor模块(TODO)

Disruptor是LMAX的Disruptor的C#端实现，但并不是直接实现，而是修改后的实现，与我重写Java版的Disruptor模块一致。

### Commons.Concurrent模块

1. 提供了Java的Executor和Future框架，并提供了对应的await语法支持。
2. 提供了默认的EventLoop实现。

### Dson.Core

Dson是我设计的文本格式，Dson.Core则是Dson文本格式的C#端实现。

了解Dson可阅读：[Dson文本](../docs/Dson.md)

### Dson.Codec

Dson.Codec是基于Dson文本的序列化实现，支持以下特性：

1. **支持泛型**
2. 默认值可选写入
3. 指定数字字段的编码格式(apt)
4. 支持多态解析，指定指定默认解码类型(apt)
5. **字段级别的读写代理(核心功能)**(apt)
6. 序列化钩子方法(apt)
7. 单例支持(apt)
8. 为外部库类生成Codec(apt)
9. 外部静态代理(apt)

由于我们提供了强大灵活的Apt，因此不支持运行时反射编解码类型。

### Dson.Apt

Dson.Apt是为Dson.Codec提供的工具，用于生成目标类的编解码类（源代码），以避免运行时的反射开销。
由于C#的编译时源码生成器尚不成熟，使用案例甚少，因此Dson.Apt是基于反射分析类型的，因此尽量避免循环依赖 -- 目标Bean最好是独立的Assembly。

PS：Dson.Apt的最佳应用是`Btree.Codec`模块，行为树的所有Codec都是通过Apt自动生成的，而非手动编写的。

## Btree.Core

btree-core是从bigcat中分离出来的，为保持最小依赖，核心包只依赖我个人的base包和jsr305注解包；
但行为树是需要能序列化的，这样才能在编辑器中编辑；在bigcat仓库的时候，btree模块依赖了我的dson-codec包，
但dson-codec包的类比较多，依赖也比较大(尤其是fastutil)，因此我将行为树的codec配置信息抽取为btree-codec模块，可选择性引入。

## Btree.Codec

btree-codec是基于dson-codec的行为树序列化实现；btree-codec模块仅有几个配置类，真正的codec是基于dson-apt注解自动生成的。
如果你需要使用基于dson的行为树序列化实现，可以添加btree-codec到项目。

## 个人公众号(游戏开发)

![写代码的诗人](../docs/res/qrcode_for_wjybxx.jpg)

