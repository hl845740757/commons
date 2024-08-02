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

package cn.wjybxx.dson.text;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
public enum StringStyle {

    /**
     * 自动判别
     * 1.当内容较短且无特殊字符，且不是特殊值（true/false/数字）时不加引号
     * 2.当内容长度中等时，打印为双引号字符串
     * 3.当内容较长时，打印为文本模式
     */
    AUTO,

    /** 自动加引号模式 -- 优先无引号，如果不可以无引号则加引号 */
    AUTO_QUOTE,

    /** 双引号模式 -- 内容可能包含特殊字符，且想保持流式输入 */
    QUOTE,

    /** 无引号模式 -- 内容不包含特殊字符，且内容较短；要小心使用 */
    UNQUOTE,

    /**
     * 纯文本模式
     * <p>
     * 1. 对内容无限制。
     * 2. 不对内容进行转义，会考虑行长度限制，一行内容可能输出为多行。
     * 3. 适用内容可能包含特殊字符，或内容较长时。
     * <pre>{@code
     * @"""
     * @- content
     * @- content
     * @"""
     * }
     * </pre>
     */
    TEXT,

    /**
     * 简单纯文本模式
     * <p>
     * 1. 内容行不能以三引号(""")开头。
     * 2. 默认不被启用，只有显式指定的情况下生效。
     * 3. 不对内容进行转义，不考虑行长度限制，直接按行打印。
     * 4. 适用内容可能包含特殊字符，或内容较长时。
     * <pre>{@code
     * """
     * content
     * """
     * }
     * </pre>
     */
    SIMPLE_TEXT,

    /**
     * 单行纯文本(sL标签)
     * <p>
     * 1. 内容不可以包含换行符。
     * 2. 默认不被启用，只有显式指定的情况下生效。
     * 3. 不对内容进行转义，不考虑行长度限制，直接打印。
     * 4. 适合包含特殊字符的短文本
     */
    STRING_LINE,
}