#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Time;

/// <summary>
/// 
/// </summary>
public static class TimeProviders
{
    /** 获取系统[毫秒时间戳]实时时间提供器 */
    public static ITimeProvider SystemMillisProvider() {
        return CSystemMillisProvider.Instance;
    }

    /** 获取系统[Tick]实时时间提供器 */
    public static ITimeProvider SystemTickProvider() {
        return CSystemTickProvider.Instance;
    }

    /// <summary>
    /// 创建一个简单的时间提供者
    /// </summary>
    /// <param name="time">初始时间</param>
    /// <returns></returns>
    public static ICachedTimeProvider NewTimeProvider(long time = 0) {
        return new TimeProvider(time);
    }

    /// <summary>
    /// 创建一个基于deltaTime更新的时间提供器，用在一些特殊的场合。
    /// 你需要调用{@link Timepiece#update(long)}更新时间值。
    /// </summary>
    /// <returns></returns>
    public static ITimepiece NewTimepiece() {
        return new Timepiece();
    }

    #region internal

    private class CSystemTickProvider : ITimeProvider
    {
        internal static readonly CSystemTickProvider Instance = new();

        public long Current => ObjectUtil.SystemTicks();

        public override string ToString() {
            return "SystemTickProvider{}";
        }
    }

    private class CSystemMillisProvider : ITimeProvider
    {
        internal static readonly CSystemMillisProvider Instance = new();

        public long Current => DatetimeUtil.CurrentEpochMillis();

        public override string ToString() {
            return "SystemMillisProvider{}";
        }
    }

    #endregion
}