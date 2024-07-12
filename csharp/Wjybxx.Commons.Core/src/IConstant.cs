#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

#pragma warning disable CS1591

namespace Wjybxx.Commons;

/// <summary>
/// 常量对象
///
/// 1. 这里的常量的多例的特殊情况，每一个Constant都代表一个特殊的实例。
/// 2. 常量对象根据引用判断相等性。
/// 3. C#不实现为自循环泛型，存在诸多不便。
/// </summary>
public interface IConstant : IComparable<IConstant>, IEquatable<IConstant>
{
    /// <summary>
    /// 常量对象在对应池中的id
    /// （非全局id）
    /// </summary>
    int Id { get; }

    /// <summary>
    /// 常量对象的名字
    /// </summary>
    string Name { get; }

    #region builder

    /// <summary>
    /// 常量对象构建器
    /// </summary>
    public abstract class Builder
    {
        private int? _id;
        private readonly string _name;

        private int cacheIndex = -1;
        private bool requireCacheIndex;

        public Builder(string name) {
            _name = CheckName(name);
        }

        /// <summary>
        /// 设置常量的id - id通常由管理常量的常量池分配
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="IllegalStateException"></exception>
        public void SetId(int? value) {
            if (_id.HasValue) {
                throw new IllegalStateException("id cannot be initialized repeatedly");
            }
            _id = value;
        }

        public int GetIdOrThrow() {
            if (_id.HasValue) {
                return _id.Value;
            }
            throw new IllegalStateException("id has not been initialized");
        }

        /** 设置高速缓存索引 -- 该方法由{@link ConstantPool}调用 */
        public Builder SetCacheIndex(int cacheIndex) {
            this.cacheIndex = cacheIndex;
            return this;
        }

        /** 设置是否需要分配高速缓存索引 */
        public Builder SetRequireCacheIndex(bool requireCacheIndex) {
            this.requireCacheIndex = requireCacheIndex;
            return this;
        }

        /// <summary>
        /// 常量的id
        /// </summary>
        public int? Id => _id;

        /// <summary>
        /// 常量的名字
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// 获取分配的高速缓存索引 -- -1表示未设置。
        /// 注意：<see cref="ConstantPool{T}"/>仅仅分配index，而真正的实现在于常量的使用者。
        /// </summary>
        public int CacheIndex => cacheIndex;
        public bool RequireCacheIndex => requireCacheIndex;

        /// <summary>
        /// 构建常量对象
        /// </summary>
        /// <returns></returns>
        public abstract IConstant Build();
    }

    /// <summary>
    /// 检查常量名字的合法性
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static string CheckName(string name) {
        if (string.IsNullOrEmpty(name)) {
            throw new ArgumentNullException(nameof(name));
        }
        return name;
    }

    #endregion
}