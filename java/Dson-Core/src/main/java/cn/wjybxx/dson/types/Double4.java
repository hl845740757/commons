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

package cn.wjybxx.dson.types;

/**
 * double4
 *
 * @author wjybxx
 * date - 2026/1/17
 */
public class Double4 {

    public static final Double4 EMPTY = new Double4(0, 0, 0, 0);

    public final double v0;
    public final double v1;
    public final double v2;
    public final double v3;

    public Double4(double v0, double v1, double v2) {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = 0;
    }

    public Double4(double v0, double v1, double v2, double v3) {
        this.v0 = v0;
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;
    }

    public double get(int index) {
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

        Double4 that = (Double4) o;
        return Double.compare(v0, that.v0) == 0 && Double.compare(v1, that.v1) == 0 && Double.compare(v2, that.v2) == 0 && Double.compare(v3, that.v3) == 0;
    }

    @Override
    public int hashCode() {
        int result = Double.hashCode(v0);
        result = 31 * result + Double.hashCode(v1);
        result = 31 * result + Double.hashCode(v2);
        result = 31 * result + Double.hashCode(v3);
        return result;
    }

    @Override
    public String toString() {
        return "Double4{" +
                "v0=" + v0 +
                ", v1=" + v1 +
                ", v2=" + v2 +
                ", v3=" + v3 +
                '}';
    }
}