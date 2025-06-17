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
using System.Collections.Generic;
using System.Text;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 表示一个元组类型的引用
///
/// 元组其实超出了类型引用的范畴...
/// </summary>
public class RecordTypeName : TypeName
{
    public readonly IList<RecordElement> elements;

    private RecordTypeName(IList<RecordElement> elements, TypeNameAttributes attributes = TypeNameAttributes.None)
        : base(attributes) {
        this.elements = Util.ToImmutableList(elements);
    }

    #region override

    /// <summary>
    /// 元组的反射名不能用于加载
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() {
        StringBuilder sb = new StringBuilder(32);
        sb.Append('(');
        for (int index = 0; index < elements.Count; index++) {
            if (index > 0) {
                sb.Append(", ");
            }
            RecordElement element = elements[index];
            sb.Append(element.type.ReflectionName());
            if (!string.IsNullOrWhiteSpace(element.name)) {
                sb.Append(' ');
                sb.Append(element.name);
            }
        }
        sb.Append(')');
        return sb.ToString();
    }

    protected override string ToStringImpl() {
        StringBuilder sb = new StringBuilder(32);
        sb.Append(GetType().Name);
        sb.Append('(');
        for (int index = 0; index < elements.Count; index++) {
            if (index > 0) {
                sb.Append(", ");
            }
            RecordElement element = elements[index];
            sb.Append(element.type);
            if (!string.IsNullOrWhiteSpace(element.name)) {
                sb.Append(' ');
                sb.Append(element.name);
            }
        }
        sb.Append(')');
        return sb.ToString();
    }

#if NET6_0_OR_GREATER
    public override RecordTypeName WithAttributes(TypeNameAttributes attributes) {
#else
    public override TypeName WithAttributes(TypeNameAttributes attributes) {
#endif
        if (this.attributes == attributes) return this;
        return new RecordTypeName(elements, attributes);
    }

#if NET6_0_OR_GREATER
    public override RecordTypeName RemoveAllNullableAttribute() {
#else
    public override TypeName RemoveAllNullableAttribute() {
#endif
        List<RecordElement> tempElements = new List<RecordElement>(elements.Count);
        foreach (RecordElement element in elements) {
            tempElements.Add(element.RemoveNullableAttribute());
        }
        return new RecordTypeName(tempElements, attributes.Unset(TypeNameAttributes.NullableReferenceType));
    }

    #endregion

    #region Parse/Get

    public static RecordTypeName Get(IList<RecordElement> elements, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new RecordTypeName(elements, attributes);
    }

    public static RecordTypeName Get(Dictionary<Type, string?> elementMap,
                                     TypeNameAttributes attributes = TypeNameAttributes.None) {
        List<RecordElement> list = new List<RecordElement>(elementMap.Count);
        foreach (var pair in elementMap) {
            list.Add(new RecordElement(TypeName.Get(pair.Key), pair.Value));
        }
        return new RecordTypeName(list, attributes);
    }

    #endregion
}
}