/*
 * Copyright 2026 wjybxx(845740757@qq.com)
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

import java.util.Objects;

/** 四分量定点数向量，仅用于Dson数据表示。 */
public final class FixedVector4 {

    public static final FixedVector4 EMPTY = new FixedVector4(
            FixedPoint4.ZERO, FixedPoint4.ZERO,
            FixedPoint4.ZERO, FixedPoint4.ZERO);

    public final FixedPoint4 v0;
    public final FixedPoint4 v1;
    public final FixedPoint4 v2;
    public final FixedPoint4 v3;

    public FixedVector4(FixedPoint4 v0, FixedPoint4 v1, FixedPoint4 v2) {
        this(v0, v1, v2, FixedPoint4.ZERO);
    }

    public FixedVector4(FixedPoint4 v0, FixedPoint4 v1, FixedPoint4 v2, FixedPoint4 v3) {
        this.v0 = Objects.requireNonNull(v0);
        this.v1 = Objects.requireNonNull(v1);
        this.v2 = Objects.requireNonNull(v2);
        this.v3 = Objects.requireNonNull(v3);
    }

    public FixedPoint4 get(int index) {
        return switch (index) {
            case 0 -> v0;
            case 1 -> v1;
            case 2 -> v2;
            case 3 -> v3;
            default -> throw new IndexOutOfBoundsException(index);
        };
    }

    @Override
    public boolean equals(Object o) {
        if (o == null || getClass() != o.getClass()) return false;
        FixedVector4 that = (FixedVector4) o;
        return v0.equals(that.v0) && v1.equals(that.v1) && v2.equals(that.v2) && v3.equals(that.v3);
    }

    @Override
    public int hashCode() {
        int result = v0.hashCode();
        result = 31 * result + v1.hashCode();
        result = 31 * result + v2.hashCode();
        result = 31 * result + v3.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "FixedVector4{" +
                "v0=" + v0 +
                ", v1=" + v1 +
                ", v2=" + v2 +
                ", v3=" + v3 +
                '}';
    }
}
