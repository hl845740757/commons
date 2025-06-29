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

import static cn.wjybxx.dson.DsonInt32.POOL_END;
import static cn.wjybxx.dson.DsonInt32.POOL_START;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public final class DsonDouble extends DsonNumber implements Comparable<DsonDouble> {

    private final double value;

    public DsonDouble(double value) {
        this.value = value;
    }

    public double getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.DOUBLE;
    }

    @Override
    public Double number() {
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
        return (float) value;
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

        DsonDouble that = (DsonDouble) o;

        return Double.compare(that.value, value) == 0;
    }

    @Override
    public int hashCode() {
        return Double.hashCode(value);
    }

    @Override
    public int compareTo(DsonDouble that) {
        return Double.compare(value, that.value);
    }

    // endregion

    @Override
    public String toString() {
        return "DsonDouble{" +
                "value=" + value +
                '}';
    }

    // region 池化管理

    /**
     * Q：为什么double要池化？
     * A：因为数字的默认解析类型是double。
     */
    private static final DsonDouble[] POOL = new DsonDouble[POOL_END - POOL_START + 1];
    public static final DsonDouble ZERO;
    public static final DsonDouble ONE;
    public static final DsonDouble MINUS_ONE;

    static {
        for (int i = POOL_START; i <= POOL_END; i++) {
            POOL[i - POOL_START] = new DsonDouble(i);
        }
        ZERO = valueOf(0);
        ONE = valueOf(1);
        MINUS_ONE = valueOf(-1);
    }

    public static DsonDouble valueOf(double dValue) {
        int value = (int) dValue;
        if (value != dValue) {// 非整数
            return new DsonDouble(dValue);
        }
        if (value < POOL_START || value > POOL_END) {
            return new DsonDouble(value);
        }
        return POOL[value - POOL_START];
    }
    // endregion
}