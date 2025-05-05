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

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 字段映射的键
/// </summary>
internal readonly struct FieldKey : IEquatable<FieldKey>
{
    public readonly string declaredTypeName;
    public readonly string fieldName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaredTypeName">声明字段的类的类名，不包含泛型参数个数信息</param>
    /// <param name="fieldName">字段的名字</param>
    public FieldKey(string declaredTypeName, string fieldName) {
        this.declaredTypeName = declaredTypeName;
        this.fieldName = fieldName;
    }

    #region equals

    public bool Equals(FieldKey other) {
        return declaredTypeName == other.declaredTypeName && fieldName == other.fieldName;
    }

    public override bool Equals(object? obj) {
        return obj is FieldKey other && Equals(other);
    }

    public override int GetHashCode() {
        return (declaredTypeName.GetHashCode() * 397) ^ fieldName.GetHashCode();
    }

    public static bool operator ==(FieldKey left, FieldKey right) {
        return left.Equals(right);
    }

    public static bool operator !=(FieldKey left, FieldKey right) {
        return !left.Equals(right);
    }

    #endregion

    /// <summary>
    /// Dson注解中也使用该格式的字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return declaredTypeName + "." + fieldName;
    }
}
}