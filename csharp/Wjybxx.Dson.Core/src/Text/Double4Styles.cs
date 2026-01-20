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

    public static void Print(DsonPrinter printer, Double4 double4, Double4Style style) {
        Double4Style basicStyle = style & Double4Style.Rect;
        switch (basicStyle) {
            case Double4Style.Array: {
                printer.FastPrint("[@D4 ");
                PrintDouble(printer, double4.v0, style);
                printer.FastPrint(", ");
                PrintDouble(printer, double4.v1, style);
                if ((style & Double4Style.Len2) == 0) {
                    printer.FastPrint(", ");
                    PrintDouble(printer, double4.v2, style);
                }
                if ((style & Double4Style.Len3) == 0) {
                    printer.FastPrint(", ");
                    PrintDouble(printer, double4.v3, style);
                }
                printer.FastPrint("]");
                break;
            }
            case Double4Style.Vector: {
                printer.FastPrint("{@D4 x: ");
                PrintDouble(printer, double4.v0, style);
                printer.FastPrint(", y: ");
                PrintDouble(printer, double4.v1, style);
                if ((style & Double4Style.Len2) == 0) {
                    printer.FastPrint(", z: ");
                    PrintDouble(printer, double4.v2, style);
                }
                if ((style & Double4Style.Len3) == 0) {
                    printer.FastPrint(", w: ");
                    PrintDouble(printer, double4.v3, style);
                }
                printer.FastPrint("}");
                break;
            }
            case Double4Style.Rgba: {
                printer.FastPrint("{@D4 r: ");
                PrintDouble(printer, double4.v0, style);
                printer.FastPrint(", g: ");
                PrintDouble(printer, double4.v1, style);
                printer.FastPrint(", b: ");
                PrintDouble(printer, double4.v2, style);
                if ((style & Double4Style.Len3) == 0) {
                    printer.FastPrint(", a: ");
                    PrintDouble(printer, double4.v3, style);
                }
                printer.FastPrint("}");
                break;
            }
            case Double4Style.Rect: {
                printer.FastPrint("{@D4 x: ");
                PrintDouble(printer, double4.v0, style);
                printer.FastPrint(", y: ");
                PrintDouble(printer, double4.v1, style);
                printer.FastPrint(", w: ");
                PrintDouble(printer, double4.v2, style);
                printer.FastPrint(", h: ");
                PrintDouble(printer, double4.v3, style);
                printer.FastPrint("}");
                break;
            }
            default: throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }

    private static void PrintDouble(DsonPrinter printer, double value, Double4Style style) {
        if ((style & Double4Style.Integer) != 0) {
            string str = NumberStyle.Simple.ToString((long)value).Value;
            printer.FastPrint(str);
        } else {
            NumberStyle numberStyle = style switch
            {
                Double4Style.NoExponent3 => NumberStyle.NoExponent3,
                Double4Style.NoExponent7 => NumberStyle.NoExponent7,
                _ => NumberStyle.Simple
            };
            string str = numberStyle.ToString(value).Value;
            printer.FastPrint(str);
        }
    }
}
}