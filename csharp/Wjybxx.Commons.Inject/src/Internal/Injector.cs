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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 默认的注入器实现
/// 
/// </summary>
internal class Injector : IInjector
{
    /// <summary>
    /// 父注射器
    /// </summary>
    private readonly Injector? parent;
    /// <summary>
    /// 只读不写，线程安全
    /// </summary>
    /// <returns></returns>
    private readonly Dictionary<ServiceKey, InjectBeanConfig> configDic;
    /// <summary>
    /// 1.会随着泛型类信息扩充而扩充。
    /// 2.会并发读写，需要使用并发集合，并发写时不能覆盖已有数据。
    /// </summary>
    private readonly ConcurrentDictionary<BeanInfoKey, BeanInfo> beanInfoDic = new();
    /// <summary>
    /// 服务到BeanInfo的缓存
    /// 注意：多个Service可能映射到同一个<see cref="BeanInfo"/>
    /// </summary>
    private readonly ConcurrentDictionary<ServiceKey, BeanInfo> service2BeanInfoDic = new();
    /// <summary>
    /// 注入的类型缓存
    /// (理论上这个数据是可以全局缓存的)
    /// </summary>
    private readonly ConcurrentDictionary<Type, InjectionType> injectionTypeDic = new();

    public Injector(Injector? parent, Dictionary<ServiceKey, InjectBeanConfig> configDic) {
        this.parent = parent;
        this.configDic = new Dictionary<ServiceKey, InjectBeanConfig>(configDic); // copy

        // 如果实现类不是泛型的，那么提前创建BeanInfo -- 这里需要去重
        LinkedHashSet<InjectBeanConfig> configs = new LinkedHashSet<InjectBeanConfig>();
        configs.AddAll(configDic.Values);
        foreach (InjectBeanConfig config in configs) {
            if (config.implType.IsGenericTypeDefinition) {
                continue;
            }
            InjectionType injectionType = new InjectionType(config.implType);
            injectionTypeDic.TryAdd(config.implType, injectionType);

            BeanInfo beanInfo = new BeanInfo(config, config.implType, config.instance, injectionType);
            BeanInfoKey beanInfoKey = new BeanInfoKey(config.configId, config.implType);
            beanInfoDic.TryAdd(beanInfoKey, beanInfo);
        }
        // 如果实现类不是泛型的，那么提前建立Service到BeanInfo的缓存
        foreach (var pair in this.configDic) {
            InjectBeanConfig config = pair.Value;
            if (config.implType.IsGenericTypeDefinition) {
                continue;
            }
            BeanInfoKey beanInfoKey = new BeanInfoKey(config.configId, config.implType);
            if (!beanInfoDic.TryGetValue(beanInfoKey, out BeanInfo beanInfo)) {
                throw new AssertionError();
            }
            service2BeanInfoDic.TryAdd(pair.Key, beanInfo);
        }
    }

    public object? GetInstance(Type serviceType, string? name, bool optional = false) {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        // 允许注入容器
        if (serviceType == typeof(IInjector) && name == null) {
            return this;
        }
        ServiceKey key = new ServiceKey(serviceType, name);
        try {
            if (service2BeanInfoDic.TryGetValue(key, out BeanInfo beanInfo)) {
                return GetOrCreateInstance(serviceType, beanInfo);
            }
            // 如果不是泛型，如果不在beanInfo中，则必须在父注入器中
            if (!serviceType.IsGenericType) {
                return TryGetInstanceFromParent(serviceType, name, optional);
            }
            // 如果是泛型，先尝试从当前Injector查找对应的泛型原型的Config
            Type genericTypeDefinition = serviceType.GetGenericTypeDefinition()!;
            ServiceKey key2 = new ServiceKey(genericTypeDefinition, name);
            if (!configDic.TryGetValue(key2, out InjectBeanConfig config)) {
                return TryGetInstanceFromParent(serviceType, name, optional);
            }
            // 根据泛型类创建对应的实现类，然后创建对应的BeanInfo
            Type implType = MakeImplType(serviceType, config);
            beanInfo = GetOrCreateBeanInfo(implType, config);
            // 添加到Service的缓存
            service2BeanInfoDic.TryAdd(key, beanInfo);
            return GetOrCreateInstance(serviceType, beanInfo!);
        }
        catch (Exception e) {
            if (e is InjectionException) {
                throw;
            }
            throw new InjectionException($"serviceType: {serviceType}, name: {name}", e);
        }
    }

    private object? TryGetInstanceFromParent(Type serviceType, string? name, bool optional) {
        if (parent == null) {
            if (optional) {
                return null;
            }
            throw new InjectionException($"serviceType: {serviceType} is not registered");
        }
        return parent.GetInstance(serviceType, name, optional);
    }

    public void InjectMembers(object instance) {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        InjectionType injectionType = GetOrCreateInjectionType(instance.GetType());
        InjectMembers(instance, injectionType);
    }

    public IInjector CreateChild(IEnumerable<IInjectModule> modules) {
        Binder binder = new Binder(this);
        foreach (IInjectModule module in modules) {
            module.Configure(binder);
        }
        return binder.Build();
    }

    public void Dispose() {
        // 暂不实现
    }

    #region internal

    /// <summary>
    /// 根据服务类和配置创建真实的实现类
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static Type MakeImplType(Type serviceType, InjectBeanConfig config) {
        if (serviceType.GetGenericTypeDefinition() == config.implType) {
            return serviceType;
        }
        if (config.implTypeMaker != null) {
            return config.implTypeMaker.Invoke(serviceType);
        } else {
            return config.implType.MakeGenericType(serviceType.GetGenericArguments());
        }
    }

    /// <summary>
    /// 动态创建<see cref="InjectionType"/>
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private InjectionType GetOrCreateInjectionType(Type type) {
        if (!injectionTypeDic.TryGetValue(type, out InjectionType r)) {
            r = new InjectionType(type);
            injectionTypeDic.TryAdd(type, r);
        }
        return r;
    }

    /// <summary>
    /// 动态创建泛型的BeanInfo
    /// </summary>
    private BeanInfo GetOrCreateBeanInfo(Type implType, InjectBeanConfig config) {
        BeanInfoKey key = new BeanInfoKey(config.configId, implType);
        if (beanInfoDic.TryGetValue(key, out BeanInfo beanInfo)) {
            return beanInfo;
        }
        InjectionType injectionType = GetOrCreateInjectionType(implType);
        beanInfo = new BeanInfo(config, implType, null, injectionType);
        if (!beanInfoDic.TryAdd(key, beanInfo)) {
            beanInfoDic.TryGetValue(key, out beanInfo); // 不可覆盖既有实例
        }
        return beanInfo!;
    }

    /// <summary>
    /// 走到这里时，实现类如果是泛型类，则是已构造泛型
    /// </summary>
    private object GetOrCreateInstance(Type serviceType, BeanInfo beanInfo) {
        object r;
        if (beanInfo.config.scope == InjectScope.Singleton) {
            r = beanInfo.instance;
            if (r != null) return r;
            // 需要加锁竞争 -- double check
            lock (beanInfo) {
                if ((r = beanInfo.instance) != null) {
                    return r;
                }
                if (beanInfo.creatingServiceType != null) {
                    throw new InjectionException($"cyclic dependency, {beanInfo.creatingServiceType} => {serviceType}");
                }
                beanInfo.creatingServiceType = serviceType;
                try {
                    r = CreateInstance(serviceType, beanInfo);
                }
                finally {
                    beanInfo.creatingServiceType = null;
                }
                // 在注入属性前需要先发布出去，因为可能存在延时的循环依赖
                beanInfo.instance = r;
                InjectMembers(r, beanInfo.injectionType);
            }
        } else {
            r = CreateInstance(serviceType, beanInfo);
            InjectMembers(r, beanInfo.injectionType);
        }
        return r;
    }

    /// <summary>
    /// 为instance注入字段和属性
    /// </summary>
    private void InjectMembers(object instance, InjectionType injectionType) {
        foreach (InjectionPoint injectionPoint in injectionType.injectionPoints) {
            MemberInfo memberInfo = injectionPoint.memberInfo;
            switch (memberInfo.MemberType) {
                case MemberTypes.Field: {
                    FieldInfo fieldInfo = (FieldInfo)memberInfo;
                    object dependencyInst = ResolverDependency(injectionPoint.dependencies[0]);
                    fieldInfo.SetValue(instance, dependencyInst);
                    break;
                }
                case MemberTypes.Property: {
                    PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                    object dependencyInst = ResolverDependency(injectionPoint.dependencies[0]);
                    propertyInfo.SetValue(instance, dependencyInst);
                    break;
                }
                case MemberTypes.Method: {
                    MethodInfo methodInfo = (MethodInfo)injectionPoint.memberInfo;
                    object[] parameters = new object[injectionPoint.dependencies.Count];
                    foreach (Dependency dependency in injectionPoint.dependencies) {
                        parameters[dependency.parameterIndex] = ResolverDependency(dependency);
                    }
                    methodInfo.Invoke(instance, parameters);
                    break;
                }
                default:
                    Debug.Assert(memberInfo.MemberType == MemberTypes.Constructor);
                    break;
            }
        }
        if (injectionType.onCreateHook != null) {
            injectionType.onCreateHook.Invoke(instance, Array.Empty<object>());
        }
    }

    /// <summary>
    /// 创建实现类的实例
    /// </summary>
    private object CreateInstance(Type serviceType, BeanInfo beanInfo) {
        try {
            Func<Type, Type, object> factory = beanInfo.config.factory;
            if (factory != null) {
                return factory(serviceType, beanInfo.implType);
            }
            InjectionPoint constructorPoint = beanInfo.injectionType.ConstructorInjectionPoint;
            if (constructorPoint == null) {
                return Activator.CreateInstance(beanInfo.implType) ?? throw new InjectionException("Activator.CreateInstance failed");
            }
            // 解析构造函数的参数
            ConstructorInfo constructorInfo = (ConstructorInfo)constructorPoint.memberInfo;
            object[] parameters = new object[constructorPoint.dependencies.Count];
            foreach (Dependency dependency in constructorPoint.dependencies) {
                parameters[dependency.parameterIndex] = ResolverDependency(dependency);
            }
            return constructorInfo.Invoke(parameters);
        }
        catch (Exception ex) {
            throw new InjectionException($"create instance failed, implType {beanInfo.implType}", ex);
        }
    }

    /// <summary>
    /// 解析依赖的实例
    /// </summary>
    private object? ResolverDependency(Dependency dependency) {
        if (dependency.IsList) {
            // 反射构建List
            object list = Activator.CreateInstance(dependency.listType!);
            foreach (var attribute in dependency.injectAttributes!) {
                object serviceInst = GetInstance(dependency.serviceType, attribute.name, attribute.optional);
                if (serviceInst == null) {
                    continue; // 可选的
                }
                dependency.addMethod!.Invoke(list, new[] { serviceInst }); // Add(inst)
            }
            return list;
        }
        if (dependency.IsDictionary) {
            // 反射构建字典
            object dictionary = Activator.CreateInstance(dependency.dictionaryType!);
            foreach (var attribute in dependency.injectAttributes!) {
                object serviceInst = GetInstance(dependency.serviceType, attribute.name, attribute.optional);
                if (serviceInst == null) {
                    continue; // 可选的
                }
                dependency.addMethod!.Invoke(dictionary, new[] { attribute.name, serviceInst }); // Add(string, inst)
            }
            return dictionary;
        }
        // 非List或字典时，我们按配置依次查找
        if (dependency.injectAttributes.Count > 0) {
            foreach (var attribute in dependency.injectAttributes) {
                object serviceInst = GetInstance(dependency.serviceType, attribute.name, attribute.optional);
                if (serviceInst != null) {
                    return serviceInst;
                }
            }
            // 这里不继续查找，否则会对用户造成迷惑 -- 走到这里证明optional为true
            return null;
        }
        return GetInstance(dependency.serviceType, null);
    }

    #endregion
}
}