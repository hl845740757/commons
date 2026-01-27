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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 用于避免将非必要方法定义在<see cref="IInjector"/>类
/// </summary>
public static class InjectorExtensions
{
    #region create-injector

    public static IInjector CreateInjector(IInjectModule module) {
        Binder binder = new Binder(null);
        module.Configure(binder);
        return binder.Build();
    }

    public static IInjector CreateInjector(params IInjectModule[] modules) {
        Binder binder = new Binder(null);
        foreach (IInjectModule module in modules) {
            module.Configure(binder);
        }
        return binder.Build();
    }

    public static IInjector CreateInjector(IEnumerable<IInjectModule> modules) {
        Binder binder = new Binder(null);
        foreach (IInjectModule module in modules) {
            module.Configure(binder);
        }
        return binder.Build();
    }

    public static IInjectModule ToInjectModule(this IList<InjectBeanConfig> beanConfigs) {
        return new InjectModule(beanConfigs);
    }

    #endregion

#nullable disable

    #region injector

    /// <summary>
    /// 获取指定实例
    /// </summary>
    /// <param name="injector">注入器</param>
    /// <param name="optional">是否是可选的</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetInstance<T>(this IInjector injector, bool optional = false) where T : class {
        return (T)injector.GetInstance(typeof(T), null, optional);
    }

    /// <summary>
    /// 获取指定实例
    /// </summary>
    /// <param name="injector">注入器</param>
    /// <param name="name">多注入时实例的名字</param>
    /// <param name="optional">是否是可选的</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetInstance<T>(this IInjector injector, string name, bool optional = false) where T : class {
        return (T)injector.GetInstance(typeof(T), name, optional);
    }

    /// <summary>
    /// 获取指定实例
    /// </summary>
    /// <param name="injector">注入器</param>
    /// <param name="serviceType">服务类型</param>
    /// <param name="optional">是否是可选的</param>
    /// <returns></returns>
    public static object GetInstance(this IInjector injector, Type serviceType, bool optional = false) {
        return injector.GetInstance(serviceType, null, optional);
    }

    #endregion

    #region binder

    /// <summary>
    /// 添加一个服务配置
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="builder">构建器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind(this IInjectBinder binder, InjectBeanConfigBuilder builder) {
        binder.Bind(builder.Build());
    }

    /// <summary>
    /// 绑定服务到自身
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="scope">绑定范围</param>
    /// <typeparam name="T">实现类型，也是服务类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind<T>(this IInjectBinder binder, InjectScope scope = InjectScope.Singleton) {
        Type implType = typeof(T);
        Bind(binder, implType, scope, implType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="scope">实例范围</param>
    /// <typeparam name="T">实现类型</typeparam>
    /// <typeparam name="U">服务类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind<T, U>(this IInjectBinder binder, InjectScope scope = InjectScope.Singleton) {
        Type implType = typeof(T);
        Type serviceType = typeof(U);
        Bind(binder, implType, scope, serviceType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="serviceTypes">实现的服务类型</param>
    /// <param name="scope">实例范围</param>
    /// <typeparam name="T">实现类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind<T>(this IInjectBinder binder, InjectScope scope, params Type[] serviceTypes) {
        Type implType = typeof(T);
        Bind(binder, implType, scope, serviceTypes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="implType">实现类型</param>
    /// <param name="scope">实例范围</param>
    /// <param name="serviceTypes">实现的服务类型</param>
    public static void Bind(this IInjectBinder binder, Type implType, InjectScope scope, params Type[] serviceTypes) {
        if (serviceTypes.Length == 0) {
            serviceTypes = new[] { implType };
        }
        List<ServiceKey> serviceKeys = new(serviceTypes.Length);
        foreach (var serviceType in serviceTypes) {
            serviceKeys.Add(new ServiceKey(serviceType, null));
        }
        binder.Bind(new InjectBeanConfigBuilder(implType)
        {
            scope = scope,
            serviceKeys = serviceKeys,
        }.Build());
    }

    /// <summary>
    /// 绑定服务到实例
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="inst">实现类实例</param>
    /// <param name="scope">作用域</param>
    /// <param name="serviceTypes">服务类型</param>
    /// <typeparam name="T">实现类型</typeparam>
    public static void Bind<T>(this IInjectBinder binder, T inst, InjectScope scope, params Type[] serviceTypes) {
        if (serviceTypes.Length == 0) {
            serviceTypes = new[] { typeof(T) };
        }
        List<ServiceKey> serviceKeys = new(serviceTypes.Length);
        foreach (var serviceType in serviceTypes) {
            serviceKeys.Add(new ServiceKey(serviceType, null));
        }
        binder.Bind(new InjectBeanConfigBuilder(typeof(T))
        {
            scope = scope,
            serviceKeys = serviceKeys,
            instance = inst
        }.Build());
    }

    // ---------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="serviceName">服务名字</param>
    /// <param name="scope">实例范围</param>
    /// <typeparam name="T">实现类型，也是服务类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind<T>(this IInjectBinder binder, string serviceName, InjectScope scope = InjectScope.Singleton) {
        Type implType = typeof(T);
        Bind(binder, implType, scope, new ServiceKey(implType, serviceName));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="serviceName">服务名字</param>
    /// <param name="scope">实例范围</param>
    /// <typeparam name="T">实现类型</typeparam>
    /// <typeparam name="U">服务类型</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Bind<T, U>(this IInjectBinder binder, string serviceName, InjectScope scope = InjectScope.Singleton) {
        Type implType = typeof(T);
        Type serviceType = typeof(U);
        Bind(binder, implType, scope, new ServiceKey(serviceType, serviceName));
    }

    // ---------------------------------------------

    /// <summary>
    /// 
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="implType">实现类型</param>
    /// <param name="scope">实例范围</param>
    /// <param name="serviceKeys">实现的服务信息</param>
    public static void Bind(this IInjectBinder binder, Type implType, InjectScope scope, params ServiceKey[] serviceKeys) {
        if (serviceKeys.Length == 0) {
            serviceKeys = new[] { new ServiceKey(implType, null) };
        }
        // 转换为服务键
        binder.Bind(new InjectBeanConfigBuilder(implType)
        {
            scope = scope,
            serviceKeys = new List<ServiceKey>(serviceKeys),
        }.Build());
    }

    /// <summary>
    /// 绑定服务到实例
    /// </summary>
    /// <param name="binder">绑定器</param>
    /// <param name="inst">实现类实例</param>
    /// <param name="scope">作用域</param>
    /// <param name="serviceKeys">实现的服务信息</param>
    /// <typeparam name="T">实现类型</typeparam>
    public static void Bind<T>(this IInjectBinder binder, T inst, InjectScope scope, params ServiceKey[] serviceKeys) {
        if (serviceKeys.Length == 0) {
            serviceKeys = new[] { new ServiceKey(typeof(T), null) };
        }
        // 转换为服务键
        binder.Bind(new InjectBeanConfigBuilder(typeof(T))
        {
            scope = scope,
            serviceKeys = new List<ServiceKey>(serviceKeys),
            instance = inst
        }.Build());
    }

    #endregion
}
}