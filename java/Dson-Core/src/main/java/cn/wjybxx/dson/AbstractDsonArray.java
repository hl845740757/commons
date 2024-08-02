package cn.wjybxx.dson;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.function.Consumer;
import java.util.function.IntFunction;
import java.util.function.Predicate;
import java.util.function.UnaryOperator;

/**
 * @author wjybxx
 * date - 2023/6/18
 */
public abstract class AbstractDsonArray extends DsonValue implements List<DsonValue> {

    final List<DsonValue> values;

    AbstractDsonArray(List<DsonValue> values) {
        Objects.requireNonNull(values);
        this.values = values;
    }

    public AbstractDsonArray append(DsonValue dsonValue) {
        add(dsonValue);
        return this;
    }

    // region equals
    // equals默认只比较values

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        return o instanceof AbstractDsonArray that && values.equals(that.values);
    }

    @Override
    public int hashCode() {
        return values.hashCode();
    }

    @Override
    public String toString() {
        return getClass().getSimpleName() + "{" +
                "values=" + values +
                '}';
    }
    // endregion

    // region 安全检查
    static void checkElement(DsonValue value) {
        if (value == null) throw new IllegalArgumentException("value cant be null");
        if (value.getDsonType() == DsonType.HEADER) throw new IllegalArgumentException("add Header");
    }

    @Override
    public DsonValue set(int index, DsonValue element) {
        checkElement(element);
        return values.set(index, element);
    }

    @Override
    public void add(int index, DsonValue element) {
        checkElement(element);
        values.add(index, element);
    }

    @Override
    public boolean add(DsonValue dsonValue) {
        checkElement(dsonValue);
        return values.add(dsonValue);
    }

    @Override
    public boolean addAll(Collection<? extends DsonValue> c) {
        c.forEach(AbstractDsonArray::checkElement);
        return values.addAll(c);
    }

    @Override
    public boolean addAll(int index, Collection<? extends DsonValue> c) {
        c.forEach(AbstractDsonArray::checkElement);
        return values.addAll(index, c);
    }

    public DsonValue removeAt(int index) {
        return values.remove(index);
    }

    // endregion

    // region 代理实现

    @Override
    public int size() {
        return values.size();
    }

    @Override
    public boolean isEmpty() {
        return values.isEmpty();
    }

    @Override
    public boolean contains(Object o) {
        return values.contains(o);
    }

    @Override
    public boolean remove(Object o) {
        return values.remove(o);
    }

    @Override
    public boolean containsAll(Collection<?> c) {
        return values.containsAll(c);
    }

    @Override
    public boolean removeAll(Collection<?> c) {
        return values.removeAll(c);
    }

    @Override
    public boolean retainAll(Collection<?> c) {
        return values.retainAll(c);
    }

    @Override
    public void replaceAll(UnaryOperator<DsonValue> operator) {
        values.replaceAll(operator); // TODO 缺少结果校验
    }

    @Override
    public void sort(Comparator<? super DsonValue> c) {
        values.sort(c);
    }

    @Override
    public void clear() {
        values.clear();
    }

    @Override
    public DsonValue get(int index) {
        return values.get(index);
    }

    @Override
    public DsonValue remove(int index) {
        return values.remove(index);
    }

    @Override
    public int indexOf(Object o) {
        return values.indexOf(o);
    }

    @Override
    public int lastIndexOf(Object o) {
        return values.lastIndexOf(o);
    }

    @Nonnull
    @Override
    public Iterator<DsonValue> iterator() {
        return values.iterator();
    }

    @Nonnull
    @Override
    public ListIterator<DsonValue> listIterator() {
        return values.listIterator(); // TODO 缺少Set和Add校验
    }

    @Nonnull
    @Override
    public ListIterator<DsonValue> listIterator(int index) {
        return values.listIterator(index);
    }

    @Nonnull
    @Override
    public List<DsonValue> subList(int fromIndex, int toIndex) {
        return values.subList(fromIndex, toIndex);
    }

    @Override
    public Spliterator<DsonValue> spliterator() {
        return values.spliterator();
    }

    @Nonnull
    @Override
    public Object[] toArray() {
        return values.toArray();
    }

    @Nonnull
    @Override
    public <T> T[] toArray(@Nonnull T[] a) {
        return values.toArray(a);
    }

    @Override
    public <T> T[] toArray(IntFunction<T[]> generator) {
        return values.toArray(generator);
    }

    @Override
    public boolean removeIf(Predicate<? super DsonValue> filter) {
        return values.removeIf(filter);
    }

    @Override
    public void forEach(Consumer<? super DsonValue> action) {
        values.forEach(action);
    }

    // endregion
}