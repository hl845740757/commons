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
using System.Collections;
using System.Collections.Generic;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 代码块
///
/// $N 发射一个名称，必要时使用名称冲突避免。名称的参数可以是字符串(实际上是任何字符序列)或任意其它<see cref="ISpecification"/>。
/// $L 发射一个没有转义的文字值。字面值的参数可以是字符串、基础值类型、任意其它<see cref="ISpecification"/>、注释甚至其它代码块。
/// $S 将该值转义为字符串，用双引号括起来，然后发射该字符串。
/// $T 发射一个类型引用。如果可能的话，将导入类型。类型的参数可以是Type、TypeName。
/// $$ 表示一个$符号。
/// $> 增加缩进级别
/// $ 减少缩进级别（C#这个左尖括号怎么打才没警告...）。
/// $[ 开始一个语句。对于多行语句，第一行之后的每一行都是双缩进的。
/// $] 结束语句。
/// </summary>
[Immutable]
public class CodeBlock
{
    /// <summary>
    /// 是否为单行表达式风格
    /// (该顺序默认只在属性和方法中生效)
    /// <code>
    /// public int sum(int x, int y) => x + y;
    /// </code>
    /// </summary>
    public readonly bool expressionStyle;
    /// <summary>
    /// 拆解后的format，包含'$L'这样的表达式和普通字符串
    /// </summary>
    public readonly IList<string> formatParts;
    public readonly IList<object?> args;

    private CodeBlock(Builder builder) {
        this.expressionStyle = builder.expressionStyle;
        this.formatParts = Util.ToImmutableList(builder.formatParts);
        this.args = Util.ToImmutableList(builder.args);
    }

    private CodeBlock(bool expressionStyle, CodeBlock codeBlock) {
        this.expressionStyle = expressionStyle;
        this.formatParts = codeBlock.formatParts;
        this.args = codeBlock.args;
    }

    private CodeBlock() {
        this.expressionStyle = false;
        this.formatParts = Util.EmptyList<string>();
        this.args = Util.EmptyList<object>();
    }

    /// <summary>
    /// 代码块是否为空
    /// </summary>
    public bool IsEmpty => formatParts.Count == 0;

    /// <summary>
    /// 如果Null和Empty对用户来说一致，就避免使用Null
    /// </summary>
    public static bool IsNullOrEmpty(CodeBlock? codeBlock) => codeBlock == null || codeBlock.IsEmpty;

    /// <summary>
    /// 拷贝
    /// </summary>
    /// <param name="expressionStyle"></param>
    /// <returns></returns>
    public CodeBlock WithExpressionStyle(bool expressionStyle = true) {
        return new CodeBlock(expressionStyle, this);
    }

    /// <summary>
    /// 空代码块
    /// </summary>
    public static CodeBlock Empty { get; } = new CodeBlock();

    public override string ToString() {
        return CollectionUtil.ToString(formatParts); // TODO
    }

    #region builder

    public static CodeBlock Of(string format, params object[] args) {
        return new Builder().Add(format, args).Build(); // 需要拆解format
    }

    public static Builder NewBuilder(string format, params object[] args) {
        return new Builder().Add(format, args);
    }

    public static Builder NewBuilder() {
        return new Builder();
    }

    public Builder ToBuilder() => new Builder()
        .ExpressionStyle(expressionStyle)
        .Add(this);

    #endregion

    public class Builder
    {
        public bool expressionStyle = false;
        internal readonly List<string> formatParts = new();
        internal readonly List<object> args = new();

        internal Builder() {
        }

        public CodeBlock Build() {
            return IsEmpty ? CodeBlock.Empty : new CodeBlock(this);
        }

        public bool IsEmpty => formatParts.Count == 0;

        /// <summary>
        /// 设置为表达式风格
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Builder ExpressionStyle(bool value = true) {
            this.expressionStyle = value;
            return this;
        }

        /// <summary>
        /// 清理代码块
        /// </summary>
        /// <returns></returns>
        public Builder Clear() {
            expressionStyle = false;
            formatParts.Clear();
            args.Clear();
            return this;
        }

        /// <summary>
        /// 合并代码块
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        public Builder Add(CodeBlock codeBlock) {
            if (codeBlock.IsEmpty) {
                return this;
            }
            formatParts.AddRange(codeBlock.formatParts);
            args.AddRange(codeBlock.args);
            return this;
        }

        /// <summary>
        /// 添加语句
        /// </summary>
        /// <param name="format">格式</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public Builder Add(string format, params object?[] args) {
            bool hasRelative = false;
            bool hasIndexed = false;

            int relativeParameterCount = 0;
            int[] indexedParameterCount = new int[args.Length];

            for (int p = 0; p < format.Length;) {
                if (format[p] != '$') { // 变量
                    int nextP = format.IndexOf('$', p + 1);
                    if (nextP == -1) nextP = format.Length;
                    formatParts.Add(format.Substring2(p, nextP));
                    p = nextP;
                    continue;
                }
                p++; // '$'.

                // Consume zero or more digits, leaving 'c' as the first non-digit char after the '$'.
                int indexStart = p;
                char c;
                do {
                    Util.CheckArgument(p < format.Length, "dangling format characters in '{0}'", format);
                    c = format[p++];
                } while (c >= '0' && c <= '9');
                int indexEnd = p - 1;

                // If 'c' doesn't take an argument, we're done.
                if (IsNoArgPlaceholder(c)) {
                    Util.CheckArgument(
                        indexStart == indexEnd, "$$, $>, $<, $[, $], $W, and $Z may not have an index");
                    formatParts.Add("$" + c);
                    continue;
                }

                // Find either the indexed argument, or the relative argument. (0-based).
                int index;
                if (indexStart < indexEnd) {
                    index = int.Parse(format.Substring2(indexStart, indexEnd)) - 1;
                    hasIndexed = true;
                    if (args.Length > 0) {
                        indexedParameterCount[index % args.Length]++; // modulo is needed, checked below anyway
                    }
                } else {
                    index = relativeParameterCount;
                    hasRelative = true;
                    relativeParameterCount++;
                }

                Util.CheckArgument(index >= 0 && index < args.Length,
                    "index {0} for '{1}' not in range (received {2} arguments)",
                    index + 1, format.Substring2(indexStart - 1, indexEnd + 1), args.Length);
                Util.CheckArgument(!hasIndexed || !hasRelative,
                    "cannot mix indexed and positional parameters");

                AddArgument(format, c, args[index]);
                formatParts.Add("$" + c);
            }

            if (hasRelative) {
                Util.CheckArgument(relativeParameterCount >= args.Length,
                    "unused arguments: expected {0}, received {1}", relativeParameterCount, args.Length);
            }
            if (hasIndexed) {
                List<string> unused = new List<string>();
                for (int i = 0; i < args.Length; i++) {
                    if (indexedParameterCount[i] == 0) {
                        unused.Add("$" + (i + 1));
                    }
                }
                string s = unused.Count == 1 ? "" : "s";
                Util.CheckArgument(unused.Count == 0, "unused argument{0}: {1}", s, string.Join(", ", unused));
            }
            return this;
        }

        #region internal

        private static readonly BitArray noArgPlaceholders = Util.CharToBitArray("$<>[]WZ");

        /** 是否是保留字符 */
        private bool IsNoArgPlaceholder(char c) {
            // return c == '$' || c == '>' || c == '<' || c == '[' || c == ']' || c == 'W' || c == 'Z';
            return noArgPlaceholders.Get(c);
        }

        private void AddArgument(string format, char c, object arg) {
            switch (c) {
                case 'N':
                    this.args.Add(ArgToName(arg));
                    break;
                case 'L':
                    this.args.Add(ArgToLiteral(arg));
                    break;
                case 'S':
                    this.args.Add(ArgToString(arg));
                    break;
                case 'T':
                    this.args.Add(ArgToType(arg));
                    break;
                default:
                    throw new ArgumentException($"invalid format string: '{format}'");
            }
        }

        private static string ArgToName(object o) {
            if (o is string str) {
                return str;
            }
            if (o is ISpecification specification) {
                string name = specification.Name;
                if (name != null) {
                    return name;
                }
            }
            throw new ArgumentException("expected name but was " + o);
        }

        private static object ArgToLiteral(object o) {
            return o;
        }

        private static string? ArgToString(object? o) {
            return o?.ToString();
        }

        private static TypeName ArgToType(object o) {
            if (o is TypeName typeName) return typeName;
            if (o is Type type) return TypeName.Get(type);
            throw new ArgumentException("expected type but was " + o);
        }

        #endregion

        #region 控制流

        public Builder BeginControlFlow(string controlFlow, params object[] args) {
            Add(controlFlow + " {\n", args);
            Indent();
            return this;
        }

        public Builder NextControlFlow(string controlFlow, params object[] args) {
            Unindent();
            Add("} " + controlFlow + " {\n", args);
            Indent();
            return this;
        }

        public Builder EndControlFlow() {
            Unindent();
            Add("}\n");
            return this;
        }

        public Builder EndControlFlow(string controlFlow, params object[] args) {
            Unindent();
            Add("} " + controlFlow + ";\n", args);
            return this;
        }

        public Builder AddStatement(string format, params object[] args) {
            Add("$[");
            Add(format, args);
            Add(";\n$]");
            return this;
        }

        public Builder AddStatement(CodeBlock codeBlock) {
            return AddStatement("$L", codeBlock);
        }

        #endregion

        public Builder AddNewLine(int count = 1) {
            for (int i = 0; i < count; i++) {
                this.formatParts.Add("\n");
            }
            return this;
        }

        public Builder AddType(Type type) {
            AddType(TypeName.Get(type));
            return this;
        }

        public Builder AddType(TypeName type) {
            if (type == null) throw new ArgumentNullException(nameof(type));
            formatParts.Add("$T");
            args.Add(type);
            return this;
        }

        public Builder AddString(string? s) {
            formatParts.Add("$S");
            args.Add(s);
            return this;
        }

        public Builder AddLiteral(string? s) {
            formatParts.Add("$L");
            args.Add(s);
            return this;
        }

        public Builder Indent() {
            this.formatParts.Add("$>");
            return this;
        }

        public Builder Unindent() {
            this.formatParts.Add("$<");
            return this;
        }
    }
}
}