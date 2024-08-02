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

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.base.time.TimeUtils;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.types.*;

import java.io.Reader;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;
import java.util.ArrayDeque;
import java.util.HexFormat;
import java.util.List;
import java.util.Objects;

/**
 * 在二进制下，写入的顺序是： type-name-value
 * 但在文本格式下，写入的顺序是：name-type-value
 * 但我们要为用户提供一致的api，即对上层表现为二进制相同的读写顺序，因此我们需要将name缓存下来，直到用户调用readName。
 * 另外，我们只有先读取了value的token之后，才可以返回数据的类型{@link DsonType}，
 * 因此 name-type-value 通常是在一次readType中完成。
 * <p>
 * 另外，分隔符也需要压栈，以验证用户输入的正确性。
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public final class DsonTextReader extends AbstractDsonReader {

    private static final List<DsonTokenType> VALUE_SEPARATOR_TOKENS = List.of(DsonTokenType.COMMA, DsonTokenType.END_OBJECT, DsonTokenType.END_ARRAY);

    private static final DsonToken TOKEN_BEGIN_HEADER = new DsonToken(DsonTokenType.BEGIN_HEADER, "@{", -1);
    private static final DsonToken TOKEN_CLASSNAME = new DsonToken(DsonTokenType.UNQUOTE_STRING, DsonHeader.NAMES_CLASS_NAME, -1);
    private static final DsonToken TOKEN_COLON = new DsonToken(DsonTokenType.COLON, ":", -1);
    private static final DsonToken TOKEN_END_OBJECT = new DsonToken(DsonTokenType.END_OBJECT, "}", -1);

    private DsonScanner scanner;
    private String nextName;
    /** 未声明为DsonValue，避免再拆装箱 */
    private Object nextValue;

    private boolean marking;
    private final ArrayDeque<DsonToken> pushedTokenQueue = new ArrayDeque<>(6);
    private final ArrayDeque<DsonToken> markedTokenQueue = new ArrayDeque<>(6);

    public DsonTextReader(DsonTextReaderSettings settings, CharSequence dsonString) {
        this(settings, new DsonScanner(dsonString));
    }

    public DsonTextReader(DsonTextReaderSettings settings, Reader reader) {
        this(settings, new DsonScanner(DsonCharStream.newBufferedCharStream(reader, settings.autoClose)));
    }

    public DsonTextReader(DsonTextReaderSettings settings, Reader reader, boolean autoClose) {
        this(settings, new DsonScanner(DsonCharStream.newBufferedCharStream(reader, autoClose)));
    }

    public DsonTextReader(DsonTextReaderSettings settings, DsonScanner scanner) {
        super(settings);
        this.scanner = Objects.requireNonNull(scanner);

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        setContext(context);
    }

    /**
     * 用于动态指定成员数据类型
     * 1.这对于精确解析数组元素和Object的字段十分有用 -- 比如解析一个{@code Vector3}的时候就可以指定字段的默认类型为float。
     * 2.辅助方法见：{@link DsonTexts#clsNameTokenOfType(DsonType)}
     */
    public void setCompClsNameToken(DsonToken dsonToken) {
        getContext().compClsNameToken = dsonToken;
    }

    @Override
    public DsonTextReaderSettings getSettings() {
        return (DsonTextReaderSettings) settings;
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public void close() {
        Context context = getContext();
        setContext(null);
        while (context != null) {
            Context parent = context.getParent();
            contextPool.release(context);
            context = parent;
        }
        if (scanner != null) {
            scanner.close();
            scanner = null;
        }
        pushedTokenQueue.clear();
        nextName = null;
        nextValue = null;
        marking = false;
        markedTokenQueue.clear();
        super.close();
    }

    // region token

    private DsonToken popToken() {
        if (pushedTokenQueue.isEmpty()) {
            DsonToken dsonToken = scanner.nextToken(false);
            if (marking) {
                markedTokenQueue.addLast(dsonToken);
            }
            return dsonToken;
        } else {
            return pushedTokenQueue.pop();
        }
    }

    private DsonToken skipToken() {
        if (pushedTokenQueue.isEmpty()) {
            return scanner.nextToken(true);
        }
        return pushedTokenQueue.pop();
    }

    private void pushToken(DsonToken token) {
        Objects.requireNonNull(token);
        pushedTokenQueue.push(token);
    }

    private void pushNextValue(Object nextValue) {
        this.nextValue = Objects.requireNonNull(nextValue);
    }

    private Object popNextValue() {
        Object r = this.nextValue;
        this.nextValue = null;
        return r;
    }

    private void pushNextName(String nextName) {
        this.nextName = Objects.requireNonNull(nextName);
    }

    private String popNextName() {
        String r = this.nextName;
        this.nextName = null;
        return r;
    }
    // endregion

    // region state

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        DsonType dsonType = readDsonTypeOfToken();
        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VARINT;
        this.currentName = INVALID_NAME;

        onReadDsonType(context, dsonType);
        if (dsonType == DsonType.HEADER) {
            context.headerCount++;
        } else {
            context.count++;
        }
        return dsonType;
    }

    @Override
    public DsonType peekDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        ArrayDeque<DsonToken> pushedTokenQueue = this.pushedTokenQueue;
        ArrayDeque<DsonToken> markedTokenQueue = this.markedTokenQueue;

        marking = true;
        markedTokenQueue.addAll(pushedTokenQueue); // 保存既有token

        DsonType dsonType = readDsonTypeOfToken();
        popNextName(); // 丢弃临时数据
        popNextValue();

        pushedTokenQueue.clear();
        pushedTokenQueue.addAll(markedTokenQueue);
        markedTokenQueue.clear();
        marking = false;

        return dsonType;
    }

    /**
     * 两个职责：
     * 1.校验token在上下文中的正确性 -- 上层会校验DsonType的合法性
     * 2.将合法的token转换为dson的键值对（或值）
     * <p>
     * 在读取valueToken时遇见 { 或 [ 时要判断是否是内置结构体，如果是内置结构体，要预读为值，而不是返回beginXXX；
     * 如果不是内置结构体，如果是 '@className' 形式声明的类型，要伪装成 {clsName: $className} 的token流，使得上层可按照相同的方式解析。
     * '@clsName' 本质是简化书写的语法糖。
     */
    private DsonType readDsonTypeOfToken() {
        // 丢弃旧值
        popNextName();
        popNextValue();

        Context context = getContext();
        // 统一处理逗号分隔符，顶层对象之间可不写分隔符
        if (context.count > 0) {
            DsonToken nextToken = popToken();
            if (context.contextType != DsonContextType.TOP_LEVEL) {
                verifyTokenType(context, nextToken, VALUE_SEPARATOR_TOKENS);
            }
            if (nextToken.type == DsonTokenType.COMMA) {
                // 禁止末尾逗号
                DsonToken nnToken = popToken();
                pushToken(nnToken);
                if (nnToken.type == DsonTokenType.END_OBJECT || nnToken.type == DsonTokenType.END_ARRAY) {
                    throw DsonIOException.invalidTokenType(context.contextType, nextToken);
                }
            } else {
                pushToken(nextToken);
            }
        }

        // object/header 需要先读取 name和冒号，但object可能出现header
        if (context.contextType == DsonContextType.OBJECT || context.contextType == DsonContextType.HEADER) {
            DsonToken nameToken = popToken();
            switch (nameToken.type) {
                case STRING, UNQUOTE_STRING -> {
                    pushNextName(nameToken.stringValue());
                }
                case BEGIN_HEADER -> {
                    if (context.contextType == DsonContextType.HEADER) {
                        throw DsonIOException.containsHeaderDirectly(nameToken);
                    }
                    ensureCountIsZero(context, nameToken);
                    pushNextValue(nameToken);
                    return DsonType.HEADER;
                }
                case END_OBJECT -> {
                    return DsonType.END_OF_OBJECT;
                }
                default -> {
                    throw DsonIOException.invalidTokenType(context.contextType, nameToken,
                            List.of(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING, DsonTokenType.END_OBJECT));
                }
            }
            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, DsonTokenType.COLON);
        }

        // 走到这里，表示 top/object/header/array 读值
        DsonToken valueToken = popToken();
        return switch (valueToken.type) {
            case INT32 -> {
                pushNextValue(valueToken.value);
                yield DsonType.INT32;
            }
            case INT64 -> {
                pushNextValue(valueToken.value);
                yield DsonType.INT64;
            }
            case FLOAT -> {
                pushNextValue(valueToken.value);
                yield DsonType.FLOAT;
            }
            case DOUBLE -> {
                pushNextValue(valueToken.value);
                yield DsonType.DOUBLE;
            }
            case BOOL -> {
                pushNextValue(valueToken.value);
                yield DsonType.BOOL;
            }
            case STRING -> {
                pushNextValue(valueToken.stringValue());
                yield DsonType.STRING;
            }
            case NULL -> {
                pushNextValue(DsonNull.NULL);
                yield DsonType.NULL;
            }
            case BINARY -> {
                pushNextValue(valueToken.value);
                yield DsonType.BINARY;
            }
            case BUILTIN_STRUCT -> parseAbbreviatedStruct(context, valueToken);
            case UNQUOTE_STRING -> parseUnquoteStringToken(context, valueToken);
            case BEGIN_OBJECT -> parseBeginObjectToken(context, valueToken);
            case BEGIN_ARRAY -> parseBeginArrayToken(context, valueToken);
            case BEGIN_HEADER -> {
                // object的header已经处理，这里只有topLevel和array可以再出现header
                if (context.contextType.isObjectLike()) {
                    throw DsonIOException.invalidTokenType(context.contextType, valueToken);
                }
                ensureCountIsZero(context, valueToken);
//                pushNextValue(valueToken);
                yield DsonType.HEADER;
            }
            case END_ARRAY -> {
                // endArray 只能在数组上下文出现；Array是在读取下一个值的时候结束；而Object必须在读取下一个name的时候结束
                if (context.contextType == DsonContextType.ARRAY) {
                    yield DsonType.END_OF_OBJECT;
                }
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
            case EOF -> {
                // eof 只能在顶层上下文出现
                if (context.contextType == DsonContextType.TOP_LEVEL) {
                    yield DsonType.END_OF_OBJECT;
                }
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
            default -> {
                throw DsonIOException.invalidTokenType(context.contextType, valueToken);
            }
        };
    }

    /** 字符串默认解析规则 */
    private DsonType parseUnquoteStringToken(Context context, DsonToken valueToken) {
        String unquotedString = valueToken.stringValue();
        // 处理header的特殊属性依赖
        if (context.contextType == DsonContextType.HEADER) {
            switch (nextName) {
                case DsonHeader.NAMES_CLASS_NAME -> {
                    pushNextValue(unquotedString);
                    return DsonType.STRING;
                }
                case DsonHeader.NAMES_LOCAL_ID -> {
                    return parseLocalId(unquotedString);
                }
            }
        }
        // 处理类型传递
        if (context.compClsNameToken != null) {
            switch (context.compClsNameToken.stringValue()) {
                case DsonTexts.LABEL_INT32 -> {
                    pushNextValue(DsonTexts.parseInt32(unquotedString));
                    return DsonType.INT32;
                }
                case DsonTexts.LABEL_INT64 -> {
                    pushNextValue(DsonTexts.parseInt64(unquotedString));
                    return DsonType.INT64;
                }
                case DsonTexts.LABEL_FLOAT -> {
                    pushNextValue(DsonTexts.parseFloat(unquotedString));
                    return DsonType.FLOAT;
                }
                case DsonTexts.LABEL_DOUBLE -> {
                    pushNextValue(DsonTexts.parseDouble(unquotedString));
                    return DsonType.DOUBLE;
                }
                case DsonTexts.LABEL_BOOL -> {
                    pushNextValue(DsonTexts.parseBool(unquotedString));
                    return DsonType.BOOL;
                }
                case DsonTexts.LABEL_STRING -> {
                    pushNextValue(unquotedString);
                    return DsonType.STRING;
                }
                case DsonTexts.LABEL_BINARY -> {
                    byte[] bytes = HexFormat.of().parseHex(unquotedString);
                    pushNextValue(bytes); // 直接压入bytes
                    return DsonType.BINARY;
                }
            }
        }

        // 处理特殊值解析
        boolean isTrueString = "true".equals(unquotedString);
        if (isTrueString || "false".equals(unquotedString)) {
            pushNextValue(isTrueString);
            return DsonType.BOOL;
        }
        if ("null".equals(unquotedString)) {
            pushNextValue(DsonNull.NULL);
            return DsonType.NULL;
        }
        if (DsonTexts.isParsable(unquotedString)) {
            pushNextValue(DsonTexts.parseDouble(unquotedString));
            return DsonType.DOUBLE;
        }
        pushNextValue(unquotedString);
        return DsonType.STRING;
    }

    private DsonType parseLocalId(String unquotedString) {
        switch (getSettings().localIdType) {
            case INT32 -> {
                pushNextValue(DsonTexts.parseInt32(unquotedString));
                return DsonType.INT32;
            }
            case INT64 -> {
                pushNextValue(DsonTexts.parseInt64(unquotedString));
                return DsonType.INT64;
            }
            default -> {
                pushNextValue(unquotedString);
                return DsonType.STRING;
            }
        }
    }

    /** 处理内置结构体的单值语法糖 */
    private DsonType parseAbbreviatedStruct(Context context, final DsonToken valueToken) {
        // 1.className不能出现在topLevel，topLevel只能出现header结构体 @{}
        if (context.contextType == DsonContextType.TOP_LEVEL) {
            throw DsonIOException.invalidTokenType(context.contextType, valueToken);
        }
        // 2.object和array的className会在beginObject和beginArray的时候转换为结构体 @{}
        // 因此这里只能出现内置结构体的简写形式
        String clsName = valueToken.stringValue();
        if (DsonTexts.LABEL_PTR.equals(clsName)) {// @ptr localId
            DsonToken nextToken = popToken();
            ensureStringsToken(context, nextToken);
            pushNextValue(new ObjectPtr(nextToken.stringValue()));
            return DsonType.POINTER;
        }
        if (DsonTexts.LABEL_LITE_PTR.equals(clsName)) { // @lptr localId
            DsonToken nextToken = popToken();
            ensureStringsToken(context, nextToken);
            long localId = DsonTexts.parseInt64(nextToken.stringValue());
            pushNextValue(new ObjectLitePtr(localId));
            return DsonType.LITE_POINTER;
        }

        if (DsonTexts.LABEL_DATETIME.equals(clsName)) { // @dt uuuu-MM-dd'T'HH:mm:ss
            LocalDateTime dateTime = ExtDateTime.parseDateTime(scanStringUtilComma());
            pushNextValue(ExtDateTime.ofDateTime(dateTime));
            return DsonType.DATETIME;
        }
        if (DsonTexts.LABEL_TIMESTAMP.equals(clsName)) { // @ts seconds
            DsonToken nextToken = popToken();
            ensureStringsToken(context, nextToken);
            Timestamp timestamp = Timestamp.parse(nextToken.stringValue());
            pushNextValue(timestamp);
            return DsonType.TIMESTAMP;
        }

        throw DsonIOException.invalidTokenType(context.contextType, valueToken);
    }

    private DsonToken popHeaderToken(Context context) {
        DsonToken headerToken = popToken();
        if (isHeaderOrBuiltStructToken(headerToken)) {
            return headerToken;
        }
        pushToken(headerToken);
        return null;
    }

    /** 处理内置结构体 */
    private DsonType parseBeginObjectToken(Context context, final DsonToken valueToken) {
        DsonToken headerToken = popHeaderToken(context);
        if (headerToken == null) {
            pushNextValue(valueToken);
            return DsonType.OBJECT;
        }
        if (headerToken.type != DsonTokenType.BUILTIN_STRUCT) {
            // 转换SimpleHeader为标准Header，token需要push以供context保存
            escapeHeaderAndPush(headerToken);
            pushNextValue(valueToken);
            return DsonType.OBJECT;
        }
        // 内置结构体
        String clsName = headerToken.stringValue();
        return switch (clsName) {
            case DsonTexts.LABEL_PTR -> {
                pushNextValue(scanPtr(context));
                yield DsonType.POINTER;
            }
            case DsonTexts.LABEL_LITE_PTR -> {
                pushNextValue(scanLitePtr(context));
                yield DsonType.LITE_POINTER;
            }
            case DsonTexts.LABEL_DATETIME -> {
                pushNextValue(scanDateTime(context));
                yield DsonType.DATETIME;
            }
            case DsonTexts.LABEL_TIMESTAMP -> {
                pushNextValue(scanTimestamp(context));
                yield DsonType.TIMESTAMP;
            }
            default -> {
                pushToken(headerToken); // 非Object形式内置结构体
//                pushNextValue(valueToken);
                yield DsonType.OBJECT;
            }
        };
    }

    /** 处理内置元组 */
    private DsonType parseBeginArrayToken(Context context, final DsonToken valueToken) {
        DsonToken headerToken = popHeaderToken(context);
        if (headerToken == null) {
            pushNextValue(valueToken);
            return DsonType.ARRAY;
        }
        if (headerToken.type != DsonTokenType.BUILTIN_STRUCT) {
            // 转换SimpleHeader为标准Header，token需要push以供context保存
            escapeHeaderAndPush(headerToken);
            pushNextValue(valueToken);
            return DsonType.ARRAY;
        }
        // 内置元组 -- 已尽皆删除...
        pushToken(headerToken);
//        pushNextValue(valueToken);
        return DsonType.ARRAY;
    }

    private void escapeHeaderAndPush(DsonToken headerToken) {
        // 如果header不是结构体，则封装为结构体，注意...要反序压栈
        if (headerToken.type == DsonTokenType.BEGIN_HEADER) {
            pushToken(headerToken);
        } else {
            pushToken(TOKEN_END_OBJECT);
            pushToken(new DsonToken(DsonTokenType.STRING, headerToken.stringValue(), -1));
            pushToken(TOKEN_COLON);
            pushToken(TOKEN_CLASSNAME);
            pushToken(TOKEN_BEGIN_HEADER);
        }
    }

    // region 内置结构体语法

    private ObjectPtr scanPtr(Context context) {
        String namespace = null;
        String localId = null;
        byte type = 0;
        byte policy = 0;
        DsonToken keyToken;
        while ((keyToken = popToken()).type != DsonTokenType.END_OBJECT) {
            // key必须是字符串
            ensureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, DsonTokenType.COLON);
            // 根据name校验
            DsonToken valueToken = popToken();
            switch (keyToken.stringValue()) {
                case ObjectPtr.NAMES_NAMESPACE -> {
                    ensureStringsToken(context, valueToken);
                    namespace = valueToken.stringValue();
                }
                case ObjectPtr.NAMES_LOCAL_ID -> {
                    ensureStringsToken(context, valueToken);
                    localId = valueToken.stringValue();
                }
                case ObjectPtr.NAMES_TYPE -> {
                    verifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    type = Byte.parseByte(valueToken.stringValue());
                }
                case ObjectPtr.NAMES_POLICY -> {
                    verifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    policy = Byte.parseByte(valueToken.stringValue());
                }
                default -> {
                    throw new DsonIOException("invalid ptr fieldName: " + keyToken.stringValue());
                }
            }
            checkSeparator(context);
        }
        return new ObjectPtr(localId, namespace, type, policy);
    }

    private ObjectLitePtr scanLitePtr(Context context) {
        String namespace = null;
        long localId = 0;
        byte type = 0;
        byte policy = 0;
        DsonToken keyToken;
        while ((keyToken = popToken()).type != DsonTokenType.END_OBJECT) {
            // key必须是字符串
            ensureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, DsonTokenType.COLON);
            // 根据name校验
            DsonToken valueToken = popToken();
            switch (keyToken.stringValue()) {
                case ObjectPtr.NAMES_NAMESPACE -> {
                    ensureStringsToken(context, valueToken);
                    namespace = valueToken.stringValue();
                }
                case ObjectPtr.NAMES_LOCAL_ID -> {
                    verifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    localId = DsonTexts.parseInt64(valueToken.stringValue());
                }
                case ObjectPtr.NAMES_TYPE -> {
                    verifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    type = Byte.parseByte(valueToken.stringValue());
                }
                case ObjectPtr.NAMES_POLICY -> {
                    verifyTokenType(context, valueToken, DsonTokenType.UNQUOTE_STRING);
                    policy = Byte.parseByte(valueToken.stringValue());
                }
                default -> {
                    throw new DsonIOException("invalid lptr fieldName: " + keyToken.stringValue());
                }
            }
            checkSeparator(context);
        }
        return new ObjectLitePtr(localId, namespace, type, policy);
    }

    private Timestamp scanTimestamp(Context context) {
        long seconds = 0;
        int nanos = 0;
        DsonToken keyToken;
        while ((keyToken = popToken()).type != DsonTokenType.END_OBJECT) {
            // key必须是字符串
            ensureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, DsonTokenType.COLON);
            // 根据name校验
            switch (keyToken.stringValue()) {
                case Timestamp.NAMES_SECONDS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    seconds = DsonTexts.parseInt64(valueToken.stringValue());
                }
                case Timestamp.NAMES_NANOS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    nanos = DsonTexts.parseInt32(valueToken.stringValue());
                }
                case Timestamp.NAMES_MILLIS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    int millis = DsonTexts.parseInt32(valueToken.stringValue());
                    Timestamp.validateMillis(millis);
                    nanos = millis * (int) TimeUtils.NANOS_PER_MILLI;
                }
                default -> {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.stringValue());
                }
            }
            checkSeparator(context);
        }
        return new Timestamp(seconds, nanos);
    }

    private ExtDateTime scanDateTime(Context context) {
        LocalDate date = LocalDate.EPOCH;
        LocalTime time = LocalTime.MIN;
        int nanos = 0;
        int offset = 0;
        byte enables = 0;
        DsonToken keyToken;
        while ((keyToken = popToken()).type != DsonTokenType.END_OBJECT) {
            // key必须是字符串
            ensureStringsToken(context, keyToken);
            // 下一个应该是冒号
            DsonToken colonToken = popToken();
            verifyTokenType(context, colonToken, DsonTokenType.COLON);
            // 根据name校验
            switch (keyToken.stringValue()) {
                case ExtDateTime.NAMES_DATE -> {
                    String dateString = scanStringUtilComma();
                    date = ExtDateTime.parseDate(dateString);
                    enables |= ExtDateTime.MASK_DATE;
                }
                case ExtDateTime.NAMES_TIME -> {
                    String timeString = scanStringUtilComma();
                    time = ExtDateTime.parseTime(timeString);
                    enables |= ExtDateTime.MASK_TIME;
                }
                case ExtDateTime.NAMES_OFFSET -> {
                    String offsetString = scanStringUtilComma();
                    offset = ExtDateTime.parseOffset(offsetString);
                    enables |= ExtDateTime.MASK_OFFSET;
                }
                case ExtDateTime.NAMES_NANOS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    nanos = DsonTexts.parseInt32(valueToken.stringValue());
                }
                case ExtDateTime.NAMES_MILLIS -> {
                    DsonToken valueToken = popToken();
                    ensureStringsToken(context, valueToken);
                    int millis = DsonTexts.parseInt32(valueToken.stringValue());
                    Timestamp.validateMillis(millis);
                    nanos = millis * (int) TimeUtils.NANOS_PER_MILLI;
                }
                default -> {
                    throw new DsonIOException("invalid datetime fieldName: " + keyToken.stringValue());
                }
            }
            checkSeparator(context);
        }
        long seconds = LocalDateTime.of(date, time).toEpochSecond(ZoneOffset.UTC);
        return new ExtDateTime(seconds, nanos, offset, enables);
    }

    /** 扫描string，直到遇见逗号或结束符 */
    private String scanStringUtilComma() {
        StringBuilder sb = new StringBuilder(12);
        while (true) {
            DsonToken valueToken = popToken();
            switch (valueToken.type) {
                case COMMA, END_OBJECT, END_ARRAY -> {
                    pushToken(valueToken);
                    return sb.toString();
                }
                case STRING, UNQUOTE_STRING, COLON -> {
                    sb.append(valueToken.stringValue());
                }
                default -> {
                    throw DsonIOException.invalidTokenType(getContextType(), valueToken);
                }
            }
        }
    }

    private void checkSeparator(Context context) {
        // 每读取一个值，判断下分隔符，尾部最多只允许一个逗号 -- 这里在尾部更容易处理
        DsonToken keyToken;
        if ((keyToken = popToken()).type == DsonTokenType.COMMA
                && (keyToken = popToken()).type == DsonTokenType.COMMA) {
            throw DsonIOException.invalidTokenType(context.contextType, keyToken);
        } else {
            pushToken(keyToken);
        }
    }
    // endregion

    /** header不可以在中途出现 */
    private static void ensureCountIsZero(Context context, DsonToken headerToken) {
        if (context.count > 0) {
            throw DsonIOException.invalidTokenType(context.contextType, headerToken,
                    List.of(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING, DsonTokenType.END_OBJECT));
        }
    }

    private static void ensureStringsToken(Context context, DsonToken token) {
        if (token.type != DsonTokenType.STRING && token.type != DsonTokenType.UNQUOTE_STRING) {
            throw DsonIOException.invalidTokenType(context.contextType, token, List.of(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING));
        }
    }

    private static boolean isHeaderOrBuiltStructToken(DsonToken token) {
        return token.type == DsonTokenType.BUILTIN_STRUCT
                || token.type == DsonTokenType.SIMPLE_HEADER
                || token.type == DsonTokenType.BEGIN_HEADER;
    }

    private static void verifyTokenType(Context context, DsonToken token, DsonTokenType expected) {
        if (token.type != expected) {
            throw DsonIOException.invalidTokenType(context.contextType, token, List.of(expected));
        }
    }

    private static void verifyTokenType(Context context, DsonToken token, List<DsonTokenType> expected) {
        if (!CollectionUtils.containsRef(expected, token.type)) {
            throw DsonIOException.invalidTokenType(context.contextType, token, expected);
        }
    }

    @Override
    protected void doReadName() {
        if (settings.enableFieldIntern) {
            currentName = Dsons.internField(popNextName());
        } else {
            currentName = Objects.requireNonNull(popNextName());
        }
    }

    // endregion

    // region 简单值

    @Override
    public Number readNumber(String name) {
        // 重写以减少拆装箱
        advanceToValueState(name, null);
        return switch (currentDsonType) {
            case INT32, INT64, FLOAT, DOUBLE -> {
                Number number = (Number) popNextValue();
                Objects.requireNonNull(number);
                setNextState();
                yield number;
            }
            default -> throw DsonIOException.dsonTypeMismatch(DsonType.DOUBLE, currentDsonType);
        };
    }

    @Override
    protected int doReadInt32() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.intValue();
    }

    @Override
    protected long doReadInt64() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.longValue();
    }

    @Override
    protected float doReadFloat() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.floatValue();
    }

    @Override
    protected double doReadDouble() {
        Number number = (Number) popNextValue();
        Objects.requireNonNull(number);
        return number.doubleValue();
    }

    @Override
    protected boolean doReadBool() {
        Boolean value = (Boolean) popNextValue();
        Objects.requireNonNull(value);
        return value;
    }

    @Override
    protected String doReadString() {
        String value = (String) popNextValue();
        Objects.requireNonNull(value);
        return value;
    }

    @Override
    protected void doReadNull() {
        Object value = popNextValue();
        assert value == DsonNull.NULL;
    }

    @Override
    protected Binary doReadBinary() {
        byte[] bytes = (byte[]) Objects.requireNonNull(popNextValue());
        return Binary.unsafeWrap(bytes);
    }

    @Override
    protected ObjectPtr doReadPtr() {
        return (ObjectPtr) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected ObjectLitePtr doReadLitePtr() {
        return (ObjectLitePtr) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected ExtDateTime doReadDateTime() {
        return (ExtDateTime) Objects.requireNonNull(popNextValue());
    }

    @Override
    protected Timestamp doReadTimestamp() {
        return (Timestamp) Objects.requireNonNull(popNextValue());
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = newContext(getContext(), contextType, dsonType);
//        newContext.beginToken = popNextValue();
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    @Override
    protected void doReadEndContainer() {
        Context context = getContext();
        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        setContext(context.parent);
        returnContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doSkipName() {
        // 名字早已读取
        popNextName();
    }

    @Override
    protected void doSkipValue() {
        popNextValue();
        switch (currentDsonType) {
            case HEADER, OBJECT, ARRAY -> skipStack(1);
        }
    }

    @Override
    protected void doSkipToEndOfObject() {
        DsonToken endToken;
        if (isAtType()) {
            endToken = skipStack(1);
        } else {
            skipName();
            endToken = switch (currentDsonType) { // 嵌套对象
                case HEADER, OBJECT, ARRAY -> skipStack(2);
                default -> skipStack(1);
            };
        }
        pushToken(endToken);
    }

    /** @return 触发结束的token */
    private DsonToken skipStack(int stack) {
        while (stack > 0) {
            DsonToken token = marking ? popToken() : skipToken();
            switch (token.type) {
                case BEGIN_ARRAY, BEGIN_OBJECT, BEGIN_HEADER -> stack++;
                case END_ARRAY, END_OBJECT -> {
                    if (--stack == 0) {
                        return token;
                    }
                }
                case EOF -> {
                    throw DsonIOException.invalidTokenType(getContextType(), token);
                }
            }
        }
        throw new AssertionError();
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        // Text的Reader和Writer实现最好相同，要么都不支持，要么都支持
        throw new UnsupportedOperationException();
    }

    // endregion

    // region context

    private static final ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<>(Context::new, Context::reset,
            DsonInternals.CONTEXT_POOL_SIZE);

    private static Context newContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = contextPool.acquire();
        context.init(parent, contextType, dsonType);
        return context;
    }

    private static void returnContext(Context context) {
        contextPool.release(context);
    }

    protected static class Context extends AbstractDsonReader.Context {

        /** header只可触发一次流程 */
        int headerCount = 0;
        /** 元素计数，判断冒号 */
        int count;
        /** 数组/Object成员的类型 - token类型可直接复用；header的该属性是用于注释外层对象的 */
        DsonToken compClsNameToken;

        public Context() {
        }

        public void reset() {
            super.reset();
            headerCount = 0;
            count = 0;
            compClsNameToken = null;
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

    }

    // endregion

}