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

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 依赖注入的模块化配置
///
/// 每个程序集可以有自己的依赖注入设置（可能多个），当我们需要创建一个大型的<see cref="IInjector"/>时，
/// 只需要收集<see cref="IInjectModule"/>即可。
/// </summary>
public interface IInjectModule
{
    /// <summary>
    /// 配置依赖注入器
    /// </summary>
    /// <param name="binder"></param>
    void Configure(IInjectBinder binder);
}
}