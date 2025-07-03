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

using System.Runtime.CompilerServices;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson所有值类型的抽象
/// </summary>
public abstract class DsonValue
{
    public abstract DsonType DsonType { get; }

    #region 拆箱类型

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AsInt32() => ((DsonInt32)this).IntValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long AsInt64() => ((DsonInt64)this).LongValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float AsFloat() => ((DsonFloat)this).FloatValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double AsDouble() => ((DsonDouble)this).DoubleValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AsBool() => ((DsonBool)this).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string AsString() => ((DsonString)this).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Binary AsBinary() => ((DsonBinary)this).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObjectPtr AsPointer() => ((DsonPointer)this).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObjectLitePtr AsLitePointer() => ((DsonLitePointer)this).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ExtDateTime AsDateTime() => ((DsonDateTime)this).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Timestamp AsTimestamp() => ((DsonTimestamp)this).Value;

    #endregion

    #region number

    public bool IsNumber => DsonType.IsNumber();

    public DsonNumber AsDsonNumber() => ((DsonNumber)this);

    #endregion

    #region Dson特定类型

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonHeader<T> AsHeader<T>() => (DsonHeader<T>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonArray<T> AsArray<T>() => (DsonArray<T>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonObject<T> AsObject<T>() => (DsonObject<T>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonHeader<string> AsHeader() => (DsonHeader<string>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonArray<string> AsArray() => (DsonArray<string>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonObject<string> AsObject() => (DsonObject<string>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonHeader<int> AsHeaderLite() => (DsonHeader<int>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonArray<int> AsArrayLite() => (DsonArray<int>)this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DsonObject<int> AsObjectLite() => (DsonObject<int>)this;

    #endregion
}
}