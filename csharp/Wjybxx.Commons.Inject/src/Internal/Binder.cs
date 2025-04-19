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

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 默认的绑定器实现
/// </summary>
internal class Binder : IInjectBinder
{
    /// <summary>
    /// 父注射器
    /// </summary>
    private readonly Injector? parent;
    /// <summary>
    /// 所有的配置数据 -- 按服务索引，可能多对1
    /// </summary>
    private readonly Dictionary<ServiceKey, InjectBeanConfig> configDic = new();

    public Binder(Injector? parent) {
        this.parent = parent;
    }

    public Injector Build() {
        return new Injector(parent, configDic);
    }

    public void Bind(InjectBeanConfig config) {
        foreach (ServiceKey key in config.serviceKeys) {
            if (configDic.ContainsKey(key)) {
                throw new ArgumentException($"service is already exist, {key.serviceType}-{key.serviceName}");
            }
        }
        foreach (var key in config.serviceKeys) {
            configDic.Add(key, config);
        }
    }
}
}