#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System.Collections.Generic;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson对象头
///
/// 1.Header不可以再持有header，否则陷入死循环
/// 2.Header的结构应该是简单清晰的，可简单编解码的 -- 不应该继承。
/// 3.header的number编号不遵循<see cref="Dsons.MakeFullNumber"/>规则，而是每一个字段编号都是固定的。
/// </summary>
public class DsonHeader<TK> : AbstractDsonObject<TK>
{
    public DsonHeader()
        : base(new ArrayDictionary<TK, DsonValue>()) {
    }

    public DsonHeader(IDictionary<TK, DsonValue> valueMap)
        : base(new ArrayDictionary<TK, DsonValue>(valueMap)) {
    }

    public override DsonType DsonType => DsonType.Header;

    public new DsonHeader<TK> Append(TK key, DsonValue value) {
        base.Append(key, value);
        return this;
    }
}

/// <summary>
/// 定义DsonHeader常量
/// </summary>
public static class DsonHeader
{
    // header常见属性名
    public const string Names_ClassName = "clsName";
    public const string Names_LocalId = "localId";
    public const string Names_LocalName = "localName";
    public const string Names_Count = "count";
    public const string Names_Namespace = "ns";
}
}