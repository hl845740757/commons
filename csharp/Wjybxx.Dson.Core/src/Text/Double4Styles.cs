#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Globalization;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// Double4的格式化实现
/// </summary>
internal static class Double4Styles
{
    private const int MASK_VECTOR4 = 'x' | 'y' << 8 | 'z' << 16 | 'w' << 24;
    private const int MASK_VECTOR3 = 'x' | 'y' << 8 | 'z' << 16;
    private const int MASK_VECTOR2 = 'x' | 'y' << 8;
    //
    private const int MASK_RGBA = 'r' | 'g' << 8 | 'b' << 16 | 'a' << 24;
    private const int MASK_RGB = 'r' | 'g' << 8 | 'b' << 16;
    //
    private const int MASK_RECT = 'x' | 'y' << 8 | 'w' << 16 | 'h' << 24;

    public static void Print(DsonPrinter printer, Double4 double4, Double4Style style) {
        // Array
        if (style <= Double4Style.Array2) {
            printer.FastPrint("[@D4 ");
            printer.FastPrint(double4.v0.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", ");
            printer.FastPrint(double4.v1.ToString(CultureInfo.InvariantCulture));
            if (style <= Double4Style.Array3) {
                printer.FastPrint(", ");
                printer.FastPrint(double4.v2.ToString(CultureInfo.InvariantCulture));
            }
            if (style <= Double4Style.Array4) {
                printer.FastPrint(", ");
                printer.FastPrint(double4.v3.ToString(CultureInfo.InvariantCulture));
            }
            printer.FastPrint("]");
            return;
        }
        // 向量
        if (style <= Double4Style.Vector2) {
            printer.FastPrint("{@D4 x: ");
            printer.FastPrint(double4.v0.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", y: ");
            printer.FastPrint(double4.v1.ToString(CultureInfo.InvariantCulture));
            if (style <= Double4Style.Vector3) {
                printer.FastPrint(", z: ");
                printer.FastPrint(double4.v2.ToString(CultureInfo.InvariantCulture));
            }
            if (style <= Double4Style.Vector4) {
                printer.FastPrint(", w: ");
                printer.FastPrint(double4.v3.ToString(CultureInfo.InvariantCulture));
            }
            printer.FastPrint("}");
            return;
        }
        // 整数向量
        if (style <= Double4Style.Vector2Int) {
            printer.FastPrint("{@D4 x: ");
            printer.FastPrint(((long)double4.v0).ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", y: ");
            printer.FastPrint(((long)double4.v1).ToString(CultureInfo.InvariantCulture));
            if (style <= Double4Style.Vector3Int) {
                printer.FastPrint(", z: ");
                printer.FastPrint(((long)double4.v2).ToString(CultureInfo.InvariantCulture));
            }
            if (style <= Double4Style.Vector4Int) {
                printer.FastPrint(", w: ");
                printer.FastPrint(((long)double4.v3).ToString(CultureInfo.InvariantCulture));
            }
            printer.FastPrint("}");
            return;
        }
        // 颜色值
        if (style == Double4Style.Rgba || style == Double4Style.Rgb) {
            printer.FastPrint("{@D4 r: ");
            printer.FastPrint(double4.v0.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", g: ");
            printer.FastPrint(double4.v1.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", b: ");
            printer.FastPrint(double4.v2.ToString(CultureInfo.InvariantCulture));
            if (style == Double4Style.Rgba) {
                printer.FastPrint(", a: ");
                printer.FastPrint(double4.v3.ToString(CultureInfo.InvariantCulture));
            }
            printer.FastPrint("}");
            return;
        }
        // 矩形
        if (style == Double4Style.Rect) {
            printer.FastPrint("{@D4 x: ");
            printer.FastPrint(double4.v0.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", y: ");
            printer.FastPrint(double4.v1.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", w: ");
            printer.FastPrint(double4.v2.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", h: ");
            printer.FastPrint(double4.v3.ToString(CultureInfo.InvariantCulture));
            printer.FastPrint("}");
            return;
        }
        if (style == Double4Style.RectInt) {
            printer.FastPrint("{@D4 x: ");
            printer.FastPrint(((long)double4.v0).ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", y: ");
            printer.FastPrint(((long)double4.v1).ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", w: ");
            printer.FastPrint(((long)double4.v2).ToString(CultureInfo.InvariantCulture));
            printer.FastPrint(", h: ");
            printer.FastPrint(((long)double4.v3).ToString(CultureInfo.InvariantCulture));
            printer.FastPrint("}");
            return;
        }
        throw new IndexOutOfRangeException(nameof(style));
    }
}
}