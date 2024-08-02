package cn.wjybxx.dson;

import cn.wjybxx.dson.types.Timestamp;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/6/17
 */
public final class DsonTimestamp extends DsonValue {

    private final Timestamp value;

    public DsonTimestamp(Timestamp value) {
        this.value = value;
    }

    public Timestamp getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.TIMESTAMP;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonTimestamp that = (DsonTimestamp) o;

        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonDateTime{" +
                "value=" + value +
                '}';
    }
}
