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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Inject;
using Wjybxx.Commons.Inject.Attributes;
using static Wjybxx.Commons.Inject.InjectorExtensions;

namespace Commons.Tests.Inject;

/// <summary>
/// 循环依赖测试
/// </summary>
public class CycleDependencyTest
{
    [Test]
    public void Test() {
        IInjector injector = CreateInjector(new BeanConfigModule());

        // IDictionary<string, object> dictionary1 = injector.GetInstance<IDictionary<string, object>>();
        // IDictionary<string, object> dictionary2 = injector.GetInstance<IDictionary<string, object>>();
        // Assert.AreNotSame(dictionary1, dictionary2);

        HeroModule heroModule = injector.GetInstance<HeroModule>();
        ItemModule itemModule = injector.GetInstance<ItemModule>();
        Assert.AreSame(heroModule.itemModule, itemModule);
        Assert.AreSame(itemModule.heroModule, heroModule);
        Assert.IsTrue(itemModule.onCreateInvoked);
    }

    private class BeanConfigModule : IInjectModule
    {
        public void Configure(IInjectBinder binder) {
            InjectorExtensions.Bind(binder, typeof(LinkedDictionary<,>), InjectScope.Prototype, new[] { typeof(IDictionary<,>) });

            binder.Bind<HeroModule>();
            binder.Bind<ItemModule>();
        }
    }

    private class HeroModule
    {
        [Inject] public ItemModule itemModule;
    }

    private class ItemModule
    {
        [Inject] public HeroModule heroModule;
        public bool onCreateInvoked;

        [InjectOnCreate]
        private void OnCreate() {
            onCreateInvoked = true;
            Console.WriteLine("ItemModule.OnCreate");
        }
    }
}