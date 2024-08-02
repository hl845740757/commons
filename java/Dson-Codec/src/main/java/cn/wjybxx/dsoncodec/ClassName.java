/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dsoncodec;

import cn.wjybxx.base.mutable.MutableInt;

import javax.annotation.concurrent.Immutable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * 结构化的类型名。
 * 1.跨语言时，建议为泛型类提供别名，避免反引号。
 * 2.不要为数组提供别名，保持'[]'结尾。
 *
 * <h3>格式化样式</h3>
 * ClassName的编码样式采用了C#的TypeName编码样式，
 * <pre>
 * System.Collections.Generic.Dictionary`2
 * [
 *   System.Int32,
 *   System.String[]
 * ][]
 * </pre>
 * 1. 数组用一对方括号表示'[]'，中间不可包含空白字符；
 * 2. 泛型参数放在一对方括号中'[,]'，通过逗号分隔；
 *
 * @author wjybxx
 * date - 2024/4/24
 */
@Immutable
public final class ClassName {

    private static final char SPLIT_CHAR = '`';

    /**
     * 无泛型参数的类型别名(简单名)。
     * 1. 如果不是泛型类，类名仅包含类的简单名。
     * 2. 如果是泛型类，类名包含泛型参数的个数 —— 别名可能不包含。
     * 3. 如果是数组，包含[]，每一阶一组[] —— []之间不可以有空格。
     */
    public final String clsName;
    /** 泛型参数信息，无泛型时为空List */
    public final List<ClassName> typeArgs;
    /**
     * HashCode缓存 -- hashcode查询频率高，因此缓存。
     * (此优化不破坏不可变约束)
     */
    private int _hashcode;

    public ClassName(String clsName) {
        this.clsName = clsName;
        this.typeArgs = List.of();
    }

    public ClassName(String clsName, List<ClassName> typeArgs) {
        this.clsName = Objects.requireNonNull(clsName, "clsName");
        this.typeArgs = typeArgs == null ? List.of() : List.copyOf(typeArgs);
    }

    // region 基础查询

    /**
     * 是否是数组类型。
     * 注意：如果为特定类型数组取了别名，该测试不一定准确；应尽量避免为数组定义别名。
     */
    public boolean isArray() {
        int idx = clsName.length() - 1;
        return clsName.charAt(idx) == ']' && clsName.charAt(idx - 1) == '[';
    }

    /**
     * 数组的阶数（维度）。
     * 如果是数组，则返回对应的阶数，否则返回0
     */
    public int getArrayRank() {
        int r = 0;
        int idx = clsName.length() - 1;
        while (clsName.charAt(idx) == ']' && clsName.charAt(idx - 1) == '[') {
            idx -= 2;
            r++;
        }
        return r;
    }

    /**
     * 获取根元素类型。
     * 如果是数组，则返回数组的最终元素类型，否则直接返回{@link #clsName}
     */
    public String getRootElement() {
        int idx = clsName.length() - 1;
        while (clsName.charAt(idx) == ']' && clsName.charAt(idx - 1) == '[') {
            idx -= 2;
        }
        return clsName.substring(0, idx + 1);
    }

    /**
     * 是否是泛型。
     * 注意：如果为特定构造泛型取了别名，该测试不一定准确。
     */
    public boolean isGeneric() {
        return typeArgs.size() > 0;
    }

    // endregion

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ClassName className = (ClassName) o;
        if (!clsName.equals(className.clsName)) return false;
        return typeArgs.equals(className.typeArgs);
    }

    @Override
    public int hashCode() {
        int r = _hashcode;
        if (r != 0) return r;

        r = clsName.hashCode();
        for (int i = 0; i < typeArgs.size(); i++) {
            r = 31 * r + typeArgs.get(i).hashCode();
        }

        _hashcode = r;
        return r;
    }

    @Override
    public String toString() {
        return toString(null).toString();
    }

    // endregion

    // region convert

    /**
     * 将{@link ClassName}转换为Dson字符串格式
     *
     * @param sb 方便外部池化减少开销
     * @return fullClsName
     */
    public StringBuilder toString(StringBuilder sb) {
        if (sb == null) sb = new StringBuilder(clsName.length());
        // 元类型放首部
        int arrayRank = getArrayRank();
        if (arrayRank == 0) {
            sb.append(clsName);
        } else {
            sb.append(clsName, 0, clsName.length() - arrayRank * 2);
        }
        // 泛型信息放中部，递归的情况不多见，这里不优化
        if (typeArgs.size() > 0) {
            sb.append('[');
            for (int i = 0; i < typeArgs.size(); i++) {
                if (i > 0) {
                    sb.append(',');
                }
                typeArgs.get(i).toString(sb);
            }
            sb.append(']');
        }
        // 数组符号放末尾
        if (arrayRank > 0) {
            sb.append(DsonConverterUtils.arrayRankSymbol(arrayRank));
        }
        return sb;
    }

    /**
     * 将Dson格式类型名字符串转换为结构体
     *
     * @param fullClsName Dson格式的完整类名
     * @return 结构化的类名
     */
    public static ClassName parse(String fullClsName) {
        return parse(fullClsName, 0, fullClsName.length() - 1);
    }

    /**
     * 解析Dson格式的类型名。
     * （实际上和C#类型名编码是相同的）
     *
     * @param fullClsName   完整类名
     * @param rawStartIndex 开始索引(包含)
     * @param rawEndIndex   结束索引(包含)
     * @return 结构化类型名
     */
    private static ClassName parse(String fullClsName, int rawStartIndex, int rawEndIndex) {
        // 跳过两端空白，避免分割字符串
        rawStartIndex = SkipLeading(fullClsName, rawStartIndex);
        rawEndIndex = SkipTrailing(fullClsName, rawEndIndex);

        // 统计数组阶数，数组的'[]'之间不能有空格
        int arrayRank = 0;
        while (fullClsName.charAt(rawEndIndex) == ']' && fullClsName.charAt(rawEndIndex - 1) == '[') {
            rawEndIndex -= 2;
            arrayRank++;
        }

        // 判断是否是泛型，通过[]来匹配更容易裁剪 -- 去除末尾的数组'[]'后，自身是泛型才能可能有'[]'
        int typeArgStartIdx = fullClsName.indexOf('[', rawStartIndex, rawEndIndex + 1);
        if (typeArgStartIdx > 0) {
            // 泛型 List`1[string]
            int typeArgEndIdx = fullClsName.lastIndexOf(']', rawEndIndex);
            if (typeArgEndIdx < 0 || typeArgStartIdx + 1 >= typeArgEndIdx) {
                throw new IllegalArgumentException("bad fullClsName : " + fullClsName);
            }
            // 截取类简单名 List`1
            int clsNameEndIndex = typeArgStartIdx - 1;
            clsNameEndIndex = SkipTrailing(fullClsName, clsNameEndIndex);
            String clsName = fullClsName.substring(rawStartIndex, clsNameEndIndex + 1);
            // 简单名需要拼接数组符号
            if (arrayRank > 0) {
                clsName = clsName + DsonConverterUtils.arrayRankSymbol(arrayRank);
            }

            // 解析泛型参数 -- 不能简单逗号分隔，需按Token匹配；泛型个数通常不超过2
            List<ClassName> typeArgs = new ArrayList<>(2);
            MutableInt eleStartIdx = new MutableInt(typeArgStartIdx + 1);
            while (eleStartIdx.getValue() > 0) {
                String typeArg = ScanNextTypeArg(fullClsName, eleStartIdx.getValue(), typeArgEndIdx - 1, eleStartIdx);
                if (typeArg == null) {
                    break;
                }
                if (typeArg.indexOf('[') > 0) { // 嵌套泛型，递归情况不多，不优化
                    typeArgs.add(parse(typeArg, 0, typeArg.length() - 1));
                } else {
                    typeArgs.add(new ClassName(typeArg, null));
                }
            }
            return new ClassName(clsName, typeArgs);
        } else {
            // 非泛型
            // Debug.Assert(fullClsName.LastIndexOf('[', rawEndIndex) < 0);
            return new ClassName(fullClsName, null);
        }
    }

    /**
     * 扫描下一个泛型参数
     *
     * @param typeArgs       泛型参数字符串
     * @param startIndex     扫描的开始位置(包含)
     * @param endIndex       扫描的结束位置(包含)
     * @param nextStartIndex 接收下一次的扫描开始位置，-1表示结束
     * @return 扫描到的泛型元素
     */
    private static String ScanNextTypeArg(String typeArgs, int startIndex, int endIndex, MutableInt nextStartIndex) {
        startIndex = SkipLeading(typeArgs, startIndex);
        endIndex = SkipTrailing(typeArgs, endIndex);
        if (startIndex > endIndex) {
            nextStartIndex.setValue(-1);
            return null; // 结束-空白
        }
        int stackDepth = 0;
        int index = startIndex;
        while (index <= endIndex) {
            char c = typeArgs.charAt(index);
            if (c == ',' && stackDepth == 0) { // 结束-分隔符
                nextStartIndex.setValue(index + 1);
                index = SkipTrailing(typeArgs, index - 1);
                return typeArgs.substring(startIndex, index + 1);
            }
            if (c == '[') { // 入栈
                stackDepth++;
            } else if (c == ']') { // 出栈
                stackDepth--;
            }
            if (index == endIndex) { // 结束-eof
                assert stackDepth == 0;
                nextStartIndex.setValue(-1);
                index = SkipTrailing(typeArgs, index);
                return typeArgs.substring(startIndex, index + 1);
            }
            index++;
        }
        throw new IllegalArgumentException("bad typeArgs: " + typeArgs);
    }

    /** 跳过首部空白字符 */
    private static int SkipLeading(String str, int index) {
        while (Character.isWhitespace(str.charAt(index))) {
            index++;
        }
        return index;
    }

    /** 跳过尾部空白字符 */
    private static int SkipTrailing(String str, int index) {
        while (Character.isWhitespace(str.charAt(index))) {
            index--;
        }
        return index;
    }
    // endregion
}
