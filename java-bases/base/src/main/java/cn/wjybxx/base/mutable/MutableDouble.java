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

package cn.wjybxx.base.mutable;

/**
 * @author wjybxx
 * date - 2024/1/4
 */
public class MutableDouble extends Number implements MutableNumber<Double>, Comparable<MutableDouble> {

    private double value;

    public MutableDouble() {
    }

    public MutableDouble(double value) {
        this.value = value;
    }

    public MutableDouble(Number value) {
        this.value = value.doubleValue();
    }

    public MutableDouble(String value) {
        this.value = Double.parseDouble(value);
    }

    // region cast

    @Override
    public Double getValue() {
        return value;
    }

    @Override
    public void setValue(Double value) {
        this.value = value;
    }

    public void setValue(final double value) {
        this.value = value;
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

    // endregion


    // region op

    /** 加上操作数 */
    public void add(final double operand) {
        this.value += operand;
    }

    /** 返回加上操作数后的值 */
    public double addAndGet(final double operand) {
        this.value += operand;
        return value;
    }

    /** 返回当前值 */
    public double getAndAdd(final double operand) {
        final double last = value;
        this.value += operand;
        return last;
    }

    /** 加1 */
    public void increment() {
        value++;
    }

    /** 返回加1后的值 */
    public double incrementAndGet() {
        return ++value;
    }

    /** 加1并返回当前值 */
    public double getAndIncrement() {
        return value++;
    }

    /** 减1 */
    public void decrement() {
        value--;
    }

    /** 返回减1后的值 */
    public double decrementAndGet() {
        return --value;
    }

    /** 减1并返回当前值 */
    public double getAndDecrement() {
        return value--;
    }

    @Override
    public MutableDouble add(Number operand) {
        this.value += operand.doubleValue();
        return this;
    }

    @Override
    public MutableDouble subtract(Number operand) {
        this.value -= operand.doubleValue();
        return this;
    }

    @Override
    public MutableDouble multiply(Number operand) {
        this.value *= operand.doubleValue();
        return this;
    }

    @Override
    public MutableDouble divide(Number operand) {
        this.value /= operand.doubleValue();
        return this;
    }

    // endregion

    // region equals

    @Override
    public int compareTo(MutableDouble that) {
        return Double.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        MutableDouble that = (MutableDouble) o;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        return Double.hashCode(value);
    }

    @Override
    public String toString() {
        return "MutableDouble{" +
                "value=" + value +
                '}';
    }

    // endregion
}
