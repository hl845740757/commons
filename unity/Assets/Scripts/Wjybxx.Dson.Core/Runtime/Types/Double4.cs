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

namespace Wjybxx.Dson.Types
{
/// <summary>
/// Double4
///
/// 注：
/// 1.该数据结构用于特定场景下的性能优化，用于减少内存中的<see cref="DsonObject{TK}"/>数量。
/// 2.使用Object格式输入时，必须顺序输入，name会被忽略。
/// </summary>
public struct Double4 : IEquatable<Double4>
{
    public double v0;
    public double v1;
    public double v2;
    public double v3;

    public Double4(double v0, double v1, double v2, double v3 = 0) {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    public double this[int index] {
        get {
            return index switch
            {
                0 => v0,
                1 => v1,
                2 => v2,
                3 => v3,
                _ => throw new IndexOutOfRangeException()
            };
        }
        set {
            switch (index) {
                case 0: v0 = value; break;
                case 1: v1 = value; break;
                case 2: v2 = value; break;
                case 3: v3 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    #region 运算符

    public static Double4 operator +(Double4 lhs, Double4 rhs) {
        return new Double4(
            lhs.v0 + rhs.v0,
            lhs.v1 + rhs.v1,
            lhs.v2 + rhs.v2,
            lhs.v3 + rhs.v3);
    }

    public static Double4 operator -(Double4 lhs, Double4 rhs) {
        return new Double4(
            lhs.v0 - rhs.v0,
            lhs.v1 - rhs.v1,
            lhs.v2 - rhs.v2,
            lhs.v3 - rhs.v3);
    }

    public static Double4 operator *(Double4 lhs, Double4 rhs) {
        return new Double4(
            lhs.v0 * rhs.v0,
            lhs.v1 * rhs.v1,
            lhs.v2 * rhs.v2,
            lhs.v3 * rhs.v3);
    }

    public static Double4 operator /(Double4 lhs, Double4 rhs) {
        return new Double4(
            lhs.v0 / rhs.v0,
            lhs.v1 / rhs.v1,
            lhs.v2 / rhs.v2,
            lhs.v3 / rhs.v3);
    }

    public static Double4 operator %(Double4 lhs, Double4 rhs) {
        return new Double4(
            lhs.v0 % rhs.v0,
            lhs.v1 % rhs.v1,
            lhs.v2 % rhs.v2,
            lhs.v3 % rhs.v3);
    }

    #endregion

    #region equals

    public bool Equals(Double4 other) {
        return v0.Equals(other.v0) && v1.Equals(other.v1) && v2.Equals(other.v2) && v3.Equals(other.v3);
    }

    public override bool Equals(object? obj) {
        return obj is Double4 other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = v0.GetHashCode();
        hashCode = (hashCode * 397) ^ v1.GetHashCode();
        hashCode = (hashCode * 397) ^ v2.GetHashCode();
        hashCode = (hashCode * 397) ^ v3.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(Double4 left, Double4 right) {
        return left.Equals(right);
    }

    public static bool operator !=(Double4 left, Double4 right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(v0)}: {v0}, {nameof(v1)}: {v1}, {nameof(v2)}: {v2}, {nameof(v3)}: {v3}";
    }

    #endregion
}
}