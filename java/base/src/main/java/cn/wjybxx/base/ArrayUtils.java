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

package cn.wjybxx.base;

import java.util.Objects;
import java.util.random.RandomGenerator;

/**
 * 数组工具类
 *
 * @author wjybxx
 * date - 2024/7/19
 */
@SuppressWarnings("unused")
public class ArrayUtils {

    public static final int INDEX_NOT_FOUND = -1;

    public static final byte[] EMPTY_BYTE_ARRAY = {};
    public static final int[] EMPTY_INT_ARRAY = {};
    public static final long[] EMPTY_LONG_ARRAY = {};
    public static final float[] EMPTY_FLOAT_ARRAY = {};
    public static final double[] EMPTY_DOUBLE_ARRAY = {};
    public static final boolean[] EMPTY_BOOLEAN_ARRAY = {};
    public static final String[] EMPTY_STRING_ARRAY = {};
    public static final Object[] EMPTY_OBJECT_ARRAY = {};
    public static final Class<?>[] EMPTY_CLASS_ARRAY = {};

    // region ref

    /** 判断是否存在给定元素的引用 */
    public static <T> boolean containsRef(T[] list, Object element) {
        return indexOfRef(list, element, 0) >= 0;
    }

    /** 查找对象引用在数组中的索引 */
    public static <T> int indexOfRef(T[] list, Object element) {
        return indexOfRef(list, element, 0);
    }

    /**
     * 查找对象引用在数组中的索引
     *
     * @param element    要查找的元素
     * @param startIndex 开始下标
     */
    public static <T> int indexOfRef(T[] list, Object element, int startIndex) {
        Objects.requireNonNull(list, "list");
        if (startIndex >= list.length) {
            return CollectionUtils.INDEX_NOT_FOUND;
        }
        if (startIndex < 0) {
            startIndex = 0;
        }
        for (int i = startIndex, size = list.length; i < size; i++) {
            if (list[i] == element) {
                return i;
            }
        }
        return CollectionUtils.INDEX_NOT_FOUND;
    }

    /** 反向查找对象引用在数组中的索引 */
    public static <T> int lastIndexOfRef(T[] list, Object element) {
        return lastIndexOfRef(list, element, Integer.MAX_VALUE);
    }

    /**
     * 反向查找对象引用在数组中的索引
     *
     * @param element    要查找的元素
     * @param startIndex 开始下标
     */
    public static <T> int lastIndexOfRef(T[] list, Object element, int startIndex) {
        Objects.requireNonNull(list, "list");
        if (startIndex < 0) {
            return CollectionUtils.INDEX_NOT_FOUND;
        }
        if (startIndex >= list.length) {
            startIndex = list.length - 1;
        }
        for (int i = startIndex; i >= 0; i--) {
            if (list[i] == element) {
                return i;
            }
        }
        return -1;
    }
    // endregion

    // region swap

    public static void swap(int[] array, int i, int j) {
        int value = array[i];
        array[i] = array[j];
        array[j] = value;
    }

    public static void swap(long[] array, int i, int j) {
        long value = array[i];
        array[i] = array[j];
        array[j] = value;
    }

    public static void swap(float[] array, int i, int j) {
        float value = array[i];
        array[i] = array[j];
        array[j] = value;
    }

    public static void swap(double[] array, int i, int j) {
        double value = array[i];
        array[i] = array[j];
        array[j] = value;
    }

    public static void swap(Object[] array, int i, int j) {
        Object value = array[i];
        array[i] = array[j];
        array[j] = value;
    }

    // endregion

    // region shuffle

    public static void shuffle(int[] array) {
        RandomGenerator rnd = RandomGenerator.getDefault();
        for (int i = array.length; i > 1; i--) {
            swap(array, i - 1, rnd.nextInt(i));
        }
    }

    public static void shuffle(long[] array) {
        RandomGenerator rnd = RandomGenerator.getDefault();
        for (int i = array.length; i > 1; i--) {
            swap(array, i - 1, rnd.nextInt(i));
        }
    }

    public static void shuffle(float[] array) {
        RandomGenerator rnd = RandomGenerator.getDefault();
        for (int i = array.length; i > 1; i--) {
            swap(array, i - 1, rnd.nextInt(i));
        }
    }

    public static void shuffle(double[] array) {
        RandomGenerator rnd = RandomGenerator.getDefault();
        for (int i = array.length; i > 1; i--) {
            swap(array, i - 1, rnd.nextInt(i));
        }
    }

    public static void shuffle(Object[] array) {
        RandomGenerator rnd = RandomGenerator.getDefault();
        for (int i = array.length; i > 1; i--) {
            swap(array, i - 1, rnd.nextInt(i));
        }
    }

    // endregion
}
