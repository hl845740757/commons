/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.base.io;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.lang.reflect.Array;
import java.util.Arrays;
import java.util.Comparator;
import java.util.TreeSet;
import java.util.function.Consumer;

/**
 * 简单数组池实现
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@NotThreadSafe
public final class SimpleArrayPool<T> implements ArrayPool<T> {

    private final Class<T> arrayType;
    private final int poolSize;
    private final int initCapacity;
    private final int maxCapacity;
    private final boolean clear;

    private final TreeSet<Node<T>> freeArrays;
    private final Consumer<T> clearHandler;

    /**
     * @param arrayType    数组类型
     * @param poolSize     池大小
     * @param initCapacity 数组初始大小
     * @param maxCapacity  数组最大大小 -- 超过大小的数组不会放入池中
     */
    public SimpleArrayPool(Class<T> arrayType, int poolSize, int initCapacity, int maxCapacity) {
        this(arrayType, poolSize, initCapacity, maxCapacity, false);
    }

    /**
     * @param arrayType    数组类型
     * @param poolSize     池大小
     * @param initCapacity 数组初始大小
     * @param maxCapacity  数组最大大小 -- 超过大小的数组不会放入池中
     * @param clear        是否清理归还到池中数组
     */
    public SimpleArrayPool(Class<T> arrayType, int poolSize, int initCapacity, int maxCapacity, boolean clear) {
        if (arrayType.getComponentType() == null) {
            throw new IllegalArgumentException("arrayType");
        }
        if (poolSize < 0 || initCapacity < 0 || maxCapacity < 0) {
            throw new IllegalArgumentException();
        }
        this.arrayType = arrayType;
        this.poolSize = poolSize;
        this.initCapacity = initCapacity;
        this.maxCapacity = maxCapacity;
        this.clear = clear;

        this.clearHandler = findClearHandler(arrayType);
        this.freeArrays = new TreeSet<>(COMPARATOR);
    }

    @SuppressWarnings("unchecked")
    @Nonnull
    @Override
    public T rent() {
        Node<T> minNode = freeArrays.pollFirst();
        if (minNode != null) {
            return minNode.array();
        }
        return (T) Array.newInstance(arrayType.getComponentType(), initCapacity);
    }

    @SuppressWarnings("unchecked")
    @Override
    public T rent(int minimumLength) {
        Node<T> ceilingNode = freeArrays.ceiling(new LengthNode<>(minimumLength));
        if (ceilingNode != null) {
            return ceilingNode.array();
        }
        return (T) Array.newInstance(arrayType.getComponentType(), minimumLength);
    }

    @Override
    public void returnOne(T array) {
        returnOneImpl(array, this.clear);
    }

    @Override
    public void returnOne(T array, boolean clear) {
        returnOneImpl(array, this.clear || clear);
    }

    private void returnOneImpl(T array, boolean clear) {
        int length = Array.getLength(array);
        if (freeArrays.size() < poolSize && length <= maxCapacity) {
            if (clear) {
                clearHandler.accept(array);
            }
            freeArrays.add(new ArrayNode<>(array, length));
        }
    }

    @Override
    public void freeAll() {
        freeArrays.clear();
    }

    // region node

    private static final Comparator<Node<?>> COMPARATOR = Comparator.comparingInt(Node::length);

    private interface Node<T> {

        T array();

        int length();
    }

    private static class ArrayNode<T> implements Node<T> {

        final T array;
        final int length;

        /** @param length 缓存下来以避免反射调用 */
        private ArrayNode(T array, int length) {
            this.array = array;
            this.length = length;
        }

        @Override
        public T array() {
            return array;
        }

        @Override
        public int length() {
            return length;
        }
    }

    private static class LengthNode<T> implements Node<T> {

        final int length;

        public LengthNode(int length) {
            this.length = length;
        }

        @Override
        public T array() {
            throw new IllegalStateException();
        }

        @Override
        public int length() {
            return length;
        }
    }
    // endregion

    // region clear handle

    @SuppressWarnings("unchecked")
    public Consumer<T> findClearHandler(Class<T> arrayType) {
        Class<?> componentType = arrayType.getComponentType();
        if (!componentType.isPrimitive()) {
            return (Consumer<T>) clear_objectArray;
        }
        if (componentType == byte.class) {
            return (Consumer<T>) clear_byteArray;
        }
        if (componentType == char.class) {
            return (Consumer<T>) clear_charArray;
        }
        if (componentType == int.class) {
            return (Consumer<T>) clear_intArray;
        }
        if (componentType == long.class) {
            return (Consumer<T>) clear_longArray;
        }
        if (componentType == float.class) {
            return (Consumer<T>) clear_floatArray;
        }
        if (componentType == double.class) {
            return (Consumer<T>) clear_doubleArray;
        }
        if (componentType == short.class) {
            return (Consumer<T>) clear_shortArray;
        }
        if (componentType == boolean.class) {
            return (Consumer<T>) clear_boolArray;
        }
        throw new IllegalArgumentException("Unsupported arrayType: " + arrayType.getSimpleName());
    }

    private static final Consumer<Object> clear_objectArray = array -> Arrays.fill((Object[]) array, null);
    private static final Consumer<Object> clear_byteArray = array -> Arrays.fill((byte[]) array, (byte) 0);
    private static final Consumer<Object> clear_charArray = array -> Arrays.fill((char[]) array, (char) 0);

    private static final Consumer<Object> clear_intArray = array -> Arrays.fill((int[]) array, 0);
    private static final Consumer<Object> clear_longArray = array -> Arrays.fill((long[]) array, 0);
    private static final Consumer<Object> clear_floatArray = array -> Arrays.fill((float[]) array, 0);
    private static final Consumer<Object> clear_doubleArray = array -> Arrays.fill((double[]) array, 0);

    private static final Consumer<Object> clear_shortArray = array -> Arrays.fill((short[]) array, (short) 0);
    private static final Consumer<Object> clear_boolArray = array -> Arrays.fill((boolean[]) array, false);

    // endregion
}