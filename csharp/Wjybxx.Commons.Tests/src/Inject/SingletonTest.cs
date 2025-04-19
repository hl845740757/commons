#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections.Generic;
using NUnit.Framework;
using Wjybxx.Commons.Inject;
using static Wjybxx.Commons.Inject.InjectorExtensions;

namespace Commons.Tests.Inject;

public class SingletonTest
{
    [Test]
    public void TestSingleton() {
        IInjector injector = CreateInjector(new Module1());
        IService1 service1 = injector.GetInstance<IService1>();
        IService2 service2 = injector.GetInstance<IService2>();
        Assert.AreNotSame(service1, service2);
        // 重复获取返回同一个实例
        Assert.AreSame(service1, injector.GetInstance<IService1>());
        Assert.AreSame(service2, injector.GetInstance<IService2>());

        // 两个服务在同一个配置
        injector = CreateInjector(new Module2());
        service1 = injector.GetInstance<IService1>();
        service2 = injector.GetInstance<IService2>();
        Assert.AreSame(service1, service2);
        Assert.AreSame(service1, injector.GetInstance<IService1>());
        Assert.AreSame(service2, injector.GetInstance<IService2>());
    }

    private interface IService1
    {
    }

    private interface IService2
    {
    }

    private class ServiceImpl : IService1, IService2
    {
    }

    /// <summary>
    /// service1和2应当返回不同实例
    /// </summary>
    private class Module1 : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<ServiceImpl, IService1>();
            binder.Bind<ServiceImpl, IService2>();
        }
    }

    /// <summary>
    /// service1和2应当返回同一个实例
    /// </summary>
    private class Module2 : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<ServiceImpl>(InjectScope.Singleton, typeof(IService1), typeof(IService2));
        }
    }
}