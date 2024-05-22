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

import java.util.Arrays;
import java.util.Comparator;
import java.util.function.Consumer;

/**
 * 数组池的公共逻辑
 *
 * @author wjybxx
 * date - 2024/5/22
 */
class ArrayPoolCore {

    public static final Comparator<Node<?>> COMPARATOR = Comparator.comparingInt(Node::length);
    private static final Consumer<Object> clear_objectArray = array -> Arrays.fill((Object[]) array, null);
    private static final Consumer<Object> clear_charArray = array -> Arrays.fill((char[]) array, (char) 0);
    private static final Consumer<Object> clear_intArray = array -> Arrays.fill((int[]) array, 0);
    private static final Consumer<Object> clear_longArray = array -> Arrays.fill((long[]) array, 0);
    private static final Consumer<Object> clear_floatArray = array -> Arrays.fill((float[]) array, 0);
    private static final Consumer<Object> clear_doubleArray = array -> Arrays.fill((double[]) array, 0);
    private static final Consumer<Object> clear_shortArray = array -> Arrays.fill((short[]) array, (short) 0);
    private static final Consumer<Object> clear_boolArray = array -> Arrays.fill((boolean[]) array, false);
    private static final Consumer<Object> clear_byteArray = array -> Arrays.fill((byte[]) array, (byte) 0);

    /** 是否是引用类型数组 */
    public static boolean isRefArray(Class<?> arrayType) {
        return !arrayType.getComponentType().isPrimitive();
    }

    @SuppressWarnings("unchecked")
    public static <T> Consumer<T> findClearHandler(Class<T> arrayType) {
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

    interface Node<T> {

        T array();

        int length();
    }

    public static class ArrayNode<T> implements Node<T> {

        final T array;
        final int length;

        /** @param length 缓存下来以避免反射调用 */
        public ArrayNode(T array, int length) {
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

    public static class LengthNode<T> implements Node<T> {

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
}
