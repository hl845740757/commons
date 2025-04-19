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

/// <summary>
/// 命名服务注入测试
/// </summary>
public class NamedInjectTest
{
    [Test]
    public void Test() {
        IInjector injector = InjectorExtensions.CreateInjector(new Module1());
        // service1是绑定了name的，不应该被匹配
        Assert.IsNull(injector.GetInstance<IService1>(null, true));
        // service1根据name的查找应当是存在的
        IService1 service1 = injector.GetInstance<IService1>("json");
        Assert.NotNull(service1);

        // 顺带测试单例
        ServiceImpl serviceImpl = injector.GetInstance<ServiceImpl>();
        Assert.AreSame(serviceImpl.service1, service1);

        // 多注入
        Assert.IsNotNull(serviceImpl.service2);
        Assert.AreEqual(2, serviceImpl.service2.Count);

        // 多注入
        Assert.IsNotNull(serviceImpl.service3);
        Assert.AreEqual(1, serviceImpl.service3.Count);

        // 单注入--但皆未绑定
        Assert.IsNull(serviceImpl.service4);
    }

    private class Module1 : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<ServiceImpl>();
            binder.Bind<IService1>("json");

            // 同时绑定aaa,bbb服务
            Type typeOfService4 = typeof(IService2);
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

#nullable disable
    private class ServiceImpl
    {
        [Inject("json")]
        public IService1 service1;

        // 正常的多注入 -- count应该为2
        [Inject("aaa")]
        [Inject("bbb")]
        public List<IService2> service2;

        // 正常的多注入，其中一个不存在 -- count应该为1
        [Inject("aaa")]
        [Inject("ccc", optional: true)]
        public Dictionary<string, IService2> service3;

        // 单注入，按声明信息依次查找 -- 查找到ddd时结束，最后为null
        [Inject("ccc", optional: true)]
        [Inject("ddd", optional: true)]
        public IService2 service4;
    }
}