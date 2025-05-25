package cn.wjybxx.dsonapt;

import com.squareup.javapoet.TypeName;

import javax.annotation.Nullable;
import javax.lang.model.element.ExecutableElement;
import javax.lang.model.element.Modifier;
import javax.lang.model.element.VariableElement;
import java.util.Set;

/**
 * @author wjybxx
 * date - 2025/5/25
 */
public final class AptFieldInfo {

    public final VariableElement element;
    public final ExecutableElement getterMethod;
    public final ExecutableElement setterMethod;
    public final TypeName typeName; // 字段的类型名缓存

    public final String name; // toString缓存
    private final FieldKey fieldKey;

    public AptFieldInfo(VariableElement element,
                        @Nullable ExecutableElement getterMethod,
                        @Nullable ExecutableElement setterMethod, TypeName typeName) {
        this.element = element;
        this.getterMethod = getterMethod;
        this.setterMethod = setterMethod;
        this.typeName = typeName;

        this.name = element.getSimpleName().toString();
        this.fieldKey = new FieldKey(element.getEnclosingElement().getSimpleName().toString(), name);
    }

    public Set<Modifier> getModifiers() {
        return element.getModifiers();
    }

    public FieldKey getFieldKey() {
        return fieldKey;
    }

    /** 是否有public的getter */
    public boolean hasPublicGetter() {
        return getterMethod != null && getterMethod.getModifiers().contains(Modifier.PUBLIC);
    }

    /** 是否有public的setter */
    public boolean hasPublicSetter() {
        return setterMethod != null && setterMethod.getModifiers().contains(Modifier.PUBLIC);
    }

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        AptFieldInfo that = (AptFieldInfo) o;
        return element.equals(that.element);
    }

    @Override
    public int hashCode() {
        return element.hashCode();
    }
    // endregion

    @Override
    public String toString() {
        return element.toString();
    }


}