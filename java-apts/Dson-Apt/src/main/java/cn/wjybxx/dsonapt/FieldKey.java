package cn.wjybxx.dsonapt;

/**
 * @author wjybxx
 * date - 2025/5/5
 */
final class FieldKey {

    public final String declaringTypeName;
    public final String fieldName;

    public FieldKey(String declaringTypeName, String fieldName) {
        this.declaringTypeName = declaringTypeName;
        this.fieldName = fieldName;
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        FieldKey fieldKey = (FieldKey) o;
        return declaringTypeName.equals(fieldKey.declaringTypeName) && fieldName.equals(fieldKey.fieldName);
    }

    @Override
    public int hashCode() {
        int result = declaringTypeName.hashCode();
        result = 31 * result + fieldName.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return declaringTypeName + "." + fieldName;
    }
}