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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 从文本读取Dson对象的Reader
/// </summary>
public sealed class DsonTextReader : AbstractDsonReader<string>
{
    private static readonly List<DsonTokenType> VALUE_SEPARATOR_TOKENS =
        CollectionUtil.NewList(DsonTokenType.Comma, DsonTokenType.EndObject, DsonTokenType.EndArray);

    private static readonly DsonToken TOKEN_BEGIN_HEADER = new DsonToken(DsonTokenType.BeginHeader, "@{", -1);
    private static readonly DsonToken TOKEN_CLASSNAME = new DsonToken(DsonTokenType.UnquoteString, DsonHeaders.Names_ClassName, -1);
    private static readonly DsonToken TOKEN_COLON = new DsonToken(DsonTokenType.Colon, ":", -1);
    private static readonly DsonToken TOKEN_END_OBJECT = new DsonToken(DsonTokenType.EndObject, "}", -1);

#nullable disable
    private DsonScanner _scanner;
    private string _nextName;
    private object _nextValue;
    /** 值类型，用于避免拆装箱，减少gc */
    private UnionValue _nextUnionValue;

    private bool _marking;
    private readonly Stack<DsonToken> _pushedTokenQueue = new Stack<DsonToken>(6); // 缓存的Token
    private readonly List<DsonToken> _markedTokenQueue = new List<DsonToken>(6); // C#没有现成的Deque，我们拿List实现

    public DsonTextReader(DsonTextReaderSettings settings, string dsonString)
        : this(settings, new DsonScanner(dsonString)) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, TextReader reader, bool? autoClose = null)
        : this(settings, new DsonScanner(IDsonCharStream.NewBufferedCharStream(reader, autoClose ?? settings.autoClose))) {
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonScanner scanner)
        : base(settings) {
        this._scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        SetContext(context);
    }

    /**
     * 用于动态指定成员数据类型
     * 1.这对于精确解析数组元素和Object的字段十分有用 -- 比如解析一个{@code Vector3}的时候就可以指定字段的默认类型为float。
     * 2.辅助方法见：{@link DsonTexts#clsNameTokenOfType(DsonType)}
     */
    public void SetCompClsNameToken(DsonToken? dsonToken) {
        GetContext().compClsNameToken = dsonToken;
    }

    public new DsonTextReaderSettings Settings => (DsonTextReaderSettings)settings;

    private new Context GetContext() {
        return (Context)context;
    }

    public override void Dispose() {
        Context context = GetContext();
        SetContext(null);
        while (context != null) {
            Context parent = context.Parent;
            contextPool.Release(context);
            context = parent;
        }
        if (_scanner != null) {
            _scanner.Dispose();
            _scanner = null;
        }
        _pushedTokenQueue.Clear();
        _nextName = null;
        _nextValue = null;
        _nextUnionValue = default;
        _marking = false;
        _markedTokenQueue.Clear();
        base.Dispose();
    }

    #region token

    /// <summary>
    /// 保存Stack的原始快照
    /// </summary>
    private void InitMarkQueue() {
        if (_pushedTokenQueue.Count <= 0) {
            return;
        }
        foreach (var dsonToken in _pushedTokenQueue) {
            _markedTokenQueue.Add(dsonToken);
        }
    }

    /// <summary>
    /// 通过快照恢复Stack
    /// </summary>
    private void ResetPushedQueue() {
        _pushedTokenQueue.Clear();
        for (int i = _markedTokenQueue.Count - 1; i >= 0; i--) {
            _pushedTokenQueue.Push(_markedTokenQueue[i]);
        }
        _markedTokenQueue.Clear();
    }

    private DsonToken PopToken() {
        if (_pushedTokenQueue.Count == 0) {
            DsonToken dsonToken = _scanner.NextToken();
            if (_marking) {
                _markedTokenQueue.Add(dsonToken);
            }
            return dsonToken;
        } else {
            return _pushedTokenQueue.Pop();
        }
    }

    private DsonToken SkipToken() {
        if (_pushedTokenQueue.Count == 0) {
            return _scanner.NextToken(skipValue: true);
        }
        return _pushedTokenQueue.Pop();
    }

    private void PushToken(DsonToken token) {
        // if (token == null) throw new ArgumentNullException(nameof(token));
        _pushedTokenQueue.Push(token);
    }

    private void PushNextValue(object nextValue) {
        if (nextValue == null) throw new ArgumentNullException(nameof(nextValue));
        this._nextValue = nextValue;
    }

    private object PopNextValue() {
        object r = this._nextValue!;
        this._nextValue = null;
        return r;
    }

    private void PushNextUnionValue(in UnionValue nextValue) {
        this._nextValue = null;
        this._nextUnionValue = nextValue; // copy
    }

    private UnionValue PopNextUnionValue() {
        UnionValue r = this._nextUnionValue; // copy
        this._nextUnionValue.type = DsonType.EndOfObject; // 标记无效
        return r;
    }

    private void PushNextName(string nextName) {
        this._nextName = nextName ?? throw new ArgumentNullException(nameof(nextName));
    }

    private string PopNextName() {
        string r = this._nextName;
        this._nextName = null;
        return r;
    }

    #endregion

    #region state

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        DsonType dsonType = ReadDsonTypeOfToken();
        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VarInt;
        this.currentName = default!;

        OnReadDsonType(context, dsonType);
        if (dsonType == DsonType.Header) {
            context.headerCount++;
        } else {
            context.count++;
        }
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        _marking = true;
        InitMarkQueue(); // 保存Stack

        DsonType dsonType = ReadDsonTypeOfToken();
        _nextName = null; // 丢弃临时数据
        _nextValue = null;
        _nextUnionValue = default;

        ResetPushedQueue(); // 恢复Stack
        _marking = false;
        return dsonType;
    }

    private DsonType ReadDsonTypeOfToken() {
        // 丢弃旧值
        _nextName = null;
        _nextValue = null;
        _nextUnionValue = default;

        Context context = GetContext();
        // 统一处理逗号分隔符，顶层对象之间可不写分隔符
        if (context.count > 0) {
            DsonToken nextToken = PopToken();
            if (context.contextType != DsonContextType.TopLevel) {
                VerifyTokenType(context, nextToken, VALUE_SEPARATOR_TOKENS);
            }
            if (nextToken.type == DsonTokenType.Comma) {
                // 禁止末尾逗号
                DsonToken nnToken = PopToken();
                PushToken(nnToken);
                if (nnToken.type == DsonTokenType.EndObject || nnToken.type == DsonTokenType.EndArray) {
                    throw DsonIOException.InvalidTokenType(context.contextType, nextToken);
                }
            } else {
                PushToken(nextToken);
            }
        }

        // object/header 需要先读取 name和冒号，但object可能出现header
        if (context.contextType == DsonContextType.Object || context.contextType == DsonContextType.Header) {
            DsonToken nameToken = PopToken();
            switch (nameToken.type) {
                case DsonTokenType.String:
                case DsonTokenType.UnquoteString: {
                    PushNextName(nameToken.StringValue());
                    break;
                }
                case DsonTokenType.BeginHeader: {
                    if (context.contextType == DsonContextType.Header) {
                        throw DsonIOException.ContainsHeaderDirectly(nameToken);
                    }
                    EnsureCountIsZero(context, nameToken);
                    PushNextValue(nameToken);
                    return DsonType.Header;
                }
                case DsonTokenType.EndObject: {
                    return DsonType.EndOfObject;
                }
                default: {
                    throw DsonIOException.InvalidTokenType(context.contextType, nameToken,
                        CollectionUtil.NewList(DsonTokenType.String, DsonTokenType.UnquoteString, DsonTokenType.EndObject));
                }
            }
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);
        }

        // 走到这里，表示 top/object/header/array 读值
        DsonToken valueToken = PopToken();
        switch (valueToken.type) {
            case DsonTokenType.Int32: {
                PushNextUnionValue(valueToken.unionValue);
                return DsonType.Int32;
            }
            case DsonTokenType.Int64: {
                PushNextUnionValue(valueToken.unionValue);
                return DsonType.Int64;
            }
            case DsonTokenType.Float: {
                PushNextUnionValue(valueToken.unionValue);
                return DsonType.Float;
            }
            case DsonTokenType.Double: {
                PushNextUnionValue(valueToken.unionValue);
                return DsonType.Double;
            }
            case DsonTokenType.Bool: {
                PushNextUnionValue(valueToken.unionValue);
                return DsonType.Bool;
            }
            case DsonTokenType.String: {
                PushNextValue(valueToken.StringValue());
                return DsonType.String;
            }
            case DsonTokenType.Null: {
                PushNextValue(DsonNull.NULL);
                return DsonType.Null;
            }
            case DsonTokenType.Binary: {
                PushNextValue(valueToken.objValue);
                return DsonType.Binary;
            }
            case DsonTokenType.BuiltinStruct: return ParseAbbreviatedStruct(context, valueToken);
            case DsonTokenType.UnquoteString: return ParseUnquoteStringToken(context, valueToken);
            case DsonTokenType.BeginObject: return ParseBeginObjectToken(context, valueToken);
            case DsonTokenType.BeginArray: return ParseBeginArrayToken(context, valueToken);
            case DsonTokenType.BeginHeader: {
                // object的header已经处理，这里只有topLevel和array可以再出现header
                if (context.contextType.IsObjectLike()) {
                    throw DsonIOException.InvalidTokenType(context.contextType, valueToken);
                }
                EnsureCountIsZero(context, valueToken);
                return DsonType.Header;
            }
            case DsonTokenType.EndArray: {
                // endArray 只能在数组上下文出现；Array是在读取下一个值的时候结束；而Object必须在读取下一个name的时候结束
                if (context.contextType == DsonContextType.Array) {
                    return DsonType.EndOfObject;
                }
                throw DsonIOException.InvalidTokenType(context.contextType, valueToken);
            }
            case DsonTokenType.Eof: {
                // eof 只能在顶层上下文出现
                if (context.contextType == DsonContextType.TopLevel) {
                    return DsonType.EndOfObject;
                }
                throw DsonIOException.InvalidTokenType(context.contextType, valueToken);
            }
            default: {
                throw DsonIOException.InvalidTokenType(context.contextType, valueToken);
            }
        }
    }

    /** 字符串默认解析规则 */
    private DsonType ParseUnquoteStringToken(Context context, DsonToken valueToken) {
        string unquotedString = valueToken.StringValue();
        // 处理header的特殊属性依赖
        if (context.contextType == DsonContextType.Header) {
            switch (_nextName) {
                case DsonHeaders.Names_ClassName:
                    PushNextValue(unquotedString);
                    return DsonType.String;
                case DsonHeaders.Names_LocalId: {
                    return ParseLocalId(unquotedString);
                }
            }
        }
        // 处理类型传递
        if (context.compClsNameToken.HasValue) {
            switch (context.compClsNameToken.Value.StringValue()) {
                case DsonTexts.LabelInt32: {
                    PushNextUnionValue(new UnionValue(DsonType.Int32)
                    {
                        iValue = DsonTexts.ParseInt32(unquotedString)
                    });
                    return DsonType.Int32;
                }
                case DsonTexts.LabelInt64: {
                    PushNextUnionValue(new UnionValue(DsonType.Int64)
                    {
                        lValue = DsonTexts.ParseInt64(unquotedString)
                    });
                    return DsonType.Int64;
                }
                case DsonTexts.LabelFloat: {
                    PushNextUnionValue(new UnionValue(DsonType.Float)
                    {
                        fValue = DsonTexts.ParseFloat(unquotedString)
                    });
                    return DsonType.Float;
                }
                case DsonTexts.LabelDouble: {
                    PushNextUnionValue(new UnionValue(DsonType.Double)
                    {
                        dValue = DsonTexts.ParseDouble(unquotedString)
                    });
                    return DsonType.Double;
                }
                case DsonTexts.LabelBool: {
                    PushNextUnionValue(new UnionValue(DsonType.Bool)
                    {
                        bValue = DsonTexts.ParseBool(unquotedString)
                    });
                    return DsonType.Bool;
                }
                case DsonTexts.LabelString: {
                    PushNextValue(unquotedString);
                    return DsonType.String;
                }
                case DsonTexts.LabelBinary: {
                    byte[] bytes = CommonsLang3.FromHexString(unquotedString);
                    PushNextValue(bytes); // 直接压入bytes避免Binary再装箱
                    return DsonType.Binary;
                }
            }
        }

        // 处理特殊值解析
        bool isTrueString = "true" == unquotedString;
        if (isTrueString || "false" == unquotedString) {
            PushNextUnionValue(new UnionValue(DsonType.Bool)
            {
                bValue = isTrueString
            });
            return DsonType.Bool;
        }
        if ("null" == unquotedString) {
            PushNextValue(DsonNull.NULL);
            return DsonType.Null;
        }
        if (DsonTexts.IsParsable(unquotedString)) {
            PushNextUnionValue(new UnionValue(DsonType.Double)
            {
                dValue = DsonTexts.ParseDouble(unquotedString)
            });
            return DsonType.Double;
        }
        PushNextValue(unquotedString);
        return DsonType.String;
    }

    private DsonType ParseLocalId(string unquotedString) {
        switch (Settings.localIdType) {
            case DsonType.Int32: {
                PushNextUnionValue(new UnionValue(DsonType.Int32)
                {
                    iValue = DsonTexts.ParseInt32(unquotedString)
                });
                return DsonType.Int32;
            }
            case DsonType.Int64: {
                PushNextUnionValue(new UnionValue(DsonType.Int64)
                {
                    lValue = DsonTexts.ParseInt64(unquotedString)
                });
                return DsonType.Int64;
            }
            default: {
                PushNextValue(unquotedString);
                return DsonType.String;
            }
        }
    }

    /** 处理内置结构体的单值语法糖 */
    private DsonType ParseAbbreviatedStruct(Context context, in DsonToken valueToken) {
        // 1.className不能出现在topLevel，topLevel只能出现header结构体 @{}
        if (context.contextType == DsonContextType.TopLevel) {
            throw DsonIOException.InvalidTokenType(context.contextType, valueToken);
        }
        // 2.object和array的className会在beginObject和beginArray的时候转换为结构体 @{}
        // 因此这里只能出现内置结构体的简写形式
        string clsName = valueToken.StringValue();
        if (DsonTexts.LabelPtr == clsName) { // @ref localId
            DsonToken nextToken = PopToken();
            EnsureStringsToken(context, nextToken);
            PushNextValue(new ObjectPtr(nextToken.StringValue()));
            return DsonType.Pointer;
        }
        if (DsonTexts.LabelLitePtr == clsName) { // @ptr localId
            DsonToken nextToken = PopToken();
            EnsureStringsToken(context, nextToken);
            long localId = DsonTexts.ParseInt64(nextToken.StringValue());
            PushNextValue(new ObjectLitePtr(localId));
            return DsonType.LitePointer;
        }
        if (DsonTexts.LabelDateTime == clsName) { // @dt uuuu-MM-dd'T'HH:mm:ss
            DateTime dateTime = ExtDateTime.ParseDateTime(ScanStringUtilComma());
            PushNextUnionValue(new UnionValue(DsonType.DateTime)
            {
                dateTime = ExtDateTime.OfDateTime(in dateTime)
            });
            return DsonType.DateTime;
        }
        if (DsonTexts.LabelTimestamp == clsName) { // @ts seconds
            DsonToken nextToken = PopToken();
            EnsureStringsToken(context, nextToken);
            PushNextUnionValue(new UnionValue(DsonType.Timestamp)
            {
                timestamp = Timestamp.Parse(nextToken.StringValue())
            });
            return DsonType.Timestamp;
        }
        throw DsonIOException.InvalidTokenType(context.contextType, valueToken);
    }

    private DsonToken? PopHeaderToken(Context context) {
        DsonToken headerToken = PopToken();
        if (IsHeaderOrBuiltStruct(headerToken)) {
            return headerToken;
        }
        PushToken(headerToken);
        return null;
    }

    /** 处理内置结构体 */
    private DsonType ParseBeginObjectToken(Context context, in DsonToken valueToken) {
        DsonToken? headerTokenWrapper = PopHeaderToken(context);
        if (!headerTokenWrapper.HasValue) {
            PushNextValue(valueToken);
            return DsonType.Object;
        }
        DsonToken headerToken = headerTokenWrapper.Value;
        if (headerToken.type != DsonTokenType.BuiltinStruct) {
            // 转换SimpleHeader为标准Header，token需要push以供context保存
            EscapeHeaderAndPush(headerToken);
            PushNextValue(valueToken);
            return DsonType.Object;
        }
        // 内置结构体
        string clsName = headerToken.StringValue();
        switch (clsName) {
            case DsonTexts.LabelPtr: {
                PushNextValue(ScanPtr(context));
                return DsonType.Pointer;
            }
            case DsonTexts.LabelLitePtr: {
                PushNextValue(ScanLitePtr(context));
                return DsonType.LitePointer;
            }
            case DsonTexts.LabelDateTime: {
                PushNextUnionValue(new UnionValue(DsonType.DateTime)
                {
                    dateTime = ScanDateTime(context)
                });
                return DsonType.DateTime;
            }
            case DsonTexts.LabelTimestamp: {
                PushNextUnionValue(new UnionValue(DsonType.Timestamp)
                {
                    timestamp = ScanTimestamp(context)
                });
                return DsonType.Timestamp;
            }
            default: {
                PushToken(headerToken); // 非Object形式内置结构体
                return DsonType.Object;
            }
        }
    }

    /** 处理内置元组 */
    private DsonType ParseBeginArrayToken(Context context, in DsonToken valueToken) {
        DsonToken? headerTokenWrapper = PopHeaderToken(context);
        if (!headerTokenWrapper.HasValue) {
            PushNextValue(valueToken);
            return DsonType.Array;
        }
        DsonToken headerToken = headerTokenWrapper.Value;
        if (headerToken.type != DsonTokenType.BuiltinStruct) {
            // 转换SimpleHeader为标准Header，token需要push以供context保存
            EscapeHeaderAndPush(headerToken);
            PushNextValue(valueToken);
            return DsonType.Array;
        }
        // 内置元组 -- 已尽皆删除
        PushToken(headerToken);
        return DsonType.Array;
    }

    private void EscapeHeaderAndPush(DsonToken headerToken) {
        // 如果header不是结构体，则封装为结构体，注意...要反序压栈
        if (headerToken.type == DsonTokenType.BeginHeader) {
            PushToken(headerToken);
        } else {
            PushToken(TOKEN_END_OBJECT);
            PushToken(new DsonToken(DsonTokenType.String, headerToken.StringValue(), -1));
            PushToken(TOKEN_COLON);
            PushToken(TOKEN_CLASSNAME);
            PushToken(TOKEN_BEGIN_HEADER);
        }
    }

    #region 内置结构体语法

    private ObjectPtr ScanPtr(Context context) {
        string ns = null;
        string localId = null;
        byte type = 0;
        byte policy = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).type != DsonTokenType.EndObject) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);
            // 根据name校验
            DsonToken valueToken = PopToken();
            switch (keyToken.StringValue()) {
                case ObjectPtr.NamesNamespace: {
                    EnsureStringsToken(context, valueToken);
                    ns = valueToken.StringValue();
                    break;
                }
                case ObjectPtr.NamesLocalId: {
                    EnsureStringsToken(context, valueToken);
                    localId = valueToken.StringValue();
                    break;
                }
                case ObjectPtr.NamesType: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    type = byte.Parse(valueToken.StringValue());
                    break;
                }
                case ObjectPtr.NamesPolicy: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    policy = byte.Parse(valueToken.StringValue());
                    break;
                }
                default: {
                    throw new DsonIOException("invalid ptr fieldName: " + keyToken.StringValue());
                }
            }
            CheckSeparator(context);
        }
        return new ObjectPtr(localId, ns, type, policy);
    }

    private ObjectLitePtr ScanLitePtr(Context context) {
        string ns = null;
        long localId = 0;
        byte type = 0;
        byte policy = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).type != DsonTokenType.EndObject) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);
            // 根据name校验
            DsonToken valueToken = PopToken();
            switch (keyToken.StringValue()) {
                case ObjectPtr.NamesNamespace: {
                    EnsureStringsToken(context, valueToken);
                    ns = valueToken.StringValue();
                    break;
                }
                case ObjectPtr.NamesLocalId: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    localId = DsonTexts.ParseInt64(valueToken.StringValue());
                    break;
                }
                case ObjectPtr.NamesType: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    type = byte.Parse(valueToken.StringValue());
                    break;
                }
                case ObjectPtr.NamesPolicy: {
                    VerifyTokenType(context, valueToken, DsonTokenType.UnquoteString);
                    policy = byte.Parse(valueToken.StringValue());
                    break;
                }
                default: {
                    throw new DsonIOException("invalid lptr fieldName: " + keyToken.StringValue());
                }
            }
            CheckSeparator(context);
        }
        return new ObjectLitePtr(localId, ns, type, policy);
    }

    private Timestamp ScanTimestamp(Context context) {
        long seconds = 0;
        int nanos = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).type != DsonTokenType.EndObject) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);
            // 根据name校验
            switch (keyToken.StringValue()) {
                case Timestamp.NamesSeconds: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    seconds = DsonTexts.ParseInt64(valueToken.StringValue());
                    break;
                }
                case Timestamp.NamesNanos: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    nanos = DsonTexts.ParseInt32(valueToken.StringValue());
                    break;
                }
                case Timestamp.NamesMillis: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    int millis = DsonTexts.ParseInt32(valueToken.StringValue());
                    Timestamp.ValidateMillis(millis);
                    nanos = millis * (int)DatetimeUtil.NanosPerMilli;
                    break;
                }
                default: {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.StringValue());
                }
            }
            CheckSeparator(context);
        }
        return new Timestamp(seconds, nanos);
    }

    private ExtDateTime ScanDateTime(Context context) {
        DateTime date = DateTime.UnixEpoch;
        int time = 0;

        int nanos = 0;
        int offset = 0;
        byte enables = 0;
        DsonToken keyToken;
        while ((keyToken = PopToken()).type != DsonTokenType.EndObject) {
            // key必须是字符串
            EnsureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = PopToken();
            VerifyTokenType(context, colonToken, DsonTokenType.Colon);
            // 根据name校验
            switch (keyToken.StringValue()) {
                case ExtDateTime.NamesDate: {
                    string dateString = ScanStringUtilComma();
                    date = ExtDateTime.ParseDate(dateString);
                    enables |= ExtDateTime.MaskDate;
                    break;
                }
                case ExtDateTime.NamesTime: {
                    string timeString = ScanStringUtilComma();
                    time = ExtDateTime.ParseTime(timeString);
                    enables |= ExtDateTime.MaskTime;
                    break;
                }
                case ExtDateTime.NamesOffset: {
                    string offsetString = ScanStringUtilComma();
                    offset = ExtDateTime.ParseOffset(offsetString);
                    enables |= ExtDateTime.MaskOffset;
                    break;
                }
                case ExtDateTime.NamesNanos: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    nanos = DsonTexts.ParseInt32(valueToken.StringValue());
                    break;
                }
                case ExtDateTime.NamesMillis: {
                    DsonToken valueToken = PopToken();
                    EnsureStringsToken(context, valueToken);
                    int millis = DsonTexts.ParseInt32(valueToken.StringValue());
                    Timestamp.ValidateMillis(millis);
                    nanos = millis * (int)DatetimeUtil.NanosPerMilli;
                    break;
                }
                default: {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.StringValue());
                }
            }
            CheckSeparator(context);
        }
        long seconds = DatetimeUtil.ToEpochSeconds(date) + time;
        return new ExtDateTime(seconds, nanos, offset, enables);
    }

    /** 扫描string，直到遇见逗号或结束符 */
    private string ScanStringUtilComma() {
        StringBuilder sb = new StringBuilder(12);
        while (true) {
            DsonToken valueToken = PopToken();
            switch (valueToken.type) {
                case DsonTokenType.Comma:
                case DsonTokenType.EndObject:
                case DsonTokenType.EndArray: {
                    PushToken(valueToken);
                    return sb.ToString();
                }
                case DsonTokenType.String:
                case DsonTokenType.UnquoteString:
                case DsonTokenType.Colon: {
                    sb.Append(valueToken.StringValue());
                    break;
                }
                default: {
                    throw DsonIOException.InvalidTokenType(ContextType, valueToken);
                }
            }
        }
    }

    private void CheckSeparator(Context context) {
        // 每读取一个值，判断下分隔符，尾部最多只允许一个逗号 -- 这里在尾部更容易处理
        DsonToken keyToken;
        if ((keyToken = PopToken()).type == DsonTokenType.Comma
            && (keyToken = PopToken()).type == DsonTokenType.Comma) {
            throw DsonIOException.InvalidTokenType(context.contextType, keyToken);
        } else {
            PushToken(keyToken);
        }
    }

    #endregion

    /** header不可以在中途出现 */
    private static void EnsureCountIsZero(Context context, DsonToken headerToken) {
        if (context.count > 0) {
            throw DsonIOException.InvalidTokenType(context.contextType, headerToken,
                CollectionUtil.NewList(DsonTokenType.String, DsonTokenType.UnquoteString, DsonTokenType.EndObject));
        }
    }

    private static void EnsureStringsToken(Context context, DsonToken token) {
        if (token.type != DsonTokenType.String && token.type != DsonTokenType.UnquoteString) {
            throw DsonIOException.InvalidTokenType(context.contextType, token,
                CollectionUtil.NewList(DsonTokenType.String, DsonTokenType.UnquoteString));
        }
    }

    private static bool IsHeaderOrBuiltStruct(DsonToken token) {
        return token.type == DsonTokenType.BuiltinStruct
               || token.type == DsonTokenType.BeginHeader
               || token.type == DsonTokenType.SimpleHeader;
    }

    private static void VerifyTokenType(Context context, DsonToken token, DsonTokenType expected) {
        if (token.type != expected) {
            throw DsonIOException.InvalidTokenType(context.contextType, token, expected);
        }
    }

    private static void VerifyTokenType(Context context, DsonToken token, List<DsonTokenType> expected) {
        if (!expected.Contains(token.type)) {
            throw DsonIOException.InvalidTokenType(context.contextType, token, expected);
        }
    }

    protected override void DoReadName() {
        if (settings.enableFieldIntern) {
            currentName = Dsons.InternField(PopNextName());
        } else {
            currentName = PopNextName() ?? throw new NullReferenceException();
        }
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        UnionValue unionValue = PopNextUnionValue();
        return unionValue.type switch
        {
            DsonType.Int32 => unionValue.iValue,
            DsonType.Int64 => (int)unionValue.lValue,
            DsonType.Float => (int)unionValue.fValue,
            DsonType.Double => (int)unionValue.dValue,
            _ => throw new InvalidOperationException($"cant cast {unionValue.type} to int32")
        };
    }

    protected override long DoReadInt64() {
        UnionValue unionValue = PopNextUnionValue();
        return unionValue.type switch
        {
            DsonType.Int32 => unionValue.iValue,
            DsonType.Int64 => unionValue.lValue,
            DsonType.Float => (long)unionValue.fValue,
            DsonType.Double => (long)unionValue.dValue,
            _ => throw new InvalidOperationException($"cant cast {unionValue.type} to int64")
        };
    }

    protected override float DoReadFloat() {
        UnionValue unionValue = PopNextUnionValue();
        return unionValue.type switch
        {
            DsonType.Int32 => unionValue.iValue,
            DsonType.Int64 => unionValue.lValue,
            DsonType.Float => unionValue.fValue,
            DsonType.Double => (float)unionValue.dValue,
            _ => throw new InvalidOperationException($"cant cast {unionValue.type} to float")
        };
    }

    protected override double DoReadDouble() {
        UnionValue unionValue = PopNextUnionValue();
        return unionValue.type switch
        {
            DsonType.Int32 => unionValue.iValue,
            DsonType.Int64 => unionValue.lValue,
            DsonType.Float => unionValue.fValue,
            DsonType.Double => unionValue.dValue,
            _ => throw new InvalidOperationException($"cant cast {unionValue.type} to double")
        };
    }

    protected override bool DoReadBool() {
        UnionValue unionValue = PopNextUnionValue();
        if (unionValue.type != DsonType.Bool) {
            throw new InvalidOperationException();
        }
        return unionValue.bValue;
    }

    protected override string DoReadString() {
        string value = (string)PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return value;
    }

    protected override void DoReadNull() {
        PopNextValue();
    }

    protected override Binary DoReadBinary() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        byte[] bytes = (byte[])value;
        return Binary.UnsafeWrap(bytes);
    }

    protected override ObjectPtr DoReadPtr() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (ObjectPtr)value;
    }

    protected override ObjectLitePtr DoReadLitePtr() {
        object value = PopNextValue();
        if (value == null) throw new InvalidOperationException();
        return (ObjectLitePtr)value;
    }

    protected override ExtDateTime DoReadDateTime() {
        UnionValue value = PopNextUnionValue();
        if (value.type != DsonType.DateTime) {
            throw new InvalidOperationException();
        }
        return value.dateTime;
    }

    protected override Timestamp DoReadTimestamp() {
        UnionValue value = PopNextUnionValue();
        if (value.type != DsonType.Timestamp) {
            throw new InvalidOperationException();
        }
        return value.timestamp;
    }

    #endregion

    #region 容器

    protected override void DoReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        // newContext.beginToken = PopNextValue();
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void DoReadEndContainer() {
        Context context = GetContext();
        // 恢复上下文
        RecoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoSkipName() {
        // 名字早已读取
        PopNextName();
    }

    protected override void DoSkipValue() {
        PopNextValue();
        switch (currentDsonType) {
            case DsonType.Header:
            case DsonType.Object:
            case DsonType.Array: {
                SkipStack(1);
                break;
            }
        }
    }

    protected override void DoSkipToEndOfObject() {
        DsonToken endToken;
        if (IsAtType) {
            endToken = SkipStack(1);
        } else {
            SkipName();
            switch (currentDsonType) {
                case DsonType.Header:
                case DsonType.Object:
                case DsonType.Array: { // 嵌套对象
                    endToken = SkipStack(2);
                    break;
                }
                default: {
                    endToken = SkipStack(1);
                    break;
                }
            }
        }
        PushToken(endToken);
    }

    /** @return 触发结束的token */
    private DsonToken SkipStack(int stack) {
        while (stack > 0) {
            DsonToken token = _marking ? PopToken() : SkipToken();
            switch (token.type) {
                case DsonTokenType.BeginArray:
                case DsonTokenType.BeginObject:
                case DsonTokenType.BeginHeader: {
                    stack++;
                    break;
                }
                case DsonTokenType.EndArray:
                case DsonTokenType.EndObject: {
                    if (--stack == 0) {
                        return token;
                    }
                    break;
                }
                case DsonTokenType.Eof: {
                    throw DsonIOException.InvalidTokenType(ContextType, token);
                }
            }
        }
        throw new InvalidOperationException("Assert Exception");
    }

    protected override byte[] DoReadValueAsBytes() {
        // Text的Reader和Writer实现最好相同，要么都不支持，要么都支持
        throw new DsonIOException("UnsupportedOperation");
    }

    #endregion

    #region context

    private static readonly ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<Context>(
        () => new Context(), context => context.Reset(),
        DsonInternals.CONTEXT_POOL_SIZE);

    private static Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = contextPool.Acquire();
        context.Init(parent, contextType, dsonType);
        return context;
    }

    private static void ReturnContext(Context context) {
        contextPool.Release(context);
    }

#nullable disable
#pragma warning disable CS0628
    protected new class Context : AbstractDsonReader<string>.Context
    {
        /** header只可触发一次流程 */
        internal int headerCount;
        /** 元素计数，判断冒号 */
        internal int count;
        /** 数组/Object成员的类型 - token类型可直接复用；header的该属性是用于注释外层对象的 */
        internal DsonToken? compClsNameToken;

        public Context() {
        }

        public new Context Parent => (Context)parent;

        public override void Reset() {
            base.Reset();
            headerCount = 0;
            count = 0;
            compClsNameToken = null;
        }
    }

    #endregion
}
}