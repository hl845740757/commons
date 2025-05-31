# 依赖注入工具

C# 也有一些依赖注入工具，比如Autofac，但Autofac设计得太复杂了，而且缺少我需要的子注入器。
所以，我还是决定实现一个我熟悉的依赖注入框架。

## 版本号同步

注意：`Wjybxx.Commons.Core`,`Wjybxx.Commons.Inject`,`Wjybxx.Commons.Concurrent` 三个程序集的版本号总是保持一致，任一程序集修改，其它程序集版本号也会修改。


## 注入方式

默认实现支持字段、属性、构造函数和普通方法注入，但构造函数不支持重载，即总是使用第一个带有注解（属性）的构造函数创建对象。

## 单例作用域问题(配置级别)

单例是锁定配置的，指向同一个配置的服务才将共享同一个实例。

```csharp
    public void Configure(IInjectBinder binder) {
        // 这将为ServiceType1和ServiceType2分别创建一个实例
        binder.Bind<ImpType, ServiceTyp1>(InjectScope.Singleton);
        binder.Bind<ImpType, ServiceTyp2>(InjectScope.Singleton);
        
        // ServiceType1和ServiceType2将共享同一个实例
        binder.Bind<ImpType>(InjectScope.Singleton, ServiceTyp1, ServiceTyp2);
    }
```

PS：如果实现类是泛型的，那么不同运行时类型之间的单例也是独立的。

## 泛型问题

当实现类是泛型类时，如果不能自动从服务类继承泛型参数，则需要用户提供函数构建对应的实现类型。

## 命名服务

依赖注入框架支持为服务命名；当服务未同时绑定无name服务时--即全部以name进行绑定时，申请实例时也必须指定名字，否则将抛出异常或返回null。

```csharp
    // injector配置
    public void Configure(IInjectBinder binder) {
        binder.Bind<ServiceImpl>();
         // 如果未同时绑定无name服务，将无法直接通过typeof(IService1)获得实例
        binder.Bind<IService1>("json");
        binder.Bind<IService2>();
    }
    //
   private class ServiceImpl
    {
        [Inject("json")] // 指定注入命名为json的Service1
        public IService1 service1;

        [Inject] // 注入 IService2，不存在时抛出异常
        public IService2 service2;

        [Inject(true)] // Service3不存在时保持null
        public IService3 service3;
    }
```

## 循环依赖

不支持构造器的循环依赖，循环依赖仅支持延迟注入 —— 不然复杂度太高。

## 注入容器

```csharp
        // 该方式可以注入容器对象的引用
        [Inject] public IInjector injecor;
```

## 反射

默认实现是基于反射的，不适用于高频**创建**对象的场景，也不适用GameObject的管理，通常用于框架层的对象管理。

PS：单例对象的效率是不差的。

## Release Notes

### 1.4.x

1. 增加`InjecMembers`接口，允许为既有实例注入依赖 —— 在Unity项目很有用。