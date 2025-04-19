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

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;

namespace Commons.Tests.Inject;

public class NamedInjectTest
{
    [Test]
    public void Test() {
        IInjector injector = InjectorExtensions.CreateInjector(new Module1());
        // service1是绑定了name的，不应该被匹配
        Assert.IsNull(injector.GetInstance<IService1>(null, true));

        // service1是绑定了name的，不应该被匹配
        IService1 service1 = injector.GetInstance<IService1>("json");
        Assert.NotNull(service1);

        ServiceImpl serviceImpl = injector.GetInstance<ServiceImpl>();
        Assert.AreSame(serviceImpl.service1, service1);

        IService2 service2 = injector.GetInstance<IService2>();
        Assert.NotNull(service2);
        Assert.AreSame(serviceImpl.service2, service2);

        // 可选注入
        Assert.IsNull(serviceImpl.service3);

        // 多注入
        Assert.IsNotNull(serviceImpl.service4);
        Assert.AreEqual(2, serviceImpl.service4.Count);

        // 单注入--但皆未绑定
        Assert.IsNull(serviceImpl.service5);
    }

    private class Module1 : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<ServiceImpl>();
            binder.Bind<IService1>("json");
            binder.Bind<IService2>();

            // 同时绑定aaa,bbb服务
            Type typeOfService4 = typeof(IService4);
            binder.Bind(typeOfService4, InjectScope.Singleton,
                new ServiceKey(typeOfService4, "aaa"),
                new ServiceKey(typeOfService4, "bbb"));
        }
    }

    private class IService1
    {
    }

    private class IService2
    {
    }

    private class IService3
    {
    }

    private class IService4
    {
    }

#nullable disable
    private class ServiceImpl
    {
        [Inject]
        [InjectService("json")]
        public IService1 service1;

        [Inject]
        public IService2 service2;

        [Inject]
        [InjectService(true)]
        public IService3 service3;

        // 正常的多注入
        [Inject]
        [InjectService("aaa")]
        [InjectService("bbb")]
        public List<IService4> service4;

        // 单注入，按声明信息依次查找 -- 查找到ddd时结束
        [Inject]
        [InjectService("ccc", optional: true)]
        [InjectService("ddd", optional: true)]
        public IService4 service5;
    }
}