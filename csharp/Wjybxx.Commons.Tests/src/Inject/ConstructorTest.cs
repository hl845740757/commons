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
using NUnit.Framework;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;

namespace Commons.Tests.Inject;

/// <summary>
/// 构造函数注入测试
/// </summary>
public class ConstructorTest
{
    [Test]
    public void Test() {
        IInjector injector = InjectorExtensions.CreateInjector(new InjectModule());
        LogicModule logicModule = injector.GetInstance<LogicModule>();

        Assert.NotNull(logicModule.service1);
        Assert.NotNull(logicModule.service2);
        Assert.IsNull(logicModule.service3);
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

    private class InjectModule : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            binder.Bind<IService1>();
            binder.Bind<IService2>();
            binder.Bind<LogicModule>();
        }
    }

    private class LogicModule
    {
        public IService1 service1;
        public IService2 service2;
        public IService3? service3;

        [Inject]
        public LogicModule(IService1 service1, IService2 service2,
                           [Inject(true)] IService3 service3) {
            this.service1 = service1;
            this.service2 = service2;
            this.service3 = service3;
        }
    }
}