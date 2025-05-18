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
/// 池化的资源对象
/// </summary>
public interface IPooledDisposable
{
    /// <summary>
    /// 查询资源对象是否已退出当前生命周期
    /// </summary>
    /// <param name="reentryId"></param>
    /// <returns></returns>
    bool IsDisposed(long reentryId);

    /// <summary>
    /// 关闭资源
    /// </summary>
    /// <param name="reentryId"></param>
    void Dispose(long reentryId);
}
}