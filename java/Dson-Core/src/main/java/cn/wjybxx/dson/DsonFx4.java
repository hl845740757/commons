/*
 * Copyright 2023-2026 wjybxx(845740757@qq.com)
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

import cn.wjybxx.dson.types.FixedPoint4;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 */
public final class DsonFx4 extends DsonValue implements Comparable<DsonFx4> {

    private final FixedPoint4 value;

    public DsonFx4(FixedPoint4 value) {
        this.value = Objects.requireNonNull(value);
    }

    public FixedPoint4 getValue() {
        return value;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.FIXED_POINT4;
    }

    @Override
    public boolean equals(Object o) {
        if (o == null || getClass() != o.getClass()) return false;
        DsonFx4 that = (DsonFx4) o;
        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    @Override
    public int compareTo(DsonFx4 that) {
        return value.compareTo(that.value);
    }

    @Override
    public String toString() {
        return "DsonFx4{" +
                "value=" + value +
                '}';
    }
}
