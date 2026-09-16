using System;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public class DsonFxp64 : DsonValue, IEquatable<DsonFxp64>, IComparable<DsonFxp64>, IComparable
{
    private readonly Fxp64 _value;

    public DsonFxp64(Fxp64 value) {
        _value = value;
    }

    public override DsonType DsonType => DsonType.Fxp64;
    public Fxp64 Value => _value;

    #region equals

    public bool Equals(DsonFxp64? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DsonFxp64)obj);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonFxp64? left, DsonFxp64? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonFxp64? left, DsonFxp64? right) {
        return !Equals(left, right);
    }

    public int CompareTo(DsonFxp64? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return _value.CompareTo(other._value);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is DsonFxp64 other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DsonFxp64)}");
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}