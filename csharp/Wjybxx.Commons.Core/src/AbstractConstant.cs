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
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons
{
/// <summary>
/// 常量类的模板实现 
/// </summary>
public abstract class AbstractConstant : IConstant
{
    private readonly int _id;
    private readonly string _name;
    private readonly string _poolId;

    protected AbstractConstant(IConstant.Builder builder) {
        _id = builder.GetIdOrThrow();
        _name = builder.Name;
        _poolId = builder.PoolId ?? throw new ArgumentException("PoolId");
    }

    public int Id => _id;
    public string Name => _name;
    public string PoolId => _poolId;

    /// <summary>
    /// 通常不应该覆盖该方法
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return _name;
    }

    #region equals

    /// <summary>
    /// 强制equals为引用相等，禁止重写
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public sealed override bool Equals(object? obj) {
        return ReferenceEquals(this, obj);
    }

    public bool Equals(IConstant? other) {
        return ReferenceEquals(this, other);
    }

    /// <summary>
    /// 不对hashcode做优化 -- 理论上使用_id是合适的。
    /// </summary>
    /// <returns></returns>
    public sealed override int GetHashCode() {
        return RuntimeHelpers.GetHashCode(this);
    }

    public static bool operator ==(AbstractConstant? left, AbstractConstant? right) {
        return ReferenceEquals(left, right);
    }

    public static bool operator !=(AbstractConstant? left, AbstractConstant? right) {
        return !ReferenceEquals(left, right);
    }

    #endregion

    #region compare

    public int CompareTo(IConstant? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        // 注意：
        // 1. 未比较名字也未比较其它信息 - 这可以保证同一个类中定义的常量，其结果与定义顺序相同，就像枚举。
        // 2. uniqueId与类初始化顺序有关，因此无法保证不同类中定义的常量的顺序。
        // 3. 有个例外，超类中定义的常量总是在子类前面，这是因为超类总是在子类之前初始化。
        if (!ReferenceEquals(_poolId, other.PoolId)) {
            int r = string.Compare(_poolId, other.PoolId, StringComparison.Ordinal);
            if (r != 0) {
                return r;
            }
        }
        if (_id < other.Id) {
            return -1;
        }
        if (_id > other.Id) {
            return 1;
        }
        throw new IllegalStateException($"failed to compare two different constants, this: {Name}, that: {other.Name}");
    }

    public static bool operator <(AbstractConstant? left, AbstractConstant? right) {
        return Comparer<AbstractConstant>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(AbstractConstant? left, AbstractConstant? right) {
        return Comparer<AbstractConstant>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(AbstractConstant? left, AbstractConstant? right) {
        return Comparer<AbstractConstant>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(AbstractConstant? left, AbstractConstant? right) {
        return Comparer<AbstractConstant>.Default.Compare(left, right) >= 0;
    }

    #endregion
}
}