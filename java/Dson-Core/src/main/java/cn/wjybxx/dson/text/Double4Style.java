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

import cn.wjybxx.dson.types.Double4;

/**
 * Double4的格式
 *
 * @author wjybxx
 * date - 2026/1/17
 */
public final class Double4Style {

    public final int features;

    public Double4Style(int features) {
        this.features = features;
    }

    // region factory

    /** 打印为普通Array格式 */
    public static final int MASK_ARRAY = 0;
    /** 打印为向量格式(1) */
    public static final int MASK_VECTOR = 0x01;
    /** 打印为颜色值格式(2) */
    public static final int MASK_RGBA = 0x02;
    /** 打印为矩形值格式(3) */
    public static final int MASK_RECT = 0x03;
    /** 限定Double4的长度为2，即只打印前两个数 */
    public static final int MASK_LEN2 = 0x04;
    /** 限定Double4的长度为3，即只打印前三个数 */
    public static final int MASK_LEN3 = 0x08;

    /** 浮点数禁用科学计数法，并最多保留小数点后3位(向最近的偶数舍入) -- 可能导致反序列化结果不相等 */
    public static final int MASK_NO_EXPONENT3 = 0x10;
    /** 浮点数禁用科学计数法，并最多保留小数点后7位(向最近的偶数舍入) -- 可能导致反序列化结果不相等 */
    public static final int MASK_NO_EXPONENT7 = 0x20;
    /** Value截断为整数 -- 可能导致反序列化结果不相等 */
    public static final int MASK_INTEGER = 0x40;


    public static final Double4Style ARRAY = new Double4Style(0);
    public static final Double4Style VECTOR4 = new Double4Style(MASK_VECTOR);
    public static final Double4Style VECTOR3 = new Double4Style(MASK_VECTOR | MASK_LEN3);
    public static final Double4Style VECTOR2 = new Double4Style(MASK_VECTOR | MASK_LEN2);

    public static final Double4Style VECTOR4_INT = new Double4Style(MASK_VECTOR | MASK_INTEGER);
    public static final Double4Style VECTOR3_INT = new Double4Style(MASK_VECTOR | MASK_INTEGER | MASK_LEN3);
    public static final Double4Style VECTOR2_INT = new Double4Style(MASK_VECTOR | MASK_INTEGER | MASK_LEN2);

    public static final Double4Style RGBA = new Double4Style(MASK_RGBA);
    public static final Double4Style RGB = new Double4Style(MASK_RGBA | MASK_LEN3);

    public static final Double4Style RECT = new Double4Style(MASK_RECT);
    public static final Double4Style RECT_INT = new Double4Style(MASK_RECT | MASK_INTEGER);

    void print(DsonPrinter printer, Double4 double4, StyleOut styleOut) {
        int style = features;
        int basicStyle = style & Double4Style.MASK_RECT;
        switch (basicStyle) {
            case Double4Style.MASK_ARRAY: {
                printer.fastPrint("[@D4 ");
                printDouble(printer, double4.v0, style, styleOut);
                printer.fastPrint(", ");
                printDouble(printer, double4.v1, style, styleOut);
                if ((style & Double4Style.MASK_LEN2) == 0) {
                    printer.fastPrint(", ");
                    printDouble(printer, double4.v2, style, styleOut);
                }
                if ((style & Double4Style.MASK_LEN3) == 0) {
                    printer.fastPrint(", ");
                    printDouble(printer, double4.v3, style, styleOut);
                }
                printer.fastPrint("]");
                break;
            }
            case Double4Style.MASK_VECTOR: {
                printer.fastPrint("{@D4 x: ");
                printDouble(printer, double4.v0, style, styleOut);
                printer.fastPrint(", y: ");
                printDouble(printer, double4.v1, style, styleOut);
                if ((style & Double4Style.MASK_LEN2) == 0) {
                    printer.fastPrint(", z: ");
                    printDouble(printer, double4.v2, style, styleOut);
                }
                if ((style & Double4Style.MASK_LEN3) == 0) {
                    printer.fastPrint(", w: ");
                    printDouble(printer, double4.v3, style, styleOut);
                }
                printer.fastPrint("}");
                break;
            }
            case Double4Style.MASK_RGBA: {
                printer.fastPrint("{@D4 r: ");
                printDouble(printer, double4.v0, style, styleOut);
                printer.fastPrint(", g: ");
                printDouble(printer, double4.v1, style, styleOut);
                printer.fastPrint(", b: ");
                printDouble(printer, double4.v2, style, styleOut);
                if ((style & Double4Style.MASK_LEN3) == 0) {
                    printer.fastPrint(", a: ");
                    printDouble(printer, double4.v3, style, styleOut);
                }
                printer.fastPrint("}");
                break;
            }
            case Double4Style.MASK_RECT: {
                printer.fastPrint("{@D4 x: ");
                printDouble(printer, double4.v0, style, styleOut);
                printer.fastPrint(", y: ");
                printDouble(printer, double4.v1, style, styleOut);
                printer.fastPrint(", w: ");
                printDouble(printer, double4.v2, style, styleOut);
                printer.fastPrint(", h: ");
                printDouble(printer, double4.v3, style, styleOut);
                printer.fastPrint("}");
                break;
            }
            default:
                throw new IllegalArgumentException();
        }
    }

    private static void printDouble(DsonPrinter printer, double value, int style, StyleOut styleOut) {
        if ((style & Double4Style.MASK_INTEGER) != 0) {
            String str = NumberStyle.SIMPLE.toString((long) value, styleOut).getValue();
            printer.fastPrint(str);
        } else {
            NumberStyle numberStyle = switch (style) {
                case Double4Style.MASK_NO_EXPONENT3 -> NumberStyle.NO_EXPONENT3;
                case Double4Style.MASK_NO_EXPONENT7 -> NumberStyle.NO_EXPONENT7;
                default -> NumberStyle.SIMPLE;
            };
            String str = numberStyle.toString(value, styleOut).getValue();
            printer.fastPrint(str);
        }
    }
}