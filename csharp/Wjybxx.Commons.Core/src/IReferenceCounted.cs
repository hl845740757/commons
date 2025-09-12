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

namespace Wjybxx.Commons
{
/// <summary>
/// 引用计数的对象
/// </summary>
public interface IReferenceCounted
{
    /// <summary>
    /// 当前引用计数
    /// </summary>
    int RefCount { get; }

    /// <summary>
    /// 增加引用计数
    /// </summary>
    void Retain(int count = 1);

    /// <summary>
    /// 减少引用计数
    /// </summary>
    void Release(int count = 1);
}
}