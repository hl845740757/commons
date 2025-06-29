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
public final class DsonFloat extends DsonNumber implements Comparable<DsonFloat> {

    private final float value;

    public DsonFloat(float value) {
        this.value = value;
    }

    public float getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.FLOAT;
    }

    @Override
    public Float number() {
        return value;
    }

    @Override
    public int intValue() {
        return (int) value;
    }

    @Override
    public long longValue() {
        return (long) value;
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

        DsonFloat dsonFloat = (DsonFloat) o;

        return Float.compare(dsonFloat.value, value) == 0;
    }

    @Override
    public int hashCode() {
        return Float.hashCode(value);
    }

    @Override
    public int compareTo(DsonFloat that) {
        return Float.compare(value, that.value);
    }

    // endregion

    @Override
    public String toString() {
        return "DsonFloat{" +
                "value=" + value +
                '}';
    }

    private static final int POOL_START = -9;
    private static final int POOL_END = 9;
    /** Float只缓存常见的几个整数值 */
    private static final DsonFloat[] POOL = new DsonFloat[POOL_END - POOL_START + 1];
    public static final DsonFloat ZERO;
    public static final DsonFloat ONE;
    public static final DsonFloat MINUS_ONE;

    static {
        for (int i = POOL_START; i <= POOL_END; i++) {
            POOL[i - POOL_START] = new DsonFloat(i);
        }
        ZERO = valueOf(0);
        ONE = valueOf(1);
        MINUS_ONE = valueOf(-1);
    }

    public static DsonFloat valueOf(float fValue) {
        int value = (int) fValue;
        if (value != fValue) { // 非整数
            return new DsonFloat(fValue);
        }
        if (value < POOL_START || value > POOL_END) {
            return new DsonFloat(value);
        }
        return POOL[value - POOL_START];
    }
}