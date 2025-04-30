# APT (注解处理器工具)

为让Poet包可以在任意场景使用，我将程序集进行了拆分，由`Wjybxx.Commons.Apt`对Roslyn进行支持。

## NullableReferenceType问题

在我们将TargetFramework调整为netstandard2.0后，反射无法访问NullableAttribute，
因此反射解析各种类型数据的接口都无法准确解析NRT信息，而Roslyn编译时根据TypeSymbol是可以获得NRT信息的。

## CodeAnalysis依赖管理

Commons.Apt的定位就是非运行时的代码生成工具，因此默认是传递`Microsoft.CodeAnalysis.CSharp`的依赖的；
不过，为了兼容Unity2021，默认的依赖是`3.8.0`；因此是不包含`IIncrementalGenerator`的；用户如果需要使用`IIncrementalGenerator`，
需要将依赖升级到`4.3.1`版本及以上。

在编写完自己的