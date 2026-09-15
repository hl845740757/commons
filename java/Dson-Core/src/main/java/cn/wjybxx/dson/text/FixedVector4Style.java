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

package cn.wjybxx.dson.text;

import cn.wjybxx.dson.types.FixedVector4;

/** FixedVector4的文本输出格式。 */
public final class FixedVector4Style {

    /** 打印为数组格式。 */
    public static final int MASK_ARRAY = 0;
    /** 打印为向量格式。 */
    public static final int MASK_VECTOR = 0x01;
    /** 只打印前两个数。 */
    public static final int MASK_LEN2 = 0x04;
    /** 只打印前三个数；与Len2同时指定时优先。 */
    public static final int MASK_LEN3 = 0x08;
    private static final int VALID_MASK = MASK_VECTOR | MASK_LEN2 | MASK_LEN3;

    public static final FixedVector4Style ARRAY = new FixedVector4Style(MASK_ARRAY);
    public static final FixedVector4Style VECTOR4 = new FixedVector4Style(MASK_VECTOR);
    public static final FixedVector4Style VECTOR3 = new FixedVector4Style(MASK_VECTOR | MASK_LEN3);
    public static final FixedVector4Style VECTOR2 = new FixedVector4Style(MASK_VECTOR | MASK_LEN2);

    public final int features;

    public FixedVector4Style(int features) {
        if ((features & ~VALID_MASK) != 0) {
            throw new IllegalArgumentException("invalid features: " + features);
        }
        this.features = features;
    }

    void print(DsonPrinter printer, FixedVector4 value) {
        int length = (features & MASK_LEN3) != 0 ? 3 : (features & MASK_LEN2) != 0 ? 2 : 4;
        boolean vector = (features & MASK_VECTOR) != 0;
        printer.fastPrint(vector ? "{@fv4 " : "[@fv4 ");
        for (int index = 0; index < length; index++) {
            if (index > 0) {
                printer.fastPrint(", ");
            }
            if (vector) {
                printer.fastPrint(switch (index) {
                    case 0 -> "x: ";
                    case 1 -> "y: ";
                    case 2 -> "z: ";
                    default -> "w: ";
                });
            }
            printer.fastPrint(value.get(index));
        }
        printer.fastPrint(vector ? "}" : "]");
    }
}
