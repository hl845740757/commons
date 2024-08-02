#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 结构化的类型名
/// 1.跨语言时，建议为泛型类提供别名，避免反引号。
/// 2.不要为数组提供别名，保持'[]'结尾。
///
/// <h3>格式化样式</h3>
/// ClassName的编码样式采用了C#的TypeName编码样式，
/// <pre>
/// System.Collections.Generic.Dictionary`2
/// [
///   System.Int32,
///   System.String[]
/// ][]
/// </pre>
/// 1. 数组用一对方括号表示'[]'，中间不可包含空白字符；
/// 2. 泛型参数放在一对方括号中'[,]'，通过逗号分隔；
/// </summary>
[Immutable]
public struct ClassName : IEquatable<ClassName>
{
    /// <summary>
    /// 分割类名和泛型个数数字的分隔符，暂定为C#使用的反引号。
    /// 注意：在跨语言通信时，不建议使用泛型类的简单名，而是提供无反引号的类型别名。
    /// </summary>
    public const char SPILT_CHAR = '`';

    /// <summary>
    /// 无泛型参数的类型别名(简单名)。
    /// 1. 如果不是泛型类，类名仅包含类的简单名。
    /// 2. 如果是泛型类，类名包含泛型参数的个数 -- 别名可能不包含。
    /// 3. 如果是数组，包含[]，每一阶一组[] —— '[]'之间不可以有空格。
    /// <code>
    /// String
    /// List`1
    /// </code>
    /// </summary>
    public readonly string clsName;
    /// <summary>
    /// 泛型参数信息，无泛型时为空List
    /// </summary>
    public readonly IList<ClassName> typeArgs;
    /// <summary>
    /// HashCode缓存 -- hashcode查询频率高，因此缓存。
    /// (此优化不破坏不可变约束)
    /// </summary>
    private int _hashcode;

    public ClassName(string clsName, IList<ClassName>? typeArgs = null) {
        this.clsName = clsName ?? throw new ArgumentNullException(nameof(clsName));
        this.typeArgs = typeArgs?.ToImmutableList2() ?? ImmutableList<ClassName>.Empty;
        this._hashcode = 0;
    }

    #region 基础查询

    /// <summary>
    /// 是否是数组类型。
    /// 注意：如果为特定类型数组取了别名，该测试不一定准确；应尽量避免为数组定义别名。
    /// </summary>
    public bool IsArray {
        get {
            int idx = clsName.Length - 1;
            return clsName[idx] == ']' && clsName[idx - 1] == '[';
        }
    }

    /// <summary>
    /// 数组的阶数（维度）。
    /// 如果是数组，则返回对应的阶数，否则返回0
    /// </summary>
    public int ArrayRank {
        get {
            int r = 0;
            int idx = clsName.Length - 1;
            while (clsName[idx] == ']' && clsName[idx - 1] == '[') {
                idx -= 2;
                r++;
            }
            return r;
        }
    }

    /// <summary>
    /// 获取根元素类型。
    /// 如果是数组，则返回数组的最终元素类型 —— 非数组的clsName；
    /// 如果不是数组，则直接返回<see cref="clsName"/>
    /// </summary>
    public string RootElement {
        get {
            int idx = clsName.Length - 1;
            while (clsName[idx] == ']' && clsName[idx - 1] == '[') {
                idx -= 2;
            }
            return clsName.Substring(0, idx + 1);
        }
    }

    /// <summary>
    /// 是否是泛型类 -- 根据TypeArgs测试是精准的。
    /// </summary>
    public bool IsGeneric => typeArgs.Count > 0;

    #endregion

    #region equals

    public bool Equals(ClassName other) {
        return clsName == other.clsName
               && typeArgs.SequenceEqual(other.typeArgs);
    }

    public override bool Equals(object? obj) {
        return obj is ClassName other && Equals(other);
    }

    public override int GetHashCode() {
        int r = _hashcode;
        if (r != 0) return r;

        r = clsName.GetHashCode();
        for (int i = 0; i < typeArgs.Count; i++) {
            r = r * 31 + typeArgs[i].GetHashCode();
        }

        _hashcode = r;
        return r;
    }

    public static bool operator ==(ClassName left, ClassName right) {
        return left.Equals(right);
    }

    public static bool operator !=(ClassName left, ClassName right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return ToString(null).ToString();
    }

    #endregion

    #region convert

    /// <summary>
    /// 将<see cref="ClassName"/>转换为Dson字符串格式
    /// </summary>
    /// <param name="sb">方便外部池化减少开销</param>
    /// <returns>fullClsName</returns>
    public StringBuilder ToString(StringBuilder? sb) {
        if (sb == null) sb = new StringBuilder(clsName.Length);
        // 元类型放首部
        int arrayRank = ArrayRank;
        if (arrayRank == 0) {
            sb.Append(clsName);
        } else {
            sb.Append(clsName, 0, clsName.Length - arrayRank * 2);
        }
        // 泛型信息放中部，递归的情况不多见，这里不优化
        if (typeArgs.Count > 0) {
            sb.Append('[');
            for (int i = 0; i < typeArgs.Count; i++) {
                if (i > 0) {
                    sb.Append(',');
                }
                typeArgs[i].ToString(sb);
            }
            sb.Append(']');
        }
        // 数组符号放末尾
        if (arrayRank > 0) {
            sb.Append(DsonConverterUtils.ArrayRankSymbol(arrayRank));
        }
        return sb;
    }

    /// <summary>
    /// 将Dson格式类型名字符串转换为结构体
    /// </summary>
    /// <param name="fullClsName">Dson格式的完整类名</param>
    /// <returns>结构化的类名</returns>
    public static ClassName Parse(string fullClsName) {
        return Parse(fullClsName, 0, fullClsName.Length - 1);
    }

    /// <summary>
    /// 解析Dson格式的类型名。
    /// （实际上和C#类型名编码是相同的）
    /// </summary>
    /// <param name="fullClsName">完整类名</param>
    /// <param name="rawStartIndex">开始索引(包含)</param>
    /// <param name="rawEndIndex">结束索引(包含)</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal static ClassName Parse(string fullClsName, int rawStartIndex, int rawEndIndex) {
        // 跳过两端空白，避免分割字符串
        SkipLeading(fullClsName, ref rawStartIndex);
        SkipTrailing(fullClsName, ref rawEndIndex);

        // 统计数组阶数，数组的'[]'之间不能有空格
        int arrayRank = 0;
        while (fullClsName[rawEndIndex] == ']' && fullClsName[rawEndIndex - 1] == '[') {
            rawEndIndex -= 2;
            arrayRank++;
        }

        // 判断是否是泛型，通过[]来匹配更容易裁剪 -- 去除末尾的数组'[]'后，自身是泛型才能可能有'[]'
        int subLength = rawEndIndex - rawStartIndex + 1;
        int typeArgStartIdx = fullClsName.IndexOf('[', rawStartIndex, subLength);
        if (typeArgStartIdx > 0) {
            // 泛型 List`1[string]
            int typeArgEndIdx = fullClsName.LastIndexOf(']', rawEndIndex, subLength);
            if (typeArgEndIdx < 0 || typeArgStartIdx + 1 >= typeArgEndIdx) {
                throw new ArgumentException("bad fullClsName : " + fullClsName);
            }
            // 截取类简单名 List`1
            int clsNameEndIndex = typeArgStartIdx - 1;
            SkipTrailing(fullClsName, ref clsNameEndIndex);
            string clsName = fullClsName.Substring(rawStartIndex, clsNameEndIndex - rawStartIndex + 1);
            // 简单名需要拼接数组符号
            if (arrayRank > 0) {
                clsName = clsName + DsonConverterUtils.ArrayRankSymbol(arrayRank);
            }

            // 解析泛型参数 -- 不能简单逗号分隔，需按Token匹配；泛型个数通常不超过2
            List<ClassName> typeArgs = new List<ClassName>(2);
            int eleStartIdx = typeArgStartIdx + 1;
            while (eleStartIdx > 0) {
                string typeArg = ScanNextTypeArg(fullClsName, eleStartIdx, typeArgEndIdx - 1, out eleStartIdx);
                if (typeArg == null) {
                    break;
                }
                if (typeArg.IndexOf('[') > 0) { // 嵌套泛型，递归情况不多，不优化
                    typeArgs.Add(Parse(typeArg, 0, typeArg.Length - 1));
                } else {
                    typeArgs.Add(new ClassName(typeArg, null));
                }
            }
            return new ClassName(clsName, typeArgs);
        } else {
            // 非泛型
            // Debug.Assert(fullClsName.LastIndexOf('[', rawEndIndex) < 0);
            return new ClassName(fullClsName, null);
        }
    }

    /// <summary>
    /// 扫描下一个泛型参数
    /// </summary>
    /// <param name="typeArgs">泛型参数字符串</param>
    /// <param name="startIndex">扫描的开始位置(包含)</param>
    /// <param name="endIndex">扫描的结束位置(包含)</param>
    /// <param name="nextStartIndex">接收下一次的扫描开始位置，-1表示结束</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string? ScanNextTypeArg(string typeArgs, int startIndex, int endIndex, out int nextStartIndex) {
        SkipLeading(typeArgs, ref startIndex);
        SkipTrailing(typeArgs, ref endIndex);
        if (startIndex > endIndex) {
            nextStartIndex = -1;
            return null; // 结束-空白
        }
        int stackDepth = 0;
        int index = startIndex;
        while (index <= endIndex) {
            char c = typeArgs[index];
            if (c == ',' && stackDepth == 0) { // 结束-分隔符
                nextStartIndex = index + 1;
                index--;
                SkipTrailing(typeArgs, ref index);
                return typeArgs.Substring(startIndex, index - startIndex + 1);
            }
            if (c == '[') { // 入栈
                stackDepth++;
            } else if (c == ']') { // 出栈
                stackDepth--;
            }
            if (index == endIndex) { // 结束-eof
                Debug.Assert(stackDepth == 0);
                nextStartIndex = -1;
                SkipTrailing(typeArgs, ref index);
                return typeArgs.Substring(startIndex, index - startIndex + 1);
            }
            index++;
        }
        throw new ArgumentException("bad typeArgs: " + typeArgs);
    }

    /** 跳过首部空白字符 */
    private static void SkipLeading(string str, ref int index) {
        while (char.IsWhiteSpace(str[index])) {
            index++;
        }
    }

    /** 跳过尾部空白字符 */
    private static void SkipTrailing(string str, ref int index) {
        while (char.IsWhiteSpace(str[index])) {
            index--;
        }
    }

    #endregion
}
}