#region LICENSE

// Copyright 2026 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// FixedVector4的格式化实现
/// </summary>
internal static class FixedVector4Styles
{
    public static void Print(DsonPrinter printer, FixedVector4 value, FixedVector4Style style) {
        const FixedVector4Style validFlags = FixedVector4Style.Vector | FixedVector4Style.Len2 | FixedVector4Style.Len3;
        if ((style & ~validFlags) != 0) {
            throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
        int len = 4;
        if ((style & FixedVector4Style.Len3) != 0) {
            len = 3;
        } else if ((style & FixedVector4Style.Len2) != 0) {
            len = 2;
        }
        bool isObject = (style & FixedVector4Style.Vector) != 0;
        printer.FastPrint(isObject ? "{@fv4 " : "[@fv4 ");
        for (int index = 0; index < len; index++) {
            if (index > 0) {
                printer.FastPrint(", ");
            }
            if (isObject) {
                printer.FastPrint(index switch
                {
                    0 => "x: ",
                    1 => "y: ",
                    2 => "z: ",
                    _ => "w: "
                });
            }
            printer.FastPrint(value[index]);
        }
        printer.FastPrint(isObject ? "}" : "]");
    }
}
}