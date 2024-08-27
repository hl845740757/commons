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
using System.Runtime.Serialization;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.IO
{
/// <summary>
/// DsonIO操作异常
/// </summary>
public class DsonIOException : Exception
{
    public DsonIOException() {
    }

    public DsonIOException(string? message) : base(message) {
    }

    public DsonIOException(string? message, Exception? innerException) : base(message, innerException) {
    }

    protected DsonIOException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }

    public static DsonIOException Wrap(Exception e, string? message = null) {
        if (e is DsonIOException exception) {
            return exception;
        }
        return new DsonIOException(message, e);
    }

    // reader/writer
    public static DsonIOException RecursionLimitExceeded() {
        return new DsonIOException("Object had too many levels of nesting.");
    }

    public static DsonIOException ContextError(DsonContextType expected, DsonContextType contextType) {
        return new DsonIOException($"context error, expected {expected}, but found {contextType}");
    }

    public static DsonIOException ContextError(IList<DsonContextType> expected, DsonContextType contextType) {
        return new DsonIOException($"context error, expected {DsonInternals.ToString(expected)}, but found {contextType}");
    }

    public static DsonIOException ContextErrorTopLevel() {
        return new DsonIOException("context error, current state is TopLevel");
    }

    public static DsonIOException UnexpectedName(int expected, int name) {
        return new DsonIOException($"The number of the field does not match, expected {expected}, but found {name}");
    }

    public static DsonIOException UnexpectedName(string? expected, string name) {
        return new DsonIOException($"The name of the field does not match, expected {expected}, but found {name}");
    }

    public static DsonIOException UnexpectedName<T>(T? expected, T name) where T : IEquatable<T> {
        return new DsonIOException($"The name of the field does not match, expected {expected}, but found {name}");
    }

    public static DsonIOException DsonTypeMismatch(DsonType expected, DsonType dsonType) {
        return new DsonIOException($"The dsonType does not match, expected {expected}, but found {dsonType}");
    }

    public static DsonIOException InvalidDsonType(IList<DsonType> expected, DsonType dsonType) {
        return new DsonIOException($"The dson type is invalid in context, " +
                                   $"context: {DsonInternals.ToString(expected)}, dsonType: {dsonType}");
    }

    public static DsonIOException InvalidDsonType(DsonContextType contextType, DsonType dsonType) {
        return new DsonIOException($"The dson type is invalid in context, context: {contextType}, dsonType: {dsonType}");
    }

    public static DsonIOException UnexpectedSubType(int expected, int subType) {
        return new DsonIOException($"Unexpected subType, expected {expected}, but found {subType}");
    }

    public static DsonIOException InvalidState(DsonContextType contextType, IList<DsonReaderState> expected, DsonReaderState state) {
        return new DsonIOException($"invalid state, contextType {contextType}, " +
                                   $"expected {DsonInternals.ToString(expected)}, but found {state}.");
    }

    public static DsonIOException InvalidState(DsonContextType contextType, IList<DsonWriterState> expected, DsonWriterState state) {
        return new DsonIOException($"invalid state, contextType {contextType}, " +
                                   $"expected {DsonInternals.ToString(expected)}, but found {state}.");
    }

    public static DsonIOException BytesRemain(int bytesUntilLimit) {
        return new DsonIOException("bytes remain " + bytesUntilLimit);
    }

    public static DsonIOException ContainsHeaderDirectly(DsonToken token) {
        return new DsonIOException($"header contains another header directly, token {token}.");
    }

    public static DsonIOException InvalidTokenType(DsonContextType contextType, DsonToken token) {
        return new DsonIOException($"invalid token, contextType {contextType}, token {token}.");
    }

    public static DsonIOException InvalidTokenType(DsonContextType contextType, DsonToken token, DsonTokenType expected) {
        return new DsonIOException($"invalid token, contextType {contextType}, " +
                                   $"expected {expected}, but found {token}.");
    }

    public static DsonIOException InvalidTokenType(DsonContextType contextType, DsonToken token, IList<DsonTokenType> expected) {
        return new DsonIOException($"invalid token, contextType {contextType}, " +
                                   $"expected {DsonInternals.ToString(expected)}, but found {token}.");
    }

    public static DsonIOException InvalidTopDsonType(DsonType dsonType) {
        return new DsonIOException($"invalid topDsonValue, dsonType: {dsonType}");
    }
    // endregion
}
}