using System;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public sealed class DsonFxp4 : DsonValue, IEquatable<DsonFxp4>
{
    public static readonly DsonFxp4 EMPTY = new DsonFxp4(default);
    private readonly Fxp4 _value;

    public DsonFxp4(Fxp4 value) {
        _value = value;
    }

    public override DsonType DsonType => DsonType.Fxp4;
    public Fxp4 Value => _value;

    public bool Equals(DsonFxp4? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        return obj is DsonFxp4 other && Equals(other);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonFxp4? left, DsonFxp4? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonFxp4? left, DsonFxp4? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}