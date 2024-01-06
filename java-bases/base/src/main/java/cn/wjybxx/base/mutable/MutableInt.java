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
public class MutableInt extends Number implements MutableNumber<Integer>, Comparable<MutableInt> {

    private int value;

    public MutableInt() {
    }

    public MutableInt(int value) {
        this.value = value;
    }

    public MutableInt(Number value) {
        this.value = value.intValue();
    }

    public MutableInt(String value) {
        this.value = Integer.parseInt(value);
    }

    // region cast

    @Override
    public Integer getValue() {
        return value;
    }

    @Override
    public void setValue(Integer value) {
        this.value = value;
    }

    public void setValue(final int value) {
        this.value = value;
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

    // endregion


    // region op

    /** 加上操作数 */
    public void add(final int operand) {
        this.value += operand;
    }

    /** 返回加上操作数后的值 */
    public int addAndGet(final int operand) {
        this.value += operand;
        return value;
    }

    /** 返回当前值 */
    public int getAndAdd(final int operand) {
        final int last = value;
        this.value += operand;
        return last;
    }

    /** 加1 */
    public void increment() {
        value++;
    }

    /** 返回加1后的值 */
    public int incrementAndGet() {
        return ++value;
    }

    /** 加1并返回当前值 */
    public int getAndIncrement() {
        return value++;
    }

    /** 减1 */
    public void decrement() {
        value--;
    }

    /** 返回减1后的值 */
    public int decrementAndGet() {
        return --value;
    }

    /** 减1并返回当前值 */
    public int getAndDecrement() {
        return value--;
    }

    @Override
    public MutableInt add(Number operand) {
        this.value += operand.intValue();
        return this;
    }

    @Override
    public MutableInt subtract(Number operand) {
        this.value -= operand.intValue();
        return this;
    }

    @Override
    public MutableInt multiply(Number operand) {
        this.value *= operand.intValue();
        return this;
    }

    @Override
    public MutableInt divide(Number operand) {
        this.value /= operand.intValue();
        return this;
    }

    // endregion

    // region equals

    @Override
    public int compareTo(MutableInt that) {
        return Integer.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        MutableInt that = (MutableInt) o;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        return value;
    }

    @Override
    public String toString() {
        return "MutableInt{" +
                "value=" + value +
                '}';
    }

    // endregion
}
