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
using System.Linq;
using System.Text;
using Wjybxx.Commons.Attributes;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Poet;

/// <summary>
/// 默认的代码生成器
///
/// 为降低生成代码的逻辑复杂度，类型名暂都使用命名空间下的全限定名，避免对泛型的内部类进行复杂解析。
/// 即内部类总是表现为如下样式：
/// <![CDATA[
///     Dictionay<int, string>.Enumerator
/// ]]>
/// </summary>
[NotThreadSafe]
public class CodeWriter
{
    private readonly string indent;
    private readonly LineWrapper codeOut;

    private bool enableFileScopedNamespace = true;
    private bool enableAutoImport = true;

    /** 当前是否正在写入文档 -- 三斜杠 */
    private bool document = false;
    /** 当前是否正在写入注释 -- 双斜杠 */
    private bool comment = false;
    /** 当前是否处于新行 */
    private bool trailingNewline = false;

    /** 缩进等级 */
    private int indentLevel;
    /// <summary>
    /// 在发出语句时，这是当前正在写入的语句行。
    /// 语句的第一行正常缩进，随后的换行行双缩进。
    /// 当当前写入的行不是语句的一部分时，该值为-1。
    /// </summary>
    private int statementLine = -1;

#nullable disable
    /// <summary>
    /// 当前要Emit的文件
    /// </summary>
    private CsharpFile csharpFile;
#nullable enable
    /// <summary>
    /// 动态解析的命名空间
    /// </summary>
    private readonly LinkedDictionary<string, string?> importableNamespaces = new LinkedDictionary<string, string?>();
    /// <summary>
    /// 当前类型上下文
    /// </summary>
    private readonly Stack<TypeSpec> typeSpecStack = new Stack<TypeSpec>();
    /// <summary>
    /// 当前命名空间上下文
    /// </summary>
    private readonly Stack<NamespaceSpec> namespaceStack = new Stack<NamespaceSpec>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indent">缩进字符</param>
    /// <param name="columnLimit">行宽度限制</param>
    public CodeWriter(string indent = "  ", int columnLimit = 120) {
        this.indent = indent ?? throw new ArgumentNullException(nameof(indent));
        this.codeOut = new LineWrapper(new StringBuilder(1024), indent, columnLimit);
    }

    /// <summary>
    /// 是否启用文件范围命名空间
    /// (用于Unity或低版本dotnet时请关闭)
    /// </summary>
    public bool EnableFileScopedNamespace {
        get => enableFileScopedNamespace;
        set => enableFileScopedNamespace = value;
    }

    /// <summary>
    /// 是否自动解析namespace导入
    /// （存在复杂的宏时请自行管理 -- <see cref="ImportSpec"/>）
    /// </summary>
    public bool EnableAutoImport {
        get => enableAutoImport;
        set => enableAutoImport = value;
    }

    /// <summary>
    /// 重置writer
    /// </summary>
    public void Reset() {
        codeOut.Reset();
        document = false;
        comment = false;
        trailingNewline = false;

        indentLevel = 0;
        statementLine = -1;

        csharpFile = null;
        importableNamespaces.Clear();
        typeSpecStack.Clear();
        namespaceStack.Clear();
    }

    /// <summary>
    /// 写入C#文件
    /// </summary>
    public string Write(CsharpFile csharpFile) {
        if (csharpFile == null) throw new ArgumentNullException(nameof(csharpFile));

        Reset();
        this.csharpFile = csharpFile;
        try {
            // 处理自动导入
            if (enableAutoImport) {
                List<KeyValuePair<string, string?>> resolvedImports = ResolveImports();
                Reset();
                this.csharpFile = csharpFile;
                this.importableNamespaces.PutAll(resolvedImports);
            }
            // 正式写入文件
            codeOut.nullWriter = false;
            EmitFile();
            return codeOut.codeOut.ToString();
        }
        finally {
            this.csharpFile = null!;
        }
    }

    private List<KeyValuePair<string, string?>> ResolveImports() {
        // 先模拟一次写入，便可获得所有的TypeName和namespace
        codeOut.nullWriter = true;
        EmitFile();
        codeOut.nullWriter = false;

        // 删除用户显式导入的类 -- 得到动态导入的类
        foreach (ISpecification nestedSpec in csharpFile.nestedSpecs) {
            if (nestedSpec.SpecType == SpecType.Import) {
                importableNamespaces.Remove(nestedSpec.Name!);
            }
        }
        // 理论上还可以排个序
        return importableNamespaces.ToList();
    }

    #region file/namespace

    private static readonly Func<ISpecification, bool> namespaceFilter = e => e.SpecType == SpecType.Namespace;

    private void EmitFile() {
        int firstIndex = csharpFile.nestedSpecs.IndexOfCustom(namespaceFilter);
        if (firstIndex == -1) {
            // 没有命名空间，认为是空文件 -- 不处理自动导入等文件
            foreach (ISpecification nestedSpec in csharpFile.nestedSpecs) {
                EmitSpec(nestedSpec);
            }
            return;
        }

        // 第一个namespace前的内容需要直接写入 -- 否则可能打乱宏管理的import/using
        for (int i = 0; i < firstIndex; i++) {
            EmitSpec(csharpFile.nestedSpecs[i]);
        }

        Emit("\n");
        // 写额外导入
        foreach (KeyValuePair<string, string?> pair in importableNamespaces) {
            EmitImport(new ImportSpec(pair.Key, pair.Value));
        }
        // 将用户的导入也加入到已解析导入中
        foreach (ISpecification nestedSpec in csharpFile.nestedSpecs) {
            if (nestedSpec.SpecType == SpecType.Import) {
                importableNamespaces.TryGetValue(nestedSpec.Name!, out string? alias);
                if (alias != null) { // 重复导入的情况下，保留别名
                    continue;
                }
                ImportSpec importSpec = (ImportSpec)nestedSpec;
                importableNamespaces[importSpec.name] = importSpec.alias;
            }
        }
        Emit("\n");

        // 如果启用了文件范围命名空间，且只有一个namespace定义，则输出为平铺结构
        if (enableFileScopedNamespace) {
            int lastIndex = csharpFile.nestedSpecs.LastIndexOfCustom(namespaceFilter);
            if (lastIndex == firstIndex) {
                NamespaceSpec namespaceSpec = (NamespaceSpec)csharpFile.nestedSpecs[firstIndex];
                namespaceStack.Push(namespaceSpec);
                Emit("namespace $L;", namespaceSpec.name); // 分号结尾
                Emit("\n");
                Emit("\n"); // 需要插入一个空行
                // 写namespace内元素
                foreach (ISpecification nestedSpec in namespaceSpec.nestedSpecs) {
                    EmitSpec(nestedSpec);
                }
                // 写剩余元素
                for (int i = firstIndex + 1; i < csharpFile.nestedSpecs.Count; i++) {
                    EmitSpec(csharpFile.nestedSpecs[i]);
                }
                if (!ReferenceEquals(namespaceStack.Pop(), namespaceSpec)) {
                    throw new IllegalStateException();
                }
                return;
            }
        }

        // 写剩余元素(不特殊处理namespace)
        for (int i = firstIndex; i < csharpFile.nestedSpecs.Count; i++) {
            EmitSpec(csharpFile.nestedSpecs[i]);
        }
    }

    /// <summary>
    /// 写{}包围的namespace
    /// </summary>
    /// <param name="namespaceSpec"></param>
    private void EmitNamespaceBlocked(NamespaceSpec namespaceSpec) {
        namespaceStack.Push(namespaceSpec);

        Emit("namespace $L", namespaceSpec.name); // {}
        Emit("\n{");
        Indent();
        foreach (ISpecification nestedSpec in namespaceSpec.nestedSpecs) {
            EmitSpec(nestedSpec);
        }
        Unindent();
        Emit("}");

        if (!ReferenceEquals(namespaceStack.Pop(), namespaceSpec)) {
            throw new IllegalStateException();
        }
    }

    private void EmitSpec(ISpecification nestedSpec) {
        switch (nestedSpec.SpecType) {
            case SpecType.Type: {
                EmitType((TypeSpec)nestedSpec);
                break;
            }
            case SpecType.Field: {
                EmitField((FieldSpec)nestedSpec);
                break;
            }
            case SpecType.Property: {
                EmitProperty((PropertySpec)nestedSpec);
                break;
            }
            case SpecType.Method: {
                EmitMethod((MethodSpec)nestedSpec);
                break;
            }
            case SpecType.EnumValue: {
                EmitEnumValue((EnumValueSpec)nestedSpec);
                break;
            }
            case SpecType.Namespace: {
                EmitNamespaceBlocked((NamespaceSpec)nestedSpec);
                break;
            }
            case SpecType.Macro: {
                EmitMacro((MacroSpec)nestedSpec);
                break;
            }
            case SpecType.Import: {
                EmitImport((ImportSpec)nestedSpec);
                break;
            }
            case SpecType.CodeBlock: {
                EmitCodeSpec((CodeBlockSpec)nestedSpec);
                break;
            }
            case SpecType.Parameter:
            default: {
                throw new IllegalStateException(); // 不应该走到这里
            }
        }
    }

    private void EmitCodeSpec(CodeBlockSpec codeBlockSpec) {
        switch (codeBlockSpec.kind) {
            case CodeBlockSpec.Kind.Code: {
                Emit(codeBlockSpec.code);
                break;
            }
            case CodeBlockSpec.Kind.Document: {
                EmitDocument(codeBlockSpec.code);
                break;
            }
            case CodeBlockSpec.Kind.Comment: {
                EmitComment(codeBlockSpec.code);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #endregion

    #region using/macro

    /// <summary>
    /// 发射import
    /// </summary>
    /// <param name="importSpec"></param>
    private void EmitImport(ImportSpec importSpec) {
        if (importSpec.alias != null) {
            Emit("using $L = $L;", importSpec.alias, importSpec.name);
        } else {
            Emit("using $L;", importSpec.name);
        }
        Emit("\n");
    }

    /// <summary>
    /// 发射宏
    /// </summary>
    /// <param name="macroSpec"></param>
    private void EmitMacro(MacroSpec macroSpec) {
        // 宏不能换行
        Emit("#$L $L", macroSpec.name, string.Join(" ", macroSpec.arguments));
        Emit("\n");
    }

    #endregion

    #region document/comment

    /// <summary>
    /// 写文档 
    /// </summary>
    /// <param name="codeBlock"></param>
    private void EmitDocument(CodeBlock codeBlock) {
        if (codeBlock.IsEmpty) {
            return;
        }
        Emit("/// <summary>\n");
        document = true;
        try {
            Emit(codeBlock, true);
        }
        finally {
            document = false;
        }
        Emit("/// </summary>\n");
    }

    /// <summary>
    /// 写普通注释
    /// </summary>
    /// <param name="codeBlock"></param>
    private void EmitComment(CodeBlock codeBlock) {
        if (codeBlock.IsEmpty) {
            return;
        }
        trailingNewline = true; // Force the '//' prefix for the comment.
        comment = true;
        try {
            Emit(codeBlock, true);
        }
        finally {
            comment = false;
        }
    }

    #endregion

    #region typespec

    private void EmitType(TypeSpec typeSpec) {
        typeSpecStack.Push(typeSpec);

        EmitDocument(typeSpec.document);
        Emit(typeSpec.headerCode, true);
        EmitAttributes(typeSpec.attributes);

        if (typeSpec.kind == TypeSpec.Kind.Delegator) {
            EmitMethod((MethodSpec)typeSpec.nestedSpecs[0], true);
        } else {
            EmitModifiers(typeSpec.modifiers);
            switch (typeSpec.kind) {
                case TypeSpec.Kind.Class: {
                    Emit("class ");
                    break;
                }
                case TypeSpec.Kind.Struct: {
                    Emit("struct ");
                    break;
                }
                case TypeSpec.Kind.Interface: {
                    Emit("interface ");
                    break;
                }
                case TypeSpec.Kind.Enum: {
                    Emit("enum ");
                    break;
                }
            }

            Emit(typeSpec.name);
            Emit(" ");

            EmitTypeVariables(typeSpec.typeVariables);
            EmitBaseClasses(typeSpec);

            // 泛型变量约束
            if (HasConstraints(typeSpec.typeVariables)) {
                Emit(" ");
                EmitTypeVariableConstraints(typeSpec.typeVariables);
                Emit(" ");
            }

            // 打印内部元素 -- 需要缩进
            Emit("\n{");
            {
                Indent();
                foreach (ISpecification nestedSpec in typeSpec.nestedSpecs) {
                    EmitSpec(nestedSpec);
                }
                Unindent();
            }
            Emit("}\n"); // 每个元素末尾都默认换行
        }

        if (!ReferenceEquals(typeSpecStack.Pop(), typeSpec)) {
            throw new IllegalStateException();
        }
    }

    /// <summary>
    /// 写超类和接口
    /// </summary>
    /// <param name="typeSpec"></param>
    private void EmitBaseClasses(TypeSpec typeSpec) {
        if (typeSpec.baseClasses.Count == 0) {
            return;
        }
        Emit(" : ");
        for (int index = 0; index < typeSpec.baseClasses.Count; index++) {
            TypeName baseClass = typeSpec.baseClasses[index];
            if (index > 0) {
                Emit(", ");
            }
            EmitTypeName(baseClass);
        }
    }

    #endregion

    #region field

    private void EmitField(FieldSpec fieldSpec) {
        EmitDocument(fieldSpec.document);
        Emit(fieldSpec.headerCode, true);
        EmitAttributes(fieldSpec.attributes);

        EmitModifiers(fieldSpec.modifiers);
        if (fieldSpec.IsEvent) {
            Emit("event ");
        }
        // Type name
        EmitTypeName(fieldSpec.type);
        Emit(" ");
        Emit(fieldSpec.name);

        if (!CodeBlock.IsNullOrEmpty(fieldSpec.initializer)) {
            Emit(" = ");
            Emit(fieldSpec.initializer!);
        }
        EmitIfLastCharNot(';'); // 代码可能包含';'
        Emit("\n");
    }

    #endregion

    #region prorperty

    private void EmitProperty(PropertySpec propertySpec) {
        Emit("\n"); // 属性前空一行
        EmitDocument(propertySpec.document);
        Emit(propertySpec.headerCode, true);
        EmitAttributes(propertySpec.attributes);

        EmitModifiers(propertySpec.modifiers); // 可能无修饰符
        // Type name
        EmitTypeName(propertySpec.type);
        if (propertySpec.IsIndexer) {
            Emit(" this[");
            EmitTypeName(propertySpec.indexType!);
            Emit(" ");
            Emit(propertySpec.indexName!);
            Emit("] ");
        } else {
            Emit(" ");
            Emit(propertySpec.name);
            Emit(" ");
        }

        if (CodeBlock.IsNullOrEmpty(propertySpec.getter)
            && CodeBlock.IsNullOrEmpty(propertySpec.setter)) {
            // 简单属性 -- 单行
            Emit("{");
            if (propertySpec.hasGetter) {
                Emit(" get;");
            }
            if (propertySpec.hasSetter) {
                EmitModifiers(propertySpec.setterModifiers);
                Emit(" set;");
            }
            Emit(" }");
            if (!CodeBlock.IsNullOrEmpty(propertySpec.initializer)) {
                Emit(" = ");
                Emit(propertySpec.initializer!);
                EmitIfLastCharNot(';'); // 代码可能包含';'
            }
            Emit("\n");
        } else {
            // 包含getter/setter代码块 -- 不可以包含初始化块，且必须都是代码块
            Emit("{\n");
            {
                Indent();
                if (!CodeBlock.IsNullOrEmpty(propertySpec.getter)) {
                    EmitGetterSetter(propertySpec.getter!, 1);
                }
                if (!CodeBlock.IsNullOrEmpty(propertySpec.setter)) {
                    EmitModifiers(propertySpec.setterModifiers);
                    EmitGetterSetter(propertySpec.setter!, 2);
                }
                Unindent();
            }
            Emit("}\n");
        }
    }

    private void EmitGetterSetter(CodeBlock codeBlock, int kind) {
        if (codeBlock.expressionStyle) {
            if (kind == 1) {
                Emit("get => ");
            } else {
                Emit("set => ");
            }
            Emit(codeBlock);
            EmitIfLastCharNot(';'); // 代码可能包含';'
            Emit("\n");
        } else {
            if (kind == 1) {
                Emit("get {\n");
            } else {
                Emit("set {\n");
            }
            {
                Indent();
                Emit(codeBlock, true);
                Unindent();
            }
            Emit("}");
        }
    }

    #endregion

    #region method

    private void EmitMethod(MethodSpec methodSpec, bool delegator = false) {
        if (methodSpec.IsConstructor && delegator) {
            throw new IllegalStateException();
        }
        Emit("\n"); // 方法前空一行
        EmitDocument(methodSpec.document);
        Emit(methodSpec.headerCode, true);
        EmitAttributes(methodSpec.attributes);

        if (methodSpec.IsConstructor) {
            EmitModifiers(methodSpec.modifiers);
            // 考虑直接EmitMethod的情况
            if (typeSpecStack.TryPeek(out TypeSpec? typeSpec)) {
                Emit(typeSpec.name);
            } else {
                Emit(methodSpec.name);
            }
            EmitMethodParameters(methodSpec);
            // 调用其它构造方法 -- 固定换行
            if (!CodeBlock.IsNullOrEmpty(methodSpec.constructorInvoker)) {
                Emit("\n");
                Emit(" : ");
                Emit(methodSpec.constructorInvoker!);
            }
            Emit(" ");
            EmitMethodBody(methodSpec);
        } else {
            // public Sum<T1, T2>(T1 arg1, T2 arg2) where T1 {}
            // 显式实现时，不能有修饰符
            if (methodSpec.explicitBaseType == null) {
                EmitModifiers(methodSpec.modifiers, delegator);
            }
            // 返回值
            EmitTypeName(methodSpec.returnType);
            Emit(" ");

            // 显式实现时，指定接口类型
            if (methodSpec.explicitBaseType != null) {
                EmitTypeName(methodSpec.explicitBaseType);
                Emit(".");
            }
            Emit(methodSpec.name);
            EmitTypeVariables(methodSpec.typeVariables);
            EmitMethodParameters(methodSpec);

            // 泛型变量约束
            if (HasConstraints(methodSpec.typeVariables)) {
                Emit(" ");
                EmitTypeVariableConstraints(methodSpec.typeVariables);
            }

            if (delegator
                || methodSpec.code == null
                || (methodSpec.modifiers & Modifiers.Abstract) != 0
                || (methodSpec.modifiers & Modifiers.Extern) != 0) {
                Emit(";");
            } else {
                Emit(" ");
                EmitMethodBody(methodSpec);
            }
        }
        Emit("\n"); // 每个元素末尾都默认换行
    }

    private void EmitMethodParameters(MethodSpec methodSpec) {
        if (methodSpec.parameters.Count == 0) {
            Emit("()");
            return;
        }
        Emit("(");
        for (var index = 0; index < methodSpec.parameters.Count; index++) {
            ParameterSpec parameter = methodSpec.parameters[index];
            if (index > 0) {
                Emit(", ");
            }
            // 变长参数，追加 params -- 还是更喜欢...
            if (methodSpec.varargs && index == methodSpec.parameters.Count - 1) {
                Emit("params ");
            }

            EmitTypeName(parameter.type);
            Emit(" ");
            Emit(parameter.name);

            // 默认值
            if (!CodeBlock.IsNullOrEmpty(parameter.defaultValue)) {
                Emit(" = ");
                Emit(parameter.defaultValue!);
            }
        }
        Emit(")");
    }

    /** 外部统一末尾换行 */
    private void EmitMethodBody(MethodSpec methodSpec) {
        if (CodeBlock.IsNullOrEmpty(methodSpec.code)) {
            Emit("{\n");
            Emit("}");
            return;
        }
        if (methodSpec.code!.expressionStyle) {
            Emit("=> ");
            Emit(methodSpec.code);
            EmitIfLastCharNot(';'); // 代码可能包含';'
            return;
        }
        Emit("{\n");
        {
            Indent();
            Emit(methodSpec.code!, true); // 代码本身可能包含换行符
            Unindent();
        }
        Emit("}");
    }

    #endregion

    #region enumvalue

    private void EmitEnumValue(EnumValueSpec enumValueSpec) {
        if (!enumValueSpec.document.IsEmpty) {
            EmitDocument(enumValueSpec.document);
        }
        if (!enumValueSpec.number.HasValue) {
            Emit("$L,\n", enumValueSpec.name);
        } else {
            Emit("$L = $L,\n", enumValueSpec.name, enumValueSpec.number.Value);
        }
    }

    #endregion

    #region modifier

    private readonly List<string> _pooledModifierList = new List<string>(4);

    /// <summary>
    /// 写入修饰符。
    /// 写入修饰符后，尾部固定会写入一个空格。
    /// </summary>
    /// <param name="modifiers"></param>
    /// <param name="delegatorMethod"></param>
    /// <returns></returns>
    private bool EmitModifiers(Modifiers modifiers, bool delegatorMethod = false) {
        List<string> modifierList = _pooledModifierList;
        modifierList.Clear();

        if ((modifiers & Modifiers.Public) != 0) {
            modifierList.Add("public");
        }
        if ((modifiers & Modifiers.Protected) != 0) {
            modifierList.Add("protected");
        }
        if ((modifiers & Modifiers.Internal) != 0) {
            modifierList.Add("internal");
        }
        if ((modifiers & Modifiers.Private) != 0) {
            modifierList.Add("private");
        }
        if (delegatorMethod) {
            modifierList.Add("delegate");
            if ((modifiers & Modifiers.Unsafe) != 0) {
                modifierList.Add("unsafe");
            }
        } else {
            // public new static extern unsafe
            if ((modifiers & Modifiers.Hide) != 0) {
                modifierList.Add("new");
            }
            if ((modifiers & Modifiers.Static) != 0) {
                modifierList.Add("static");
            }
            if ((modifiers & Modifiers.Extern) != 0) {
                modifierList.Add("extern");
            }
            if ((modifiers & Modifiers.Unsafe) != 0) {
                modifierList.Add("unsafe");
            }
            // sealed override
            if ((modifiers & Modifiers.Abstract) != 0) {
                modifierList.Add("abstract");
            }
            if ((modifiers & Modifiers.Virtual) != 0) {
                modifierList.Add("virtual");
            }
            if ((modifiers & Modifiers.Sealed) != 0) {
                modifierList.Add("sealed");
            }
            if ((modifiers & Modifiers.Override) != 0) {
                modifierList.Add("override");
            }

            if ((modifiers & Modifiers.Readonly) != 0) {
                modifierList.Add("readonly");
            }
            if ((modifiers & Modifiers.Const) != 0) {
                modifierList.Add("const");
            }
            if ((modifiers & Modifiers.Partial) != 0) {
                modifierList.Add("partial");
            }
            if ((modifiers & Modifiers.Async) != 0) {
                modifierList.Add("async");
            }
            if ((modifiers & Modifiers.Operator) != 0) {
                modifierList.Add("operator");
            }
        }
        if (modifierList.Count == 0) {
            return false;
        }

        Emit("$L", string.Join(" ", modifierList));
        Emit(" ");
        return true;
    }

    #endregion

    #region typevar

    /// <summary>
    /// 发射泛型变量的定义。
    /// 注意，C#的泛型变量定义和约束是分开的。
    /// </summary>
    /// <param name="typeVariables"></param>
    private void EmitTypeVariables(IList<TypeVariableName> typeVariables) {
        if (typeVariables.Count == 0) return;

        Emit("<");
        bool firstTypeVariable = true;
        for (var i = 0; i < typeVariables.Count; i++) {
            TypeVariableName typeVariable = typeVariables[i];
            if (!firstTypeVariable) Emit(", ");
            Emit("$L", typeVariable.name);
            firstTypeVariable = false;
        }
        Emit(">");
    }

    /// <summary>
    /// 是否包含泛型变量约束
    /// </summary>
    /// <param name="typeVariables"></param>
    /// <returns></returns>
    private bool HasConstraints(IList<TypeVariableName> typeVariables) {
        foreach (TypeVariableName typeVariable in typeVariables) {
            if (typeVariable.HasConstraints()) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 发射泛型变量的约束
    /// </summary>
    /// <param name="typeVariables"></param>
    private void EmitTypeVariableConstraints(IList<TypeVariableName> typeVariables) {
        if (typeVariables.Count == 0) return;

        for (int i = 0; i < typeVariables.Count; i++) {
            TypeVariableName typeVariable = typeVariables[i];
            if (typeVariable.HasConstraints()) {
                EmitTypeVariableConstraints(typeVariable);
            }
        }
    }

    /// <summary>
    /// 发射单个泛型变量的约束。
    /// 不能直接调用<see cref="TypeVariableName.ConstraintsToString"/>，需要优化TypeName的输出。
    /// </summary>
    private void EmitTypeVariableConstraints(TypeVariableName typeVariable) {
        if (typeVariable.attributes == 0 && typeVariable.bounds.Count == 0) {
            return;
        }
        Emit(" where $L : ", typeVariable.name);
        if ((typeVariable.attributes & TypeNameAttributes.NotNullableValueTypeConstraint) != 0) {
            Emit("struct");
            return;
        }

        int count = 0;
        if ((typeVariable.attributes & TypeNameAttributes.ReferenceTypeConstraint) != 0) {
            Emit("class");
            count++;
        }
        if ((typeVariable.attributes & TypeNameAttributes.DefaultConstructorConstraint) != 0) {
            if (count++ > 0) Emit(", ");
            Emit("notnull");
        }
        if ((typeVariable.attributes & TypeNameAttributes.DefaultConstructorConstraint) != 0) {
            if (count++ > 0) Emit(", ");
            Emit("new()");
        }
        foreach (TypeName bound in typeVariable.bounds) {
            if (count++ > 0) Emit(", ");
            EmitTypeName(bound); // 打印优化的TypeName
        }
    }

    #endregion

    #region attribute

    /// <summary>
    /// 写对象/方法/属性的注解
    /// </summary>
    /// <param name="attributes"></param>
    private void EmitAttributes(IList<AttributeSpec> attributes) {
        if (attributes.Count == 0) return;
        for (int i = 0; i < attributes.Count; i++) {
            EmitAttribute(attributes[i]);
        }
    }

    private void EmitAttribute(AttributeSpec attributeSpec) {
        Emit("[");
        EmitTypeName(attributeSpec.type, true);
        if (!CodeBlock.IsNullOrEmpty(attributeSpec.constructor) || attributeSpec.props.Count > 0) {
            Emit("(");
            if (!CodeBlock.IsNullOrEmpty(attributeSpec.constructor)) {
                Emit(attributeSpec.constructor!);
            }
            if (attributeSpec.props.Count > 0) {
                Emit(", ");
            }
            for (int i = 0; i < attributeSpec.props.Count; i++) {
                KeyValuePair<string, CodeBlock> pair = attributeSpec.props[i];
                if (i > 0) Emit(", ");
                Emit(pair.Key);
                Emit(" = ");
                Emit(pair.Value);
            }
            Emit(")");
        }
        Emit("]");
        Emit("\n");
    }

    #endregion

    #region codeblock

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Emit(string format, params object[] args) {
        Emit(CodeBlock.Of(format, args), false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Emit(CodeBlock codeBlock) {
        Emit(codeBlock, false);
    }

    /// <summary>
    ///
    /// 不使用默认值，以方便我们查看引用
    /// </summary>
    /// <param name="codeBlock">要写的代码块</param>
    /// <param name="ensureTrailingNewline">是否末尾必须处于新行</param>
    private void Emit(CodeBlock codeBlock, bool ensureTrailingNewline) {
        IList<string> formatParts = codeBlock.formatParts;
        IList<object?> objectArgs = codeBlock.args;

        int argIndex = 0;
        for (int index = 0; index < formatParts.Count; index++) {
            string part = formatParts[index];
            switch (part) {
                case "$L": { // 字面量
                    EmitLiteral(objectArgs[argIndex++]);
                    break;
                }
                case "$N": { // Name
                    Emit((string)objectArgs[argIndex++]!);
                    break;
                }
                case "$S": { // String
                    string? s = (string?)objectArgs[argIndex++];
                    Emit(s == null ? "null" : Util.StringLiteralWithDoubleQuotes(s, indent));
                    break;
                }
                case "$T": { // Type
                    TypeName typeName = (TypeName)objectArgs[argIndex++]!;
                    EmitTypeName(typeName);
                    break;
                }
                case "$$": {
                    Emit("$");
                    break;
                }
                case "$>": {
                    Indent();
                    break;
                }
                case "$<": {
                    Unindent();
                    break;
                }
                case "$[": {
                    Util.CheckState(statementLine == -1, "statement enter $[ followed by statement enter $[");
                    statementLine = 0;
                    break;
                }
                case "$]": {
                    Util.CheckState(statementLine != -1, "statement exit $] has no matching statement enter $[");
                    if (statementLine > 0) {
                        Unindent(2); // End a multi-line statement. Decrease the indentation level.
                    }
                    statementLine = -1;
                    break;
                }
                case "$W": { // Whitespace
                    codeOut.WrappingSpace(indentLevel + 2);
                    break;
                }
                case "$Z": { // ZeroSpace
                    codeOut.ZeroWidthSpace(indentLevel + 2);
                    break;
                }
                default: {
                    Emit(part);
                    break;
                }
            }
        }
        if (ensureTrailingNewline && codeOut.LastChar != '\n') {
            Emit("\n");
        }
    }

    private void EmitLiteral(object? obj) {
        if (obj == null) {
            Emit("null");
            return;
        }
        if (obj is string s) {
            Emit(s);
        } else if (obj is bool boolValue) {
            // C#的bool toString是大驼峰 -- 巨坑
            Emit(boolValue ? "true" : "false");
        } else if (obj is ISpecification spec) {
            EmitSpec(spec);
        } else if (obj is CodeBlock codeBlock) {
            Emit(codeBlock);
        } else {
            Emit(obj.ToString()!);
        }
    }

    private readonly Stack<string> pooledTypeNameStack = new Stack<string>(4);

    private void EmitTypeName(TypeName typeName, bool isAttribute = false) {
        // System.String[]*[]&
        if (typeName is ByRefTypeName refTypeName) { // 引用类型
            if (refTypeName.kind == ByRefTypeName.Kind.In) {
                Emit("in ");
            } else if (refTypeName.kind == ByRefTypeName.Kind.Out) {
                Emit("out ");
            } else {
                Emit("ref ");
            }
            typeName = refTypeName.targetType;
        }
        Stack<string> typeNameStack = pooledTypeNameStack;
        typeNameStack.Clear();

        CollectTypeName(typeName, typeNameStack, isAttribute);
        Emit(string.Join("", typeNameStack));
    }

    private void CollectTypeName(TypeName typeName, Stack<string> typeNameStack, bool isAttribute) {
        if (typeName is ArrayTypeName arrayTypeName) { // 考虑指针数组
            PushNullableSymbol(typeName, typeNameStack);
            typeNameStack.Push(Util.ArrayRankSymbol(arrayTypeName.GetArrayRank()));
            typeName = arrayTypeName.GetRootElementType();
        }

        if (typeName is PointerTypeName pointerTypeName) { // 指针类型
            PushNullableSymbol(typeName, typeNameStack);
            typeNameStack.Push(Util.PointerRankSymbol(pointerTypeName.GetPointerRank()));
            typeName = pointerTypeName.GetRootTargetType();
        }

        if (typeName is ArrayTypeName arrayTypeName2) { // 考虑数组的指针
            PushNullableSymbol(typeName, typeNameStack);
            typeNameStack.Push(Util.ArrayRankSymbol(arrayTypeName2.GetArrayRank()));
            typeName = arrayTypeName2.GetRootElementType();
        }

        PushNullableSymbol(typeName, typeNameStack);
        if (TypeName.IsNullableStruct(typeName)) { // 处理Nullable<T>
            ClassName nullable = (ClassName)typeName;
            typeName = nullable.typeArguments[0];
        }
        if (typeName is ClassName className) {
            // 内部类总是打印外部类名，简化逻辑
            while (true) {
                if (className.declaredTypeArguments.Count > 0) {
                    typeNameStack.Push(">"); // 反向打印
                    for (int index = className.declaredTypeArguments.Count - 1; index >= 0; index--) {
                        TypeName typeArgument = className.declaredTypeArguments[index];
                        CollectTypeName(typeArgument, typeNameStack, false);
                        if (index > 0) {
                            typeNameStack.Push(", ");
                        }
                    }
                    typeNameStack.Push("<");
                }
                // 处理注解特殊语法...C#这些不必要的语法真的让人冒火
                if (typeNameStack.Count == 0
                    && isAttribute
                    && className.simpleName.EndsWith("Attribute")) {
                    string shortcutName = className.simpleName.Substring(0, className.simpleName.Length - 9);
                    typeNameStack.Push(shortcutName);
                } else {
                    typeNameStack.Push(className.simpleName);
                }
                if (className.enclosingClassName == null) {
                    break;
                }
                typeNameStack.Push(".");
                className = className.enclosingClassName;
            }
            // 有命名空间别名时，使用别名引用Type
            if (importableNamespaces.TryGetValue(className.ns, out string? alias) && alias != null) {
                typeNameStack.Push(".");
                typeNameStack.Push(alias);
            } else if (typeName.Internal_Keyword == null
                       && namespaceStack.TryPeek(out NamespaceSpec? namespaceSpec)
                       && namespaceSpec.name != className.ns) {
                // 基于关键字时不引入system，同命名空间下时也不引入
                importableNamespaces.TryAdd(className.ns, null);
            }
        } else if (typeName is TypeVariableName typeVariableName) {
            typeNameStack.Push(typeVariableName.name);
        } else {
            typeNameStack.Push(typeName.Internal_Keyword!);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushNullableSymbol(TypeName typeName, Stack<string> typeNameStack) {
        if ((typeName.attributes & TypeNameAttributes.NullableReferenceType) != 0
            || TypeName.IsNullableStruct(typeName)) {
            typeNameStack.Push("?");
        }
    }

    #endregion

    #region string

    /// <summary>
    /// 如果当前最后一个字符不是给定的char，则写入char。
    /// 通常用于追加缩进字符和分隔符
    /// </summary>
    /// <param name="c"></param>
    private void EmitIfLastCharNot(char c) {
        if (codeOut.LastChar == c) return;
        switch (c) {
            case ' ':
                Emit(" ");
                break;
            case ';':
                Emit(";");
                break;
            case '\n':
                Emit("\n");
                break;
            default:
                Emit(c.ToString());
                break;
        }
    }

    /// <summary>
    /// 写入一个字符串。
    /// 所有写入的代码都要在这里执行，因为我们惰性地发出缩进以避免不必要的尾随空格（缩进）。
    /// </summary>
    /// <param name="s"></param>
    private void Emit(string s) {
        // 对单行字符串做下优化，避免不必要的Lines调用
        if (s.LastIndexOf('\n') < 0) {
            Internal_Emit(s);
            return;
        }
        // 对单独的换行符做优化，避免不必要的字符串切割
        if (s == "\n") {
            Internal_EmitNewLine();
            return;
        }

        bool first = true;
        foreach (string line in s.Lines()) {
            if (!first) {
                Internal_EmitNewLine();
            }
            first = false;
            Internal_Emit(line);
        }
        // java使用的 split("\\R", -1) "\n"会被拆解为两个空字符串，c#端未找到合适的等价方法，我们在末尾追加一次处理
        if (s[s.Length - 1] == '\n') {
            Internal_EmitNewLine();
        }
    }

    /// <summary>
    /// 写入新行
    /// </summary>
    private void Internal_EmitNewLine() {
        // Emit a newline character. Make sure blank lines in document & comments look good.
        if ((document || comment) && trailingNewline) {
            EmitIndentation();
            codeOut.Append(document ? "///" : "//");
        }
        codeOut.Append("\n");
        trailingNewline = true;
        if (statementLine != -1) {
            if (statementLine == 0) {
                Indent(2); // Begin multiple-line statement. Increase the indentation level.
            }
            statementLine++;
        }
    }

    /// <summary>
    /// 写入文本内容
    /// </summary>
    /// <param name="line"></param>
    private void Internal_Emit(string line) {
        if (line.Length == 0) return;

        // Emit indentation and comment prefix if necessary.
        if (trailingNewline) {
            EmitIndentation();
            if (document) {
                codeOut.Append("/// ");
            } else if (comment) {
                codeOut.Append("// ");
            }
        }

        codeOut.Append(line);
        trailingNewline = false;
    }

    private void EmitIndentation() {
        for (int j = 0; j < indentLevel; j++) {
            codeOut.Append(indent);
        }
    }

    private void Indent(int levels = 1) {
        Util.CheckArgument(levels >= 0, "cannot Indent {0} from {1}", levels, indentLevel);
        indentLevel += levels;
    }

    private void Unindent(int levels = 1) {
        Util.CheckArgument(levels >= 0 && (indentLevel - levels) >= 0, "cannot unindent {0} from {1}", levels, indentLevel);
        indentLevel -= levels;
    }

    #endregion
}