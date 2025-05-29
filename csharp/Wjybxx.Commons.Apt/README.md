# APT (注解处理器工具)

为让Poet包可以在任意场景使用，我将程序集进行了拆分，由该模块对`Roslyn`进行支持。
该程序集主要提供大量的Util方法，方便基于Roslyn边界代码生成器。

## NullableReferenceType问题

在我们将TargetFramework调整为netstandard2.0后，反射无法访问NullableAttribute，
因此反射解析各种类型数据的接口都无法准确解析NRT信息，而Roslyn编译时根据TypeSymbol是可以获得NRT信息的。

## 版本号同步

注意：该程序集总是和`Wjybxx.Commons.Poet`版本号保持一致，不论该程序集是否发生变化。