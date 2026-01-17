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

package cn.wjybxx.dson.text;

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.dson.types.Double4;

/**
 * Double4的格式
 *
 * @author wjybxx
 * date - 2026/1/17
 */
public enum Double4Style implements EnumLite {

    ARRAY4(0),
    ARRAY3(1),
    ARRAY2(2),

    VECTOR4(3),
    VECTOR3(4),
    VECTOR2(5),

    VECTOR4_INT(6),
    VECTOR3_INT(7),
    VECTOR2_INT(8),

    RGBA(9),
    RGB(10),

    RECT(11),
    RECT_INT(12),

    ;
    public final int number;

    Double4Style(int number) {
        this.number = number;
    }

    @Override
    public int getNumber() {
        return number;
    }

    static void print(DsonPrinter printer, Double4 double4, Double4Style style) {
        // Array
        if (style.number <= Double4Style.ARRAY2.number) {
            printer.fastPrint("[@D4 ");
            printer.fastPrint(Double.toString(double4.v0));
            printer.fastPrint(", ");
            printer.fastPrint(Double.toString(double4.v1));
            if (style.number <= Double4Style.ARRAY3.number) {
                printer.fastPrint(", ");
                printer.fastPrint(Double.toString(double4.v2));
            }
            if (style.number <= Double4Style.ARRAY4.number) {
                printer.fastPrint(", ");
                printer.fastPrint(Double.toString(double4.v3));
            }
            printer.fastPrint("]");
            return;
        }
        // 向量
        if (style.number <= Double4Style.VECTOR2.number) {
            printer.fastPrint("{@D4 x: ");
            printer.fastPrint(Double.toString(double4.v0));
            printer.fastPrint(", y: ");
            printer.fastPrint(Double.toString(double4.v1));
            if (style.number <= Double4Style.VECTOR3.number) {
                printer.fastPrint(", z: ");
                printer.fastPrint(Double.toString(double4.v2));
            }
            if (style.number <= Double4Style.VECTOR4.number) {
                printer.fastPrint(", w: ");
                printer.fastPrint(Double.toString(double4.v3));
            }
            printer.fastPrint("}");
            return;
        }
        // 整数向量
        if (style.number <= Double4Style.VECTOR2_INT.number) {
            printer.fastPrint("{@D4 x: ");
            printer.fastPrint(Long.toString((long) double4.v0));
            printer.fastPrint(", y: ");
            printer.fastPrint(Long.toString((long) double4.v1));
            if (style.number <= Double4Style.VECTOR3_INT.number) {
                printer.fastPrint(", z: ");
                printer.fastPrint(Long.toString((long) double4.v2));
            }
            if (style.number <= Double4Style.VECTOR4_INT.number) {
                printer.fastPrint(", w: ");
                printer.fastPrint(Long.toString((long) double4.v3));
            }
            printer.fastPrint("}");
            return;
        }
        // 颜色值
        if (style == Double4Style.RGBA || style == Double4Style.RGB) {
            printer.fastPrint("{@D4 r: ");
            printer.fastPrint(Double.toString(double4.v0));
            printer.fastPrint(", g: ");
            printer.fastPrint(Double.toString(double4.v1));
            printer.fastPrint(", b: ");
            printer.fastPrint(Double.toString(double4.v2));
            if (style == Double4Style.RGBA) {
                printer.fastPrint(", a: ");
                printer.fastPrint(Double.toString(double4.v3));
            }
            printer.fastPrint("}");
            return;
        }
        // 矩形
        if (style == Double4Style.RECT) {
            printer.fastPrint("{@D4 x: ");
            printer.fastPrint(Double.toString(double4.v0));
            printer.fastPrint(", y: ");
            printer.fastPrint(Double.toString(double4.v1));
            printer.fastPrint(", w: ");
            printer.fastPrint(Double.toString(double4.v2));
            printer.fastPrint(", h: ");
            printer.fastPrint(Double.toString(double4.v3));
            printer.fastPrint("}");
            return;
        }
        if (style == Double4Style.RECT_INT) {
            printer.fastPrint("{@D4 x: ");
            printer.fastPrint(Long.toString((long) double4.v0));
            printer.fastPrint(", y: ");
            printer.fastPrint(Long.toString((long) double4.v1));
            printer.fastPrint(", w: ");
            printer.fastPrint(Long.toString((long) double4.v2));
            printer.fastPrint(", h: ");
            printer.fastPrint(Long.toString((long) double4.v3));
            printer.fastPrint("}");
            return;
        }
        throw new IndexOutOfBoundsException(style.number);
    }
}