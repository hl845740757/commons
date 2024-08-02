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

using System;
using System.Runtime.CompilerServices;

namespace Wjybxx.Dson
{
/// <summary>
/// 字段编号
/// 在使用int表达类字段名字的时候，编号并不是随意的，而是具有编码的；字段的编号由 继承深度+本地编号 构成，
/// 
/// 1.Dson最初是为序列化设计的，是支持继承的。
/// 2.完整编号不可直接比较，需要调用这里提供的静态比较方法
/// </summary>
public readonly struct FieldNumber : IEquatable<FieldNumber>, IComparable<FieldNumber>, IComparable
{
    public static readonly FieldNumber ZERO = new FieldNumber(0);

    private readonly int _fullNumber;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="idep">继承深度</param>
    /// <param name="lnumber">字段本地编号</param>
    public FieldNumber(byte idep, int lnumber) {
        if (idep > Dsons.IdepMaxValue) {
            throw InvalidArgs(idep, lnumber);
        }
        if (lnumber < 0) {
            throw InvalidArgs(idep, lnumber);
        }
        this._fullNumber = Dsons.MakeFullNumber(idep, lnumber);
    }

    private FieldNumber(int fullNumber) {
        this._fullNumber = fullNumber;
    }

    private static Exception InvalidArgs(int idep, int lnumber) {
        throw new ArgumentException($"idep: {idep}, lnumber: {lnumber}");
    }

    /// <summary>
    /// 继承深度
    /// </summary>
    public byte Idep => Dsons.IdepOfFullNumber(_fullNumber);
    /// <summary>
    /// 字段本地编码
    /// </summary>
    public int Lnumber => Dsons.LnumberOfFullNumber(_fullNumber);

    /// <summary>
    /// 字段的完整编号
    /// 注意：完整编号不可直接比较，需要调用这里提供的静态比较方法
    /// </summary>
    public int FullNumber => _fullNumber;

    /// <summary>
    /// 通过字段本地编号创建结构，默认继承深度0
    /// </summary>
    /// <param name="lnumber">字段本地编号</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldNumber OfLnumber(int lnumber) {
        return new FieldNumber(0, lnumber);
    }

    /// <summary>
    /// 通过字段的完整编号创建结构
    /// </summary>
    /// <param name="fullNumber">字段完整编号</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldNumber OfFullNumber(int fullNumber) {
        return new FieldNumber(fullNumber);
    }

    /// <summary>
    /// 比较两个字段的大小
    /// </summary>
    /// <param name="fullNumber1">字段完整编号</param>
    /// <param name="fullNumber2">字段完整编号</param>
    /// <returns></returns>
    public static int Compare(int fullNumber1, int fullNumber2) {
        if (fullNumber1 == fullNumber2) {
            return 0;
        }
        // 先比较继承深度 -- 父类字段靠前
        var idepComparison = Dsons.IdepOfFullNumber(fullNumber1)
            .CompareTo(Dsons.IdepOfFullNumber(fullNumber2));
        if (idepComparison != 0) {
            return idepComparison;
        }
        // 再比较类
        return Dsons.LnumberOfFullNumber(fullNumber1)
            .CompareTo(Dsons.LnumberOfFullNumber(fullNumber2));
    }

    #region equals

    public bool Equals(FieldNumber other) {
        return _fullNumber == other._fullNumber;
    }

    public override bool Equals(object? obj) {
        return obj is FieldNumber other && Equals(other);
    }

    public override int GetHashCode() {
        return _fullNumber;
    }

    #endregion

    #region compare

    public int CompareTo(FieldNumber other) {
        return Compare(_fullNumber, other._fullNumber);
    }

    public int CompareTo(object? obj) {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is FieldNumber other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(FieldNumber)}");
    }

    public static bool operator <(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(FieldNumber left, FieldNumber right) {
        return left.CompareTo(right) >= 0;
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Idep)}: {Idep}, {nameof(Lnumber)}: {Lnumber}";
    }
}
}