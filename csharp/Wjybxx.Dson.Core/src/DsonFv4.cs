using System;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public sealed class DsonFv4 : DsonValue, IEquatable<DsonFv4>
{
    public static readonly DsonFv4 EMPTY = new DsonFv4(default);
    private readonly FixedVector4 _value;

    public DsonFv4(FixedVector4 value) {
        _value = value;
    }

    public override DsonType DsonType => DsonType.FixedVector4;
    public FixedVector4 Value => _value;

    public bool Equals(DsonFv4? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        return obj is DsonFv4 other && Equals(other);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonFv4? left, DsonFv4? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonFv4? left, DsonFv4? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}