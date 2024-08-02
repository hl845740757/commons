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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public class DsonLitePointer : DsonValue, IEquatable<DsonLitePointer>
{
    private readonly ObjectLitePtr _value;

    public DsonLitePointer(in ObjectLitePtr value) {
        _value = value;
    }

    public override DsonType DsonType => DsonType.LitePointer;

    public ObjectLitePtr Value => _value;

    #region equals

    public bool Equals(DsonLitePointer? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonLitePointer)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonLitePointer? left, DsonLitePointer? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonLitePointer? left, DsonLitePointer? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(Value)}: {Value}";
    }
}
}