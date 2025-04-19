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
/// 服务配置的键
/// </summary>
public readonly struct ServiceKey : IEquatable<ServiceKey>
{
    public readonly Type serviceType;
    public readonly string? serviceName;

    public ServiceKey(Type serviceType, string? serviceName) {
        this.serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        this.serviceName = serviceName;
    }

    public bool Equals(ServiceKey other) {
        return serviceType == other.serviceType && serviceName == other.serviceName;
    }

    public override bool Equals(object? obj) {
        return obj is ServiceKey other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            return (serviceType.GetHashCode() * 397) ^ (serviceName != null ? serviceName.GetHashCode() : 0);
        }
    }

    public static bool operator ==(ServiceKey left, ServiceKey right) {
        return left.Equals(right);
    }

    public static bool operator !=(ServiceKey left, ServiceKey right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(serviceType)}: {serviceType}, {nameof(serviceName)}: {serviceName}";
    }
}
}