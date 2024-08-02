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
public class MutableFloat extends Number implements MutableNumber<Float>, Comparable<MutableFloat> {

    private float value;

    public MutableFloat() {
    }

    public MutableFloat(float value) {
        this.value = value;
    }

    public MutableFloat(Number value) {
        this.value = value.floatValue();
    }

    public MutableFloat(String value) {
        this.value = Float.parseFloat(value);
    }

    // region cast

    @Override
    public Float getValue() {
        return value;
    }

    @Override
    public void setValue(Float value) {
        this.value = value;
    }

    public void setValue(final float value) {
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
        return value;
    }

    @Override
    public double doubleValue() {
        return value;
    }

    // endregion


    // region op

    /** 加上操作数 */
    public void add(final float operand) {
        this.value += operand;
    }

    /** 返回加上操作数后的值 */
    public float addAndGet(final float operand) {
        this.value += operand;
        return value;
    }

    /** 返回当前值 */
    public float getAndAdd(final float operand) {
        final float last = value;
        this.value += operand;
        return last;
    }

    /** 加1 */
    public void increment() {
        value++;
    }

    /** 返回加1后的值 */
    public float incrementAndGet() {
        return ++value;
    }

    /** 加1并返回当前值 */
    public float getAndIncrement() {
        return value++;
    }

    /** 减1 */
    public void decrement() {
        value--;
    }

    /** 返回减1后的值 */
    public float decrementAndGet() {
        return --value;
    }

    /** 减1并返回当前值 */
    public float getAndDecrement() {
        return value--;
    }

    @Override
    public MutableFloat add(Number operand) {
        this.value += operand.floatValue();
        return this;
    }

    @Override
    public MutableFloat subtract(Number operand) {
        this.value -= operand.floatValue();
        return this;
    }

    @Override
    public MutableFloat multiply(Number operand) {
        this.value *= operand.floatValue();
        return this;
    }

    @Override
    public MutableFloat divide(Number operand) {
        this.value /= operand.floatValue();
        return this;
    }

    // endregion

    // region equals

    @Override
    public int compareTo(MutableFloat that) {
        return Float.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        MutableFloat that = (MutableFloat) o;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        return Float.hashCode(value);
    }

    @Override
    public String toString() {
        return "MutableFloat{" +
                "value=" + value +
                '}';
    }

    // endregion
}
