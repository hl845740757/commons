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

package cn.wjybxx.dson.types;

import cn.wjybxx.dson.Dsons;

import javax.annotation.concurrent.Immutable;

/**
 * long值的简单扩展
 *
 * @author wjybxx
 * date - 2023/4/19
 */
@Immutable
public class ExtInt32 implements Comparable<ExtInt32> {

    private final int type;
    private final boolean hasValue; // 比较时放前面
    private final int value;

    public ExtInt32(int type, int value) {
        this(type, value, true);
    }

    public ExtInt32(int type, Integer value) {
        this(type, value == null ? 0 : value, value != null);
    }

    public ExtInt32(int type, int value, boolean hasValue) {
        Dsons.checkSubType(type);
        Dsons.checkHasValue(value, hasValue);
        this.type = type;
        this.value = value;
        this.hasValue = hasValue;
    }

    public static ExtInt32 emptyOf(int type) {
        return new ExtInt32(type, 0, false);
    }

    public int getType() {
        return type;
    }

    public int getValue() {
        return value;
    }

    public boolean hasValue() {
        return hasValue;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ExtInt32 extInt32 = (ExtInt32) o;

        if (type != extInt32.type) return false;
        if (value != extInt32.value) return false;
        return hasValue == extInt32.hasValue;
    }

    @Override
    public int hashCode() {
        int result = type;
        result = 31 * result + value;
        result = 31 * result + (hasValue ? 1 : 0);
        return result;
    }

    @Override
    public int compareTo(ExtInt32 that) {
        int r = Integer.compare(type, that.type);
        if (r != 0) {
            return r;
        }
        r = Boolean.compare(hasValue, that.hasValue);
        if (r != 0) {
            return r;
        }
        return Integer.compare(value, that.value);
    }

    //endregion

    @Override
    public String toString() {
        return "ExtInt32{" +
                "type=" + type +
                ", value=" + value +
                ", hasValue=" + hasValue +
                '}';
    }
}
