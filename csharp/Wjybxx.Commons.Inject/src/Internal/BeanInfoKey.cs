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

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// <see cref="BeanInfo"/>的键
/// </summary>
internal readonly struct BeanInfoKey : IEquatable<BeanInfoKey>
{
    public readonly int configId;
    public readonly Type implType;

    public BeanInfoKey(int configId, Type implType) {
        this.configId = configId;
        this.implType = implType;
    }

    public bool Equals(BeanInfoKey other) {
        return configId == other.configId && implType == other.implType;
    }

    public override bool Equals(object? obj) {
        return obj is BeanInfoKey other && Equals(other);
    }

    public override int GetHashCode() {
        return (configId * 397) ^ implType.GetHashCode();
    }

    public static bool operator ==(BeanInfoKey left, BeanInfoKey right) {
        return left.Equals(right);
    }

    public static bool operator !=(BeanInfoKey left, BeanInfoKey right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(configId)}: {configId}, {nameof(implType)}: {implType}";
    }
}
}