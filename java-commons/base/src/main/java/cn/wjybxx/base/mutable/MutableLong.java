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
public class MutableLong extends Number implements MutableNumber<Long>, Comparable<MutableLong> {

    private long value;

    public MutableLong() {
    }

    public MutableLong(long value) {
        this.value = value;
    }

    public MutableLong(Number value) {
        this.value = value.longValue();
    }

    public MutableLong(String value) {
        this.value = Long.parseLong(value);
    }

    // region cast

    @Override
    public Long getValue() {
        return value;
    }

    @Override
    public void setValue(Long value) {
        this.value = value;
    }

    public void setValue(final long value) {
        this.value = value;
    }

    @Override
    public int intValue() {
        return (int) value;
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
    public void add(final long operand) {
        this.value += operand;
    }

    /** 返回加上操作数后的值 */
    public long addAndGet(final long operand) {
        this.value += operand;
        return value;
    }

    /** 返回当前值 */
    public long getAndAdd(final long operand) {
        final long last = value;
        this.value += operand;
        return last;
    }

    /** 加1 */
    public void increment() {
        value++;
    }

    /** 返回加1后的值 */
    public long incrementAndGet() {
        return ++value;
    }

    /** 加1并返回当前值 */
    public long getAndIncrement() {
        return value++;
    }

    /** 减1 */
    public void decrement() {
        value--;
    }

    /** 返回减1后的值 */
    public long decrementAndGet() {
        return --value;
    }

    /** 减1并返回当前值 */
    public long getAndDecrement() {
        return value--;
    }

    @Override
    public MutableLong add(Number operand) {
        this.value += operand.longValue();
        return this;
    }

    @Override
    public MutableLong subtract(Number operand) {
        this.value -= operand.longValue();
        return this;
    }

    @Override
    public MutableLong multiply(Number operand) {
        this.value *= operand.longValue();
        return this;
    }

    @Override
    public MutableLong divide(Number operand) {
        this.value /= operand.longValue();
        return this;
    }

    // endregion

    // region equals

    @Override
    public int compareTo(MutableLong that) {
        return Long.compare(value, that.value);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        MutableLong that = (MutableLong) o;
        return value == that.value;
    }

    @Override
    public int hashCode() {
        return Long.hashCode(value);
    }

    @Override
    public String toString() {
        return "MutableLong{" +
                "value=" + value +
                '}';
    }

    // endregion
}
