/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import cn.wjybxx.dson.types.Double4;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2026/1/17
 */
public class DsonDouble4 extends DsonValue {

    public static final DsonDouble4 EMPTY = new DsonDouble4(Double4.EMPTY);

    private final Double4 value;

    public DsonDouble4(Double4 value) {
        this.value = Objects.requireNonNull(value);
    }

    public Double4 getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.DOUBLE4;
    }

    @Override
    public boolean equals(Object o) {
        if (o == null || getClass() != o.getClass()) return false;

        DsonDouble4 that = (DsonDouble4) o;
        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    @Override
    public String toString() {
        return "DsonDouble4{" +
                "value=" + value +
                '}';
    }
}