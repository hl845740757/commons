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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// Record的成员
/// </summary>
public readonly struct RecordElement : IEquatable<RecordElement>
{
    public readonly TypeName type;
    public readonly string? name;

    public RecordElement(TypeName type, string? name = null) {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.name = name;
    }

    public RecordElement RemoveNullableAttribute() {
        return new RecordElement(type.RemoveAllNullableAttribute(), name);
    }

    public bool Equals(RecordElement other) {
        return type.Equals(other.type) && name == other.name;
    }

    public override bool Equals(object? obj) {
        return obj is RecordElement other && Equals(other);
    }

    public override int GetHashCode() {
        return (type.GetHashCode() * 397) ^ (name != null ? name.GetHashCode() : 0);
    }

    public override string ToString() {
        return $"{nameof(type)}: {type}, {nameof(name)}: {name}";
    }
}
}