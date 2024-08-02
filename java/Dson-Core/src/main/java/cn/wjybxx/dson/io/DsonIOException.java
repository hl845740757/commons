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

package cn.wjybxx.dson.io;

import cn.wjybxx.dson.DsonContextType;
import cn.wjybxx.dson.DsonReaderState;
import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.DsonWriterState;
import cn.wjybxx.dson.text.DsonToken;
import cn.wjybxx.dson.text.DsonTokenType;

import java.util.List;

/**
 * @author wjybxx
 * date - 2023/4/22
 */
public class DsonIOException extends RuntimeException {

    public DsonIOException() {
    }

    public DsonIOException(String message) {
        super(message);
    }

    public DsonIOException(String message, Throwable cause) {
        super(message, cause);
    }

    public DsonIOException(Throwable cause) {
        super(cause);
    }

    public DsonIOException(String message, Throwable cause, boolean enableSuppression, boolean writableStackTrace) {
        super(message, cause, enableSuppression, writableStackTrace);
    }

    public static DsonIOException wrap(Exception e) {
        if (e instanceof DsonIOException) {
            return (DsonIOException) e;
        }
        return new DsonIOException(e);
    }

    public static DsonIOException wrap(Exception e, String message) {
        if (e instanceof DsonIOException) {
            return (DsonIOException) e;
        }
        return new DsonIOException(message, e);
    }

    // reader/writer
    public static DsonIOException recursionLimitExceeded() {
        return new DsonIOException("Object had too many levels of nesting.");
    }

    public static DsonIOException contextError(DsonContextType expected, DsonContextType contextType) {
        return new DsonIOException(String.format("context error, expected %s, but found %s", expected, contextType));
    }

    public static DsonIOException contextError(List<DsonContextType> expected, DsonContextType contextType) {
        return new DsonIOException(String.format("context error, expected %s, but found %s", expected, contextType));
    }

    public static DsonIOException contextErrorTopLevel() {
        return new DsonIOException("context error, current state is TopLevel");
    }

    public static DsonIOException unexpectedName(int expected, int name) {
        return new DsonIOException(String.format("The number of the field does not match, expected %d, but found %d", expected, name));
    }

    public static DsonIOException unexpectedName(String expected, String name) {
        return new DsonIOException(String.format("The name of the field does not match, expected %s, but found %s", expected, name));
    }

    public static DsonIOException dsonTypeMismatch(DsonType expected, DsonType dsonType) {
        return new DsonIOException(String.format("The dsonType does not match, expected %s, but found %s", expected, dsonType));
    }

    public static DsonIOException invalidDsonType(List<DsonType> dsonTypeList, DsonType dsonType) {
        return new DsonIOException(String.format("The dson type is invalid in context, context: %s, dsonType: %s", dsonTypeList, dsonType));
    }

    public static DsonIOException invalidDsonType(DsonContextType contextType, DsonType dsonType) {
        return new DsonIOException(String.format("The dson type is invalid in context, context: %s, dsonType: %s", contextType, dsonType));
    }

    public static DsonIOException unexpectedSubType(int expected, int subType) {
        return new DsonIOException(String.format("Unexpected subType, expected %d, but found %d", expected, subType));
    }

    public static DsonIOException invalidState(DsonContextType contextType, List<DsonReaderState> expected, DsonReaderState state) {
        return new DsonIOException(String.format("invalid state, contextType %s, expected %s, but found %s.",
                contextType, expected, state));
    }

    public static DsonIOException invalidState(DsonContextType contextType, List<DsonWriterState> expected, DsonWriterState state) {
        return new DsonIOException(String.format("invalid state, contextType %s, expected %s, but found %s.",
                contextType, expected, state));
    }

    public static DsonIOException bytesRemain(int bytesUntilLimit) {
        return new DsonIOException("bytes remain " + bytesUntilLimit);
    }

    public static DsonIOException containsHeaderDirectly(DsonToken token) {
        return new DsonIOException(String.format("header contains another header directly, token %s.", token));
    }

    public static DsonIOException invalidTokenType(DsonContextType contextType, DsonToken token) {
        return new DsonIOException(String.format("invalid token, contextType %s, token %s.", contextType, token));
    }

    public static DsonIOException invalidTokenType(DsonContextType contextType, DsonToken token, List<DsonTokenType> expected) {
        return new DsonIOException(String.format("invalid token, contextType %s, expected %s, but found %s.",
                contextType, expected, token));
    }

    public static DsonIOException invalidTopDsonType(DsonType dsonType) {
        return new DsonIOException("invalid topDsonValue, dsonType: " + dsonType);
    }

    // endregion
}