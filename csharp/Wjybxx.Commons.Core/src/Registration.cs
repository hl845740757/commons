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

namespace Wjybxx.Commons
{
/// <summary>
/// 默认的关闭句柄
/// </summary>
public readonly struct Registration : IRegistration, IEquatable<Registration>
{
    public static Registration Closed => new Registration();

    /// <summary>
    /// 真正的资源
    /// </summary>
    private readonly IPooledDisposable? _res;
    /// <summary>
    /// 资源的重入id（版本id）
    /// </summary>
    private readonly int _rid;

    public Registration(IPooledDisposable? res, int rid) {
        _res = res;
        _rid = rid;
    }

    public void Dispose() {
        _res?.Dispose(_rid);
    }

    public bool Equals(Registration other) {
        return _rid == other._rid && _res == other._res; // 引用相等
    }

    public override bool Equals(object? obj) {
        return obj is Registration other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            return ((_res != null ? _res.GetHashCode() : 0) * 397) ^ _rid;
        }
    }

    public static bool operator ==(Registration left, Registration right) {
        return left.Equals(right);
    }

    public static bool operator !=(Registration left, Registration right) {
        return !left.Equals(right);
    }
}
}