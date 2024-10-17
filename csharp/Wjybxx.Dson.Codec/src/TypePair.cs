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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// TypePair
/// </summary>
public readonly struct TypePair : IEquatable<TypePair>
{
    private readonly Type first;
    private readonly Type second;

    public TypePair(Type first, Type second) {
        this.first = first ?? throw new ArgumentNullException(nameof(first));
        this.second = second ?? throw new ArgumentNullException(nameof(second));
    }

    public bool Equals(TypePair other) {
        return first == other.first
               && second == other.second;
    }

    public override bool Equals(object? obj) {
        return obj is TypePair other && Equals(other);
    }

    public override int GetHashCode() {
        return first.GetHashCode() * 31 + second.GetHashCode();
    }

    public static bool operator ==(TypePair left, TypePair right) {
        return left.Equals(right);
    }

    public static bool operator !=(TypePair left, TypePair right) {
        return !left.Equals(right);
    }
}
}