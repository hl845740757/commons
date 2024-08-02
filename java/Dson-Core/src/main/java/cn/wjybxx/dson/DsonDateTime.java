package cn.wjybxx.dson;

import cn.wjybxx.dson.types.ExtDateTime;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/6/17
 */
public final class DsonDateTime extends DsonValue {

    private final ExtDateTime value;

    public DsonDateTime(ExtDateTime value) {
        this.value = value;
    }

    public ExtDateTime getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.DATETIME;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonDateTime that = (DsonDateTime) o;

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
