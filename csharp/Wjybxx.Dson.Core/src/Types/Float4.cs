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
/// Float4
///
/// 注：
/// 1.该数据结构用于特定场景下的性能优化，用于减少内存中的<see cref="DsonObject{TK}"/>数量。
/// 2.使用Object格式输入时，必须顺序输入，name会被忽略。
/// </summary>
public struct Float4 : IEquatable<Float4>
{
    public float v0;
    public float v1;
    public float v2;
    public float v3;

    public Float4(float v0, float v1, float v2, float v3 = 0) {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    public float this[int index] {
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

    #region quals

    public bool Equals(Float4 other) {
        return v0.Equals(other.v0) && v1.Equals(other.v1) && v2.Equals(other.v2) && v3.Equals(other.v3);
    }

    public override bool Equals(object? obj) {
        return obj is Float4 other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = v0.GetHashCode();
        hashCode = (hashCode * 397) ^ v1.GetHashCode();
        hashCode = (hashCode * 397) ^ v2.GetHashCode();
        hashCode = (hashCode * 397) ^ v3.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(Float4 left, Float4 right) {
        return left.Equals(right);
    }

    public static bool operator !=(Float4 left, Float4 right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(v0)}: {v0}, {nameof(v1)}: {v1}, {nameof(v2)}: {v2}, {nameof(v3)}: {v3}";
    }

    #endregion
}
}