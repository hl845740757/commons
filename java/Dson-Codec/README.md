# Dson序列化

Dson有许多强大的特性，你如果只是简单使用Dson，那和普通的序列化组件差不多，可能还没那么方便，因为还要做点准备工作；
如果与Dson深度集成，Dson将提供许多强大的功能。

ps:

1. 下面的代码片段来自test目录下的`CodecBeanExample`类。
2. 对Dson和DsonCodec的大面积应用见[BigCat](https://github.com/hl845740757/BigCat)项目。
3. Csharp的Codec也会实现这儿的特性。
4. 建议多阅读源码文档和测试用例，这里的文档不一定及时更新。

## 特性一览

1. 有限泛型支持
2. 默认值可选写入
3. 指定数字字段的编码格式
4. 支持多态解析，指定指定默认解码类型
5. **字段级别的读写代理** -- 核心功能。
6. 序列化钩子方法
7. 单例支持
8. 为外部库类生成Codec
9. 外部静态代理

## 有限泛型支持

由于Java在运行时无法获得对象的泛型类型信息，因此Dson库对泛型的支持是有限的 —— 如果泛型参数也是泛型类，在编码时将擦除泛型信息。
支持泛型的优点：

1. 类型自解释，精准编解码
2. 跨语言通信支持 -- 更多是共享配置文件。

## 默认值可选写入

对于基础类型 int32,int64,float,double,bool，可以通过`Options.appendDef`控制是否写入写入默认值；
对于引用类型，可以通过`Options.appendNull`控制是否写入null值。

如果数据结构中有大量的可选属性（默认值），那么不写入默认只和null可以很好的压缩数据包。

## 指定数字字段的编码格式

Dson集成了Protobuf的组件，支持数字的`varint`、`unit`、`sint`、`fixed`4种编码格式，你可以简单的通过`DsonProperty`注解声明
字段的编码格式，而且**修改编码格式不会导致兼容性问题**，eg：

```
    @DsonProperty(wireType = WireType.UINT)
    public int age;
    
    // 生成的编码代码
    writer.writeInt(names_age, instance.age, WireType.UINT);
    writer.writeString(names_name, instance.name);
```

示例中的int类型的age字段，在编码时将使用uint格式编码。

## 指定多态字段的实现

以Map的解码为例，一般序列化框架只能反序列化为LinkedHashMap，限制了业务对数据结构的引用；但Dson支持你指定字段的实现类，eg：

```
    @DsonProperty(impl = EnumMap.class)
    public Map<Sex, String> sex2NameMap3;
```

上面的这个Map字段在解码时就会解码为EnumMap。**具体类型的集合和Map，通常不需要指定实现类**，但也是可以指定的，eg：

```
    // 未指定实现类，APT判断为具体类型，直接调用构造函数
    public Int2IntOpenHashMap currencyMap1;
    
    // 指定了实现类型，APT调用指定类的构造函数
    @DsonProperty(impl = Int2IntOpenHashMap.class)
    public Int2IntMap currencyMap2;
    
    // 生成的factory
    public static final Supplier<Map<CodecBeanExample.Sex, String>> factories_sex2NameMap3 = () -> new EnumMap<>(CodecBeanExample.Sex.class);    
    
    // 生成的解码代码
    instance.sex2NameMap3 = reader.readObject(names_sex2NameMap3, types_sex2NameMap3, factories_sex2NameMap3);
```

上面的这两个Map字段都会解码为 Int2IntOpenHashMap，编解码代码都是生成的静态代码，看看生成的代码你就很容易明白这是如何实现的。

## 字段级别的读写代理(核心)

上面都是`DsonProperty`的简单用法，`DsonProperty`的最强大之处就在于字段级别的读写代理。  
Dson的理念是：**能托管的逻辑就让生成的代码负责，用户只处理特殊编解码的部分**。  
一个很好的编码指导是：**我们写的代码越少，代码就越干净，维护成本就越低，项目代码质量就越有保证**。

与一般的序列化工具不同，Dson支持生成的代码调用用户的自定义代码，从而实现在编解码过程中用户只处理特殊字段逻辑。  
举个栗子，假设一个Class有100个字段，有一个字段需要特殊解码，那么用户就可以只处理这一个字段的编解码，其余的仍然由生成的代码负责，
生成的代码在编解码该特殊字段的时候就会调用用户手写的代码。看段代码：

ps: 字段读写代理几乎可实现`DsonProperty`提供的其它所有功能。

```
    @DsonProperty(writeProxy = "writeCustom", readProxy = "readCustom")
    public Object custom;

    // 定义了钩子方法后，生成的Codec代码会自动调用
    public void writeCustom(DsonObjectWriter writer, String name) {
        writer.writeObject(custom, TypeArgInfo.OBJECT);
    }

    public void readCustom(DsonObjectReader reader, String name) {
        this.custom = reader.readObject(TypeArgInfo.OBJECT);
    }
```

我们在类中有一个Object类型的custom字段，并且通过`DsonProperty`声明了读写代理方法的名字，
生成的代码就会在编解码custom的时候调用用户的方法，下面是生成的代码节选：

```
    // 解码方法
    instance.currencyMap1 = reader.readObject(names_currencyMap1, types_currencyMap1);
    instance.currencyMap2 = reader.readObject(names_currencyMap2, types_currencyMap2);
    instance.readCustom(reader, names_custom);
    // 编码方法
    writer.writeObject(names_currencyMap1, instance.currencyMap1, types_currencyMap1);
    writer.writeObject(names_currencyMap2, instance.currencyMap2, types_currencyMap2);
    instance.writeCustom(writer, names_custom);
```

## 序列化钩子方法

Dson提供了`newInstance`、`constructor`、`afterDecode`、`beforeEncode`、`writeObject`、`readObject`6种默认的钩子调用支持。

1. 如果类提供了静态的`newInstance(DsonObjectReader, TypeInfo)`方法，将自动调用 -- 优先级高于构造方法，可处理final字段。
2. 如果类提供了非私有的DsonObjectReader的单参构造方法，将自动调用 -- 该方法可用于final和忽略字段。
3. 如果类提供了非私有的`afterDecode(ConverterOptions)`方法，且在options中启用，则自动调用 -- 通常用于数据转换，或构建缓存字段。
4. 如果类提供了非私有的`beforeEncode(ConverterOptions)`方法，且在options中启用，则自动调用 -- 通常用于数据转换。
5. 如果类提供了非私有的`readObject(DsonObjectReader)`方法，将自动调用 -- 该方法可用于忽略字段。
6. 如果类提供了非私有的`writeObject(DsonObjectWriter)`方法，将自动调用 -- 该方法可用于final和忽略字段。
7. 如果是通过`DsonCodecLinkerBean`配置的类，这些方法都需要转换为静态方法 -- 外部静态代理。

注意，这里仍然遵从前面的编码指导，你只需要处理特殊的字段，其它字段交给生成的代码处理即可。

```
    // 序列化前钩子
    public void beforeEncode(ConverterOptions options) {
    }
    // 自定义写入字段 - 紧随beforeEncode调用
    public void writeObject(DsonObjectWriter writer) {
    }
    
    // newInstance钩子 - 可处理final字段
    public static CodecBeanExample newInstance(DsonObjectReader reader, TypeInfo typeInfo) {
        return new CodecBeanExample();
    }    
    // 读自定义写入字段
    public void readObject(DsonObjectReader reader) {
    }
    // 反序列化钩子
    public void afterDecode(ConverterOptions options) {
        if (age < 1) throw new IllegalStateException();
    }
   
    // 字段读写钩子
    public void writeCustom(DsonObjectWriter writer, String name) {
    }
    public void readCustom(DsonObjectReader reader, String name) {
    }
```

## 单例支持

Dson在`DsonClassProps`注解中提供了`singleton`属性，当用户指定`singleton`属性时，生成的Codec将简单调用给定方法返回共享实例。

```java

@DsonClassProps(singleton = "getInstance")
@DsonSerializable
public class SingletonTest {
    private static final SingletonTest INST = new SingletonTest("wjybxx", 29);

    public static SingletonTest getInstance() {
        return INST;
    }
}
```

## 为外部类生成Codec类

APT除了支持为项目中的类生成Codec外，还支持为外部库的类生成Codec，通过`DsonCodecLinkerGroup`和`DsonCodecLinker`两个注解实现。

```java

@DsonCodecLinkerGroup(outputPackage = "cn.wjybxx.btree.fsm")
private static class FsmLinker {
    @DsonCodecLinker(props = @DsonClassProps)
    private ChangeStateTask<?> changeStateTask;
    @DsonCodecLinker(props = @DsonClassProps)
    private StateMachineTask<?> stateMachineTask;
}
```

ps: 该注解的最佳实例可见[BTree-Codec](https://github.com/hl845740757/BTree/tree/dev/java/btree-codec)

## 外部静态代理

如果我们要序列化的是一个外部库的类，且期望能够参与到目标类型序列化过程中，我们就可以通过`CodecLinkerBean`
实现外部静态代理。  
`CodecLinkerBean`支持除构造函数以外的所有钩子，包括字段的读写代理。

```
@CodecLinkerBean(value = ThirdPartyBean2.class)
public class CodecLinkerBeanTest {

    @DsonProperty(wireType = WireType.UINT)
    private ThirdPartyBean2 age;

    @DsonProperty(stringStyle = StringStyle.AUTO_QUOTE)
    private ThirdPartyBean2 name;

    // newInstance钩子 - 可处理final字段
    public static ThirdPartyBean2 newInstance(DsonObjectReader reader, TypeInfo typeInfo) {
        return new ThirdPartyBean2();
    }    

    // 这些钩子方法，生成的代码会自动调用
    public static void beforeEncode(ThirdPartyBean2 inst, ConverterOptions options) {
    }
    public static void writeObject(ThirdPartyBean2 inst, DsonObjectWriter writer) {
    }
    public static void readObject(ThirdPartyBean2 inst, DsonObjectReader reader) {
    }
    public static void afterDecode(ThirdPartyBean2 inst, ConverterOptions options) {
    }
}
```

ps：

1. `CodecLinkerBean`同样会为目标Bean生成Codec类。
2. 你可以将`CodecLinkerBean`看做目标Bean的外部配置类。
3. 注解的使用实例可参考[BigCat](https://github.com/hl845740757/BigCat)项目。

---

## Dson与Json、Bson、Protobuf的比较

### Dson与Json和Bson

Json的数据格式是简单清晰的，但不是自解释的，即不能表达它是由什么数据序列化而来。
Bson在Json的基础之上进行了一定的改进，设计了值类型，生成的Json也是特殊的，在反序列化时能较Json更精确一些；
但这仍然不够，Bson的文档和Json的对象一样，不是自解释的（缺少自描述信息），因此在反序列化时无法精确解析，只能解析为Document。

对于Document，要实现精确的解析，我们可以在Document里存储一些特殊的数据以表达其类型 -- 轻度污染；
对于Array，则没有办法，因为在Array里插入额外数据是危险的，数组元素个数的改变是危险的 -- 重度污染。  
简单说，Json和Bson在设计之初并没有很好的考虑反序列化的问题，因此不适合做复杂情况下的序列化组件。

Dson为Array和Object设计了一个对象头，用于保存其类型信息，由于它是单独存储的，因此不会对数据造成污染。

阅读源码，大家会发现Dson的代码和MongoDB的Bson很像，这是因为我对MongoDB的Bson较为熟悉--前几年研究过Bson和Protobuf的编解码，
于是这两天写Dson的时候参考了Bson的代码，不过我们在许多地方的设计仍然是不同的，我相信你用Dson的Reader和Writer会更舒服。

### Dson与Protobuf

Q：为什么不使用Protobuf序列化？  
A：Protobuf是很好的序列化工具，但它也存在一些问题：

1. 必须定义Proto文件。
2. 不能序列化自定义类，必须定义Message，然后通过Builder进行构建。
3. 不支持继承多态 -- 我们只能编码为bytes或展开为标签类
4. 兼容性问题 -- Protobuf过于兼容了。

#### 定义Proto文件

其实，对于一个跨语言的序列化工具来说，通过DSL文件描述数据结构是必须的，因此这个问题是个小问题；
不过在我们不需要跨语言的时候，维护proto文件就有点让人不爽。

#### 自定义类问题

对于不能序列化自定义类这点，在java端是容易解决的，因为有注解。我们可以通过注解将一个类声明为需要按照Protobuf格式序列化，
然后静态或动态代码生成编解码代码；Protobuf的序列化格式是比较简单的，因此生成代码并不算复杂。
(其实有现成的框架——protostuff)

而对于不能通过工具解决的语言或项目，维护自定义类到Message之间的映射是痛苦的，这需要付出较多的维护成本。  
（当年既要写转Message的代码，还要写MongoDB的Codec的日子真的痛苦...）

#### 继承和多态问题

Protobuf不支持多态（继承），是因为其需要定义schema，而schema要求一切都是明确的，明确的数据可以让编码结果更小。
我们在通过protobuf传输多态对象时，通常使用万能的bytes，将类型信息和对象一起放入bytes，或将其展开为标签类。

如果我们传输的数据通常是简单的，那么使用Protobuf并不会带来太大的影响；但在实际的业务开发中，出现继承的频率是很高的，
这导致我们定义了许多的标签类，让人维护得很是难受。

PS：我见过一些项目，由于序列化的缺陷，导致业务数据结构设计受到掣肘 -- 类似的是，由于SQL数据表的限制，业务数据结构按照表结构设计，
我认为这是不好的，因为依赖关系是反的，这使得你的业务代码很难迁移。

#### 兼容性问题

protobuf的数据非常兼容，以至于发生一些不期望的事情，这与protobuf的编码格式有关，pb的字段编码结果中只包含filedNumber和wireType，
即字段编号和编码格式，而**不包含字段的类型信息**，解码时完全按照接收方的schema进行解码，就可能胡乱解码，产生奇怪的兼容或异常。

不过，PB也正是能省则省才能够达到这么小的包体，在客户端与服务器通信时仍然是首选，在客户端服务器同步维护前，我们通常避免修改字段的类型。  
不过，也正是因为PB的这些问题，PB是不适合做持久化存储的 -- 个人认为用PB持久化（入库），等于给自己挖坑。

PS：DSON在序列化时仍然使用了Protobuf的数字压缩算法，以压缩数字。