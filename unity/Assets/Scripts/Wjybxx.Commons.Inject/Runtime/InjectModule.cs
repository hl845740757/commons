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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 最基础的Inject模块
/// </summary>
internal class InjectModule : IInjectModule
{
    private readonly ImmutableList<InjectBeanConfig> _beanConfigs;

    public InjectModule(IEnumerable<InjectBeanConfig> beanConfigs) {
        _beanConfigs = beanConfigs.ToImmutableList2();
    }

    public void Configure(IInjectBinder binder) {
        foreach (InjectBeanConfig beanConfig in _beanConfigs) {
            binder.Bind(beanConfig);
        }
    }
}
}