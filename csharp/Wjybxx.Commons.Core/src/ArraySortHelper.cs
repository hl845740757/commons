#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;

namespace Wjybxx.Commons;

internal class ArraySortHelper
{
    #region array

    /// <summary>
    /// 如果元素存在，则返回元素对应的下标；
    /// 如果元素不存在，则返回(-(insertion point) - 1)
    /// 即： (index + 1) * -1 可得应当插入的下标。 
    /// </summary>
    /// <param name="array"></param>
    /// <param name="fromIndex">inclusive</param>
    /// <param name="toIndex">exclusive</param>
    /// <param name="key">要查找的元素</param>
    /// <returns></returns>
    public static int BinarySearch(int[] array, int fromIndex, int toIndex,
                                   int key) {
        int low = fromIndex;
        int high = toIndex - 1;

        while (low <= high) {
            int mid = (low + high) >> 1;
            int midVal = array[mid];

            if (midVal < key)
                low = mid + 1;
            else if (midVal > key)
                high = mid - 1;
            else
                return mid; // key found
        }
        return -(low + 1); // key not found.
    }

    public static int BinarySearch<T>(T[] a, int fromIndex, int toIndex,
                                      T key, Comparer<T> c) {
        int low = fromIndex;
        int high = toIndex - 1;

        while (low <= high) {
            int mid = (low + high) >> 1;
            T midVal = a[mid];
            int cmp = c.Compare(midVal, key);
            if (cmp < 0)
                low = mid + 1;
            else if (cmp > 0)
                high = mid - 1;
            else
                return mid; // key found
        }
        return -(low + 1); // key not found.
    }

    public static int BinarySearch<T>(T[] a, int fromIndex, int toIndex,
                                      Func<T, int> c) {
        int low = fromIndex;
        int high = toIndex - 1;

        while (low <= high) {
            int mid = (low + high) >> 1;
            T midVal = a[mid];
            int cmp = c.Invoke(midVal);
            if (cmp < 0)
                low = mid + 1;
            else if (cmp > 0)
                high = mid - 1;
            else
                return mid; // key found
        }
        return -(low + 1); // key not found.
    }

    #endregion

    #region list

    public static int BinarySearch<T>(List<T> a, int fromIndex, int toIndex,
                                      T key, Comparer<T> c) {
        int low = fromIndex;
        int high = toIndex - 1;

        while (low <= high) {
            int mid = (low + high) >> 1;
            T midVal = a[mid];
            int cmp = c.Compare(midVal, key);
            if (cmp < 0)
                low = mid + 1;
            else if (cmp > 0)
                high = mid - 1;
            else
                return mid; // key found
        }
        return -(low + 1); // key not found.
    }

    public static int BinarySearch<T>(List<T> a, int fromIndex, int toIndex,
                                      Func<T, int> c) {
        int low = fromIndex;
        int high = toIndex - 1;

        while (low <= high) {
            int mid = (low + high) >> 1;
            T midVal = a[mid];
            int cmp = c.Invoke(midVal);
            if (cmp < 0)
                low = mid + 1;
            else if (cmp > 0)
                high = mid - 1;
            else
                return mid; // key found
        }
        return -(low + 1); // key not found.
    }

    #endregion
}