using System;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 四分量定点数向量，仅用于Dson数据表示。
/// </summary>
public struct FixedVector4 : IEquatable<FixedVector4>
{
    public FixedPoint4 v0;
    public FixedPoint4 v1;
    public FixedPoint4 v2;
    public FixedPoint4 v3;

    public FixedVector4(FixedPoint4 v0, FixedPoint4 v1, FixedPoint4 v2, FixedPoint4 v3 = default) {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    public FixedPoint4 this[int index] {
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

    public bool Equals(FixedVector4 other) {
        return v0.Equals(other.v0) && v1.Equals(other.v1) && v2.Equals(other.v2) && v3.Equals(other.v3);
    }

    public override bool Equals(object? obj) {
        return obj is FixedVector4 other && Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = v0.GetHashCode();
        hashCode = (hashCode * 397) ^ v1.GetHashCode();
        hashCode = (hashCode * 397) ^ v2.GetHashCode();
        hashCode = (hashCode * 397) ^ v3.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(FixedVector4 left, FixedVector4 right) {
        return left.Equals(right);
    }

    public static bool operator !=(FixedVector4 left, FixedVector4 right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{nameof(v0)}: {v0}, {nameof(v1)}: {v1}, {nameof(v2)}: {v2}, {nameof(v3)}: {v3}";
    }
}
}