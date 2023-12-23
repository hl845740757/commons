package cn.wjybxx.common;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/12/23
 */
public class ArrayUtils {

    public static final int INDEX_NOT_FOUND = -1;

    public static final byte[] EMPTY_BYTE_ARRAY = {};
    public static final Object[] EMPTY_OBJECT_ARRAY = {};
    public static final Class<?>[] EMPTY_CLASS_ARRAY = {};

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
            return INDEX_NOT_FOUND;
        }
        if (startIndex < 0) {
            startIndex = 0;
        }
        for (int i = startIndex, size = list.length; i < size; i++) {
            if (list[i] == element) {
                return i;
            }
        }
        return INDEX_NOT_FOUND;
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
            return INDEX_NOT_FOUND;
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
}