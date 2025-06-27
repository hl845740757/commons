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

package cn.wjybxx.dson;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public final class DsonInt32 extends DsonNumber implements Comparable<DsonInt32> {

    private final int value;

    public DsonInt32(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.INT32;
    }

    @Override
    public Integer number() {
        return value;
    }

    @Override
    public int intValue() {
        return value;
    }

    @Override
    public long longValue() {
        return value;
    }

    @Override
    public float floatValue() {
        return value;
    }

    @Override
    public double doubleValue() {
        return value;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonInt32 dsonInt32 = (DsonInt32) o;

        return value == dsonInt32.value;
    }

    @Override
    public int hashCode() {
        return value;
    }

    @Override
    public int compareTo(DsonInt32 that) {
        return Integer.compare(value, that.value);
    }

    // endregion

    @Override
    public String toString() {
        return "DsonInt32{" +
                "value=" + value +
                '}';
    }

    // region 池化管理

    static final int POOL_START = -9;
    static final int POOL_END = 127;
    // 注意初始化顺序
    private static final DsonInt32[] POOL = new DsonInt32[POOL_END - POOL_START + 1];
    public static final DsonInt32 ZERO = valueOf(0);
    public static final DsonInt32 ONE = valueOf(1);
    public static final DsonInt32 MINUS_ONE = valueOf(-1);

    static {
        for (int i = POOL_START; i <= POOL_END; i++) {
            POOL[i - POOL_START] = new DsonInt32(i);
        }
    }

    public static DsonInt32 valueOf(int value) {
        if (value < POOL_START || value > POOL_END) {
            return new DsonInt32(value);
        }
        return POOL[value - POOL_START];
    }
    // endregion
}