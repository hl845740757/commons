using System;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public sealed class DsonLong4 : DsonValue, IEquatable<DsonLong4>
{
    public static readonly DsonLong4 EMPTY = new DsonLong4(default);
    private readonly Long4 _value;

    public DsonLong4(Long4 value) {
        _value = value;
    }

    public override DsonType DsonType => DsonType.Long4;
    public Long4 Value => _value;

    public bool Equals(DsonLong4? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj) {
        return obj is DsonLong4 other && Equals(other);
    }

    public override int GetHashCode() {
        return _value.GetHashCode();
    }

    public static bool operator ==(DsonLong4? left, DsonLong4? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonLong4? left, DsonLong4? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}, {nameof(_value)}: {_value}";
    }
}
}