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

using NUnit.Framework;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;

namespace Commons.Tests.Inject;

/// <summary>
/// 基础的注入测试
/// 
/// 包含字段和属性的注入测试，以及可选注入测试
/// </summary>
public class FieldInjectTest
{
    [Test]
    public void Test() {
        IInjector injector = InjectorExtensions.CreateInjector(new InjectModule());
        LogicModule logicModule = injector.GetInstance<LogicModule>();

        Assert.NotNull(logicModule.service1);
        Assert.NotNull(logicModule.service2);
        Assert.IsNull(logicModule.service3);

        Assert.NotNull(logicModule.Props1);
        Assert.NotNull(logicModule.Props2);
        Assert.IsNull(logicModule.Props3);
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
        [Inject] public IService1 service1;

        [Inject] public IService2 service2;

        [Inject(true)]
        public IService3? service3;

        [Inject] public IService1 Props1 { get; private set; }

        [Inject] public IService2 Props2 { get; private set; }

        [Inject(true)]
        public IService3 Props3 { get; private set; }

        /// <summary>
        /// 由于没定义set，会被忽略
        /// </summary>
        [Inject] public IService3 Props4 { get; }
    }
}