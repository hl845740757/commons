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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// 1. 读取数组内普通成员时，name传null或零值，读取嵌套对象时使用无name参数的start方法
/// 2. 为减少API数量，我们的所有简单值读取都是带有name参数的，在已读取name的情况下，接口的name参数将被忽略。
/// </summary>
/// <typeparam name="TName">name的类型，string或<see cref="FieldNumber"/></typeparam>
public interface IDsonReader<TName> : IDisposable where TName : IEquatable<TName>
{
    #region Ctx

    /// <summary>
    /// 获取当前上下文的类型
    /// </summary>
    DsonContextType ContextType { get; }

    /// <summary>
    /// 当前是否处于应该读取type状态
    /// </summary>
    bool IsAtType { get; }

    /// <summary>
    /// 读取下一个值的类型
    /// 如果到达对象末尾，则返回{@link DsonType#END_OF_OBJECT}
    /// 
    /// 循环的基本写法：
    /// <code>
    ///  DsonType dsonType;
    ///  while((dsonType = ReadDsonType()) != DsonType.END_OF_OBJECT) {
    ///      ReadName();
    ///      ReadValue();
    ///  }
    /// </code>
    /// </summary>
    /// <returns></returns>
    DsonType ReadDsonType();

    /// <summary>
    /// 查看下一个值的类型
    /// 1.该方法对于解码很有帮助，最常见的作用是判断是否写入了header
    /// 2.不论是否支持mark和reset，定义该方法都是必要的，以允许实现类以最小的代价实现
    /// </summary>
    /// <returns></returns>
    DsonType PeekDsonType();

    /// <summary>
    /// 当前是否处于应该读取name状态
    /// </summary>
    bool IsAtName { get; }

    /// <summary>
    /// 读取下一个值的name
    /// </summary>
    /// <returns></returns>
    TName ReadName();

    /// <summary>
    /// 读取下一个值的name
    /// 如果下一个name不等于期望的值，则抛出异常
    /// PS：对于int类型会产生装箱，但目前暂不优化。
    /// </summary>
    /// <param name="name"></param>
    void ReadName(TName name);

    /// <summary>
    /// 当前是否处于应该读取value状态
    /// </summary>
    /// <returns></returns>
    bool IsAtValue { get; }

    /// <summary>
    /// 获取当前的数据类型
    /// 1.该值在调用任意的读方法后将变化
    /// 2.如果尚未执行过{@link #ReadDsonType()}则抛出异常
    /// </summary>
    /// <returns></returns>
    DsonType CurrentDsonType { get; }

    //
    /// <summary>
    /// 获取当前的字段名字
    /// 1.该值在调用任意的读方法后将变化
    /// 2.只有在读取值状态下才可访问
    /// </summary>
    TName CurrentName { get; }

    #endregion

    #region 简单值

    int ReadInt32(TName name);

    long ReadInt64(TName name);

    float ReadFloat(TName name);

    double ReadDouble(TName name);

    bool ReadBool(TName name);

    string ReadString(TName name);

    void ReadNull(TName name);

    Binary ReadBinary(TName name);

    ObjectPtr ReadPtr(TName name);

    ObjectLitePtr ReadLitePtr(TName name);

    ExtDateTime ReadDateTime(TName name);

    Timestamp ReadTimestamp(TName name);

    #endregion

    #region 容器

    void ReadStartArray();

    void ReadEndArray();

    void ReadStartObject();

    void ReadEndObject();

    void ReadStartHeader();

    void ReadEndHeader();

    /// <summary>
    /// 回退到等待开始状态
    /// 1.该方法只回退状态，不回退输入
    /// 2.只有在等待读取下一个值的类型时才可以执行，即等待<see cref="ReadDsonType"/>时才可以执行
    /// 3.通常用于在读取header之后回退，然后让业务对象的codec去解码
    /// </summary>
    void BackToWaitStart();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadStartArray(TName name) {
        ReadName(name);
        ReadStartArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadStartObject(TName name) {
        ReadName(name);
        ReadStartObject();
    }

    #endregion

    #region 特殊

    /// <summary>
    /// 跳过当前name
    /// 如果当前是数组上下文，则不产生影响；
    /// 如果当前是Object上下文，且处于读取Name状态则跳过name，否则抛出状态异常
    /// </summary>
    void SkipName();

    /// <summary>
    /// 跳过当前值
    /// 如果当前不处于读值状态则抛出状态异常
    /// </summary>
    void SkipValue();

    /// <summary>
    /// 跳过当前容器对象(Array、Object、Header)的剩余内容
    /// 调用该方法后，{@link #getCurrentDsonType()}将返回{@link DsonType#END_OF_OBJECT}
    /// 也就是说，调用该方法后应立即调用 ReadEnd 相关方法
    /// </summary>
    void SkipToEndOfObject();

    /// <summary>
    /// 将value的值读取为字节数组
    /// 1.支持类型：String、Binary、Array、Object、Header;
    /// 2.返回的bytes中去除了value的length信息;
    /// 3.只在二进制流下生效。
    /// 
    /// 该方法主要用于避免中间编解码过程，eg：
    /// A端：             B端            C端
    /// object->bytes  bytes->bytes  bytes->object
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    byte[] ReadValueAsBytes(TName name);

#nullable disable
    /// <summary>
    /// 附近一个数据到当前上下文
    /// </summary>
    /// <param name="userData">用户自定义数据</param>
    /// <returns>旧值</returns>
    object Attach(object userData);

    /// <summary>
    /// 获取附加到当前上下文的数据
    /// </summary>
    /// <returns></returns>
    object Attachment();

    /// <summary>
    /// 读操作指导
    /// </summary>
    /// <returns></returns>
    DsonReaderGuide WhatShouldIDo();

    #endregion
}
}