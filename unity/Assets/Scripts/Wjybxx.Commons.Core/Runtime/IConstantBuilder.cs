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

namespace Wjybxx.Commons
{
/// <summary>
/// 该文件定义Builder
/// </summary>
public partial interface IConstant
{
    /// <summary>
    /// 接口用于约定数据，实际构建时使用的都是<see cref="Builder{T}"/>的子类
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// 常量的id
        /// </summary>
        public int? Id { get; }

        /// <summary>
        /// 常量的名字
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 声明常量的池
        /// </summary>
        public string? PoolId { get; }

        /// <summary>
        /// 获取分配的高速缓存索引 -- -1表示未设置。
        /// 注意：<see cref="ConstantPool{T}"/>仅仅分配index，而真正的实现在于常量的使用者。
        /// </summary>
        public int CacheIndex { get; set; }

        /// <summary>
        /// 设置是否需要分配高速缓存索引
        /// </summary>
        public bool RequireCacheIndex { get; set; }

        /// <summary>
        /// 构建最终的常量
        /// </summary>
        /// <returns></returns>
        protected internal IConstant Build();

        /// <summary>
        /// 获取常量的id，如果未设置则抛出异常
        /// </summary>
        /// <returns></returns>
        public int GetIdOrThrow();
    }

    public abstract class Builder<T> : IBuilder where T : IConstant
    {
        private int? _id;
        private readonly string _name;
        private string? _poolId;

        private int _cacheIndex = -1;
        private bool requireCacheIndex;

        protected Builder(string name) {
            _name = CheckName(name);
        }

        /// <summary>
        /// 设置常量的id - id通常由管理常量的常量池分配
        /// </summary>
        /// <param name="poolId">声明常量的池</param>
        /// <param name="id">分配的常量id</param>
        internal void SetId(string poolId, int id) {
            if (_id.HasValue) {
                throw new InvalidOperationException("id cannot be initialized repeatedly");
            }
            _id = id;
            _poolId = poolId;
        }

        public int GetIdOrThrow() {
            if (_id.HasValue) {
                return _id.Value;
            }
            throw new InvalidOperationException("id has not been initialized");
        }

        /// <summary>
        /// 构建常量对象
        /// 注意：该接口不是用户调用的，因此权限不是public
        /// </summary>
        /// <returns></returns>
        protected internal abstract T Build();

        /** 接口适配 */
        IConstant IBuilder.Build() {
            return Build();
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
        /// 声明常量的池
        /// </summary>
        public string? PoolId => _poolId;

        /// <summary>
        /// 获取分配的高速缓存索引 -- -1表示未设置。
        /// 注意：<see cref="ConstantPool{T}"/>仅仅分配index，而真正的实现在于常量的使用者。
        /// </summary>
        public int CacheIndex {
            get => _cacheIndex;
            set => _cacheIndex = value;
        }

        /// <summary>
        /// 设置是否需要分配高速缓存索引
        /// </summary>
        public bool RequireCacheIndex {
            get => requireCacheIndex;
            set => requireCacheIndex = value;
        }
    }
}
}