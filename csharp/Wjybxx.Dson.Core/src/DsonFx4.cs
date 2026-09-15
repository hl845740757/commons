using System;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public class DsonFx4 : DsonValue, IEquatable<DsonFx4>, IComparable<DsonFx4>, IComparable
{
    private readonly FixedPoint4 _value;

    public DsonFx4(FixedPoint4 value) {
        _value = value;
    }

    public override DsonType DsonType => DsonType.FixedPoint4;
    public FixedPoint4 Value => _value;

    #region equals

    public bool Equals(DsonFx4? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DsonFx4)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonFx4? left, DsonFx4? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonFx4? left, DsonFx4? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonFx4? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonFx4 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonFx4)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}