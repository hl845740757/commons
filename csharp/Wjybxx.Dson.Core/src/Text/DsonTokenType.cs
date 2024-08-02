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

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 文本token类型
/// </summary>
public enum DsonTokenType
{
    /** 到达文件尾部 */
    Eof,

    /** 对象开始符， '{' */
    BeginObject,
    /** 对象结束符， '}' */
    EndObject,

    /** 数组开始符，'[' */
    BeginArray,
    /** 数组结束符，']' */
    EndArray,

    /** 对象头开始符 '@{' -- @{k1:v1, k2:v2}} */
    BeginHeader,
    /** 简单对象头 '@{‘ -- @{clsName} */
    SimpleHeader,

    /** KV分隔符，冒号 ':' */
    Colon,
    /** 元素分隔符，英文逗号 ',' */
    Comma,

    /** 显式声明 '@i' */
    Int32,
    /** 显式声明 '@L' */
    Int64,
    /** 显式声明 '@f' */
    Float,
    /** 显式声明 '@d' */
    Double,
    /** 显式声明 '@b' */
    Bool,
    /** 显式声明 双引号 或 '@s' 或 '@ss' */
    String,
    /** 显式声明 '@N' */
    Null,
    /** 显式声明 '@bin' */
    Binary,

    /** 内建结构体 */
    BuiltinStruct,
    /** 无引号字符串，scan的时候不解析，使得返回后可以根据上下文推断其类型 */
    UnquoteString,
}
}