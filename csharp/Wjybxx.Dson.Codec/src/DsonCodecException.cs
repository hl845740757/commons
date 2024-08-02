#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using System.Runtime.Serialization;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 用于表示对象序列化过程中的异常
/// </summary>
public class DsonCodecException : DsonIOException
{
    public DsonCodecException() {
    }

    public DsonCodecException(string? message) : base(message) {
    }

    public DsonCodecException(string? message, Exception? innerException) : base(message, innerException) {
    }

    public static DsonCodecException UnsupportedType(Type type) {
        return new DsonCodecException("Can't find a codec for " + type);
    }

    public static DsonCodecException UnsupportedKeyType(Type type) {
        return new DsonCodecException("Can't find a codec for " + type + ", or key is not EnumLite");
    }

    public static DsonCodecException EnumAbsent(Type declared, string value) {
        return new DsonCodecException($"EnumLite is absent, declared: {declared}, number: {value}");
    }

    public static DsonCodecException Incompatible(Type declared, DsonType dsonType) {
        return new DsonCodecException($"Incompatible data format, declaredType {declared}, dsonType {dsonType}");
    }

    public static DsonCodecException Incompatible(DsonType expected, DsonType dsonType) {
        return new DsonCodecException($"Incompatible data format, expected {expected}, dsonType {dsonType}");
    }

    public static DsonCodecException Incompatible<T>(Type declared, T classId) {
        return new DsonCodecException($"Incompatible data format, declaredType {declared}, classId {classId}");
    }
}
}