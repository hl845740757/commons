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

import cn.wjybxx.base.MathCommon;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.function.Consumer;

/**
 * 数组池的公共逻辑
 *
 * @author wjybxx
 * date - 2024/5/22
 */
final class ArrayPoolCore {

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

    /**
     * @param defCapacity 默认空间
     * @param maxCapacity 最大空间
     * @param growFactor  数组空间成长系数
     * @return 空间信息
     */
    public static int[] calArrayCapacities(int defCapacity, int maxCapacity, double growFactor) {
        if (defCapacity < 0 || maxCapacity < defCapacity) {
            throw new IllegalArgumentException("defCapacity: %d, maxCapacity: %d".formatted(defCapacity, maxCapacity));
        }
        growFactor = Math.max(1.25d, growFactor); // 数组长度不可以变小

        List<Integer> capacityList = new ArrayList<>();
        capacityList.add(defCapacity);

        long capacity = defCapacity;
        while (capacity < maxCapacity) {
            capacity = (long) Math.min(maxCapacity, capacity * growFactor);
            capacityList.add((int) capacity);
        }
        return capacityList.stream().mapToInt(e -> e).toArray();
    }

    /**
     * @param bucketCount       块数量
     * @param firstBucketLength 第一个块大小
     * @param growFactor        成长系数
     * @return 每个bucket的长度
     */
    public static int[] calArrayCacheCounts(int bucketCount, int firstBucketLength, double growFactor) {
        if (firstBucketLength < 0) throw new IllegalArgumentException("firstBucketLength");
        if (growFactor < 0) throw new IllegalArgumentException("growFactor");
        // bucket的长度可以变小
        int[] result = new int[bucketCount];
        result[0] = firstBucketLength;
        for (int idx = 1; idx < bucketCount; idx++) {
            long expectedLength = (long) (result[idx - 1] * growFactor);
            result[idx] = Math.clamp(expectedLength, 0, MathCommon.MAX_POWER_OF_TWO);
        }
        return result;
    }

    /**
     * @param bucketInfo      用户设置的
     * @param arrayCapacities 数组空间信息
     * @param cacheCounts     数组缓存数量信息
     */
    public static void initArrayCapacityAndCacheCounts(List<ArrayBucketConfig> bucketInfo,
                                                       int[] arrayCapacities, int[] cacheCounts) {
        if (bucketInfo.size() != arrayCapacities.length || bucketInfo.size() != cacheCounts.length) {
            throw new IllegalArgumentException();
        }
        for (int i = 0; i < bucketInfo.size(); i++) {
            ArrayBucketConfig bucketConfig = bucketInfo.get(i);
            int arrayCapacity = bucketConfig.getArrayCapacity();
            int cacheCount = bucketConfig.getCacheCount();
            arrayCapacities[i] = arrayCapacity;
            cacheCounts[i] = cacheCount;
            // 数组空间不可以缩小
            if (i > 0 && arrayCapacity <= arrayCapacities[i - 1]) {
                throw new IllegalArgumentException("bucketInfo: " + bucketInfo);
            }
        }
    }

    /**
     * @param capacityArray 空间信息数组
     * @param arrayLength   期望的数组大小
     * @return 所属的chunk的索引，-1表示不存在匹配的空间
     */
    public static int indexBucketOfArray(int[] capacityArray, int arrayLength) {
        int index = Arrays.binarySearch(capacityArray, arrayLength);
        if (index < 0) {
            // 不匹配的情况下再校验合法性
            if (arrayLength < capacityArray[0]
                    || arrayLength > capacityArray[capacityArray.length - 1]) {
                return -1;
            }
            index = (index + 1) * -1;
        }
        return index;
    }
}
