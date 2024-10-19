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

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.function.Predicate;
import java.util.function.ToIntFunction;
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

    // region indexOf

    /** 查找对象引用在数组中的索引 */
    public static <T> int indexOf(T[] list, Object element) {
        return indexOf(list, element, 0, list.length);
    }

    /** 反向查找对象引用在数组中的索引 */
    public static <T> int lastIndexOf(T[] list, Object element) {
        return lastIndexOf(list, element, 0, list.length);
    }

    /**
     * @param list    数组
     * @param element 要查找的元素
     * @param start   数组的有效区间起始下标(inclusive)
     * @param end     数组的有效区间结束下标(exclusive)
     */
    public static <T> int indexOf(T[] list, Object element, int start, int end) {
        if (element == null) {
            for (int i = start; i < end; i++) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = start; i < end; i++) {
                if (element.equals(list[i])) {
                    return i;
                }
            }
        }
        return -1;
    }

    /**
     * @param list    数组
     * @param element 要查找的元素
     * @param start   数组的有效区间起始下标(inclusive)
     * @param end     数组的有效区间结束下标(exclusive)
     */
    public static <T> int lastIndexOf(T[] list, Object element, int start, int end) {
        if (element == null) {
            for (int i = end - 1; i >= start; i--) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = end - 1; i >= start; i--) {
                if (element.equals(list[i])) {
                    return i;
                }
            }
        }
        return -1;
    }

    // endregion

    // region index-ref

    /** 判断是否存在给定元素的引用 */
    public static <T> boolean containsRef(T[] list, Object element) {
        return indexOfRef(list, element, 0, list.length) >= 0;
    }

    /** 查找对象引用在数组中的索引 */
    public static <T> int indexOfRef(T[] list, Object element) {
        return indexOfRef(list, element, 0, list.length);
    }

    /** 反向查找对象引用在数组中的索引 */
    public static <T> int lastIndexOfRef(T[] list, Object element) {
        return lastIndexOfRef(list, element, 0, list.length);
    }

    /**
     * @param list    数组
     * @param element 要查找的元素
     * @param start   数组的有效区间起始下标(inclusive)
     * @param end     数组的有效区间结束下标(exclusive)
     */
    public static <T> int indexOfRef(T[] list, Object element, int start, int end) {
        if (element == null) {
            for (int i = start; i < end; i++) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = start; i < end; i++) {
                if (element == list[i]) {
                    return i;
                }
            }
        }
        return -1;
    }

    /**
     * @param list    数组
     * @param element 要查找的元素
     * @param start   数组的有效区间起始下标(inclusive)
     * @param end     数组的有效区间结束下标(exclusive)
     */
    public static <T> int lastIndexOfRef(T[] list, Object element, int start, int end) {
        if (element == null) {
            for (int i = end - 1; i >= start; i--) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = end - 1; i >= start; i--) {
                if (element == list[i]) {
                    return i;
                }
            }
        }
        return -1;
    }

    // endregion

    // region index-custom

    public static <T> boolean containsCustom(T[] list, Predicate<? super T> indexFunc) {
        return indexOfCustom(list, indexFunc, 0, list.length) >= 0;
    }

    public static <T> int indexOfCustom(T[] list, Predicate<? super T> indexFunc) {
        return indexOfCustom(list, indexFunc, 0, list.length);
    }

    public static <T> int lastIndexOfCustom(T[] list, Predicate<? super T> indexFunc) {
        return lastIndexOfCustom(list, indexFunc, 0, list.length);
    }

    /**
     * @param list      数组
     * @param indexFunc 查询函数
     * @param start     数组的有效区间起始下标(inclusive)
     * @param end       数组的有效区间结束下标(exclusive)
     */
    public static <T> int indexOfCustom(T[] list, Predicate<? super T> indexFunc, int start, int end) {
        for (int i = start; i < end; i++) {
            if (indexFunc.test(list[i])) {
                return i;
            }
        }
        return -1;
    }

    /**
     * @param list      数组
     * @param indexFunc 查询函数
     * @param start     数组的有效区间起始下标(inclusive)
     * @param end       数组的有效区间结束下标(exclusive)
     */
    public static <T> int lastIndexOfCustom(T[] list, Predicate<? super T> indexFunc, int start, int end) {
        for (int i = end - 1; i >= start; i--) {
            if (indexFunc.test(list[i])) {
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

    // region binary-search

    /**
     * 如果元素存在，则返回元素对应的下标；
     * 如果元素不存在，则返回(-(insertion point) - 1)
     * 即： (index + 1) * -1 可得应当插入的下标。
     *
     * @param array 数组
     * @param c     比较器
     * @return 元素下标或插入下标
     */
    public static <T> int binarySearch(T[] array, ToIntFunction<? super T> c) {
        return binarySearch0(array, 0, array.length, c);
    }

    /**
     * 如果元素存在，则返回元素对应的下标；
     * 如果元素不存在，则返回(-(insertion point) - 1)
     * 即： (index + 1) * -1 可得应当插入的下标。
     *
     * @param array     数组
     * @param fromIndex 开始索引
     * @param toIndex   结束索引
     * @param c         比较器
     * @return 元素下标或插入下标
     */
    public static <T> int binarySearch(T[] array, int fromIndex, int toIndex, ToIntFunction<? super T> c) {
        rangeCheck(array.length, fromIndex, toIndex);
        return binarySearch0(array, fromIndex, toIndex, c);
    }

    private static <T> int binarySearch0(T[] array, int fromIndex, int toIndex, ToIntFunction<? super T> c) {
        int low = fromIndex;
        int high = toIndex - 1;

        while (low <= high) {
            int mid = (low + high) >> 1;
            T midVal = array[mid];
            int cmp = c.applyAsInt(midVal);
            if (cmp < 0)
                low = mid + 1;
            else if (cmp > 0)
                high = mid - 1;
            else
                return mid; // key found
        }
        return -(low + 1); // key not found.
    }

    static void rangeCheck(int arrayLength, int fromIndex, int toIndex) {
        if (fromIndex > toIndex) {
            throw new IllegalArgumentException(
                    "fromIndex(" + fromIndex + ") > toIndex(" + toIndex + ")");
        }
        if (fromIndex < 0) {
            throw new ArrayIndexOutOfBoundsException(fromIndex);
        }
        if (toIndex > arrayLength) {
            throw new ArrayIndexOutOfBoundsException(toIndex);
        }
    }

    // endregion

    // region other

    public static <E> List<E> asList(E[] array) {
        return Arrays.asList(array);
    }

    public static <E> List<E> asList(final E[] array, int offset, int length) {
        if (offset == 0 && length == array.length) {
            return Arrays.asList(array);
        } else {
            return Arrays.asList(array).subList(offset, offset + length);
        }
    }

    public static <E> ArrayList<E> toList(final E[] array) {
        return new ArrayList<>(new CollectionUtils.ToArrayHelper<>(array, 0, array.length));
    }

    public static <E> ArrayList<E> toList(final E[] array, int offset, int length) {
        // arrayList不支持数组的构造参数，因此无法绕过冗余的拷贝
        return new ArrayList<>(new CollectionUtils.ToArrayHelper<>(array, offset, length));
    }

    public static int[] toIntArray(final List<Integer> list) {
        int[] array = new int[list.size()];
        for (int idx = 0; idx < list.size(); idx++) {
            int val = list.get(idx);
            array[idx] = val;
        }
        return array;
    }

    public static long[] toLongArray(final List<Long> list) {
        long[] array = new long[list.size()];
        for (int idx = 0; idx < list.size(); idx++) {
            long val = list.get(idx);
            array[idx] = val;
        }
        return array;
    }

    public static float[] toFloatArray(final List<Float> list) {
        float[] array = new float[list.size()];
        for (int idx = 0; idx < list.size(); idx++) {
            float val = list.get(idx);
            array[idx] = val;
        }
        return array;
    }

    public static double[] toDoubleArray(final List<Double> list) {
        double[] array = new double[list.size()];
        for (int idx = 0; idx < list.size(); idx++) {
            double val = list.get(idx);
            array[idx] = val;
        }
        return array;
    }

    // endregion

    // region rank

    /** 最大支持9阶 - 我都没见过3阶以上的数组... */
    private static final String[] arrayRankSymbols = {
            "[]",
            "[][]",
            "[][][]",
            "[][][][]",
            "[][][][][]",
            "[][][][][][]",
            "[][][][][][][]",
            "[][][][][][][][]",
            "[][][][][][][][][]"
    };

    public static String arrayRankSymbol(int rank) {
        if (rank < 1 || rank > 9) {
            throw new IllegalArgumentException("rank: " + rank);
        }
        return arrayRankSymbols[rank - 1];
    }

    /** 获取根元素的类型 -- 如果Type是数组，则返回最底层的元素类型；如果不是数组，则返回type */
    public static Class<?> getRootComponentType(Class<?> clz) {
        while (clz.isArray()) {
            clz = clz.getComponentType();
        }
        return clz;
    }

    /** 获取数组的阶数 -- 如果不是数组，则返回0 */
    public static int getArrayRank(Class<?> clz) {
        int r = 0;
        while (clz.isArray()) {
            r++;
            clz = clz.getComponentType();
        }
        return r;
    }
    // endregion
}
