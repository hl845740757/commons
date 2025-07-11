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

#pragma warning disable CS0108
namespace Wjybxx.Commons.Fx
{
public partial class ComponentId
{
    /// <summary>
    /// 接口描述组件Builder具备的数据
    /// </summary>
    public interface IBuilder : IConstant.IBuilder
    {
        public ComponentKind Kind { get; set; }
        public bool Shared { get; set; }
        public int MaxCount { get; set; }
        public long EnableFuncs { get; set; }

        public long Flags { get; set; }
        public string? MountPath { get; set; }
        public int GroupKey { get; set; }
        public object ExtraInfo { get; set; }
        public ComponentId? BaseId { get; set; }
    }

    /// <summary>
    /// Class用于真实构建组件id
    /// （这里不能声明为泛型的<see cref="ComponentId"/>，规则太复杂）
    /// </summary>
    /// <typeparam name="T">组件的类型</typeparam>
    public class Builder<T> : IConstant.Builder<ComponentId>, IBuilder
    {
#nullable disable
        /** 组件类型，默认为需要框架调度的脚本 */
        private ComponentKind kind = ComponentKind.Script;
        /** 是否可共享 */
        private bool shared = false;
        /** 最大可挂载数量 */
        private int maxCount = 1;
        /** 启用的函数，扫描重写的方法计算得到 */
        private long enableFuncs;
        /** 超类组件id */
        private ComponentId? baseId;

        /** 业务自定义flags */
        private long flags;
        /** 挂载路径 */
        private string mountPath;
        /** 用于分组的键 */
        private int groupKey;
        /** 用户扩展数据 -- 必须的不可变的 */
        private object extraInfo;

        // public 勿改，反射调用
        public Builder(string name)
            : base(name) {
            RequireCacheIndex = true; // 需要分配缓存索引
        }

#if NET6_0_OR_GREATER
        protected internal override ComponentId<T> Build() {
            return new ComponentId<T>(this);
        }
#else
        protected internal override ComponentId Build() {
            return new ComponentId<T>(this);
        }
#endif

        #region props

        public ComponentKind Kind {
            get => kind;
            set => kind = value;
        }
        public bool Shared {
            get => shared;
            set => shared = value;
        }
        public int MaxCount {
            get => maxCount;
            set => maxCount = value;
        }
        public long EnableFuncs {
            get => enableFuncs;
            set => enableFuncs = value;
        }
        public long Flags {
            get => flags;
            set => flags = value;
        }
        public int GroupKey {
            get => groupKey;
            set => groupKey = value;
        }
        public string MountPath {
            get => mountPath;
            set => mountPath = value;
        }
        public object ExtraInfo {
            get => extraInfo;
            set => extraInfo = value;
        }
        public ComponentId BaseId {
            get => baseId;
            set => baseId = value;
        }

        #endregion
    }
}
}