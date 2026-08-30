#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Reflection;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Codec.Codecs
{
internal class EnumCodecUtil
{
    private const int CACHE_SIZE = 64;
    private static readonly string[] numberStringArray;

    static EnumCodecUtil() {
        numberStringArray = new string[CACHE_SIZE];
        for (int number = 0; number < CACHE_SIZE; number++) {
            numberStringArray[number] = number.ToString();
        }
    }

    public static string ToString(int number) {
        if (number >= 0 && number < CACHE_SIZE) {
            return numberStringArray[number];
        }
        return number.ToString();
    }

    public static SerializeFeatures GetEncodeFeatures<T>() {
        DsonSerializableAttribute serializableAttribute = typeof(T).GetCustomAttribute<DsonSerializableAttribute>();
        return serializableAttribute == null ? default : serializableAttribute.EncodeFeatures;
    }

    public static DeserializeFeatures GetDecodeFeatures<T>() {
        DsonSerializableAttribute serializableAttribute = typeof(T).GetCustomAttribute<DsonSerializableAttribute>();
        return serializableAttribute == null ? default : serializableAttribute.DecodeFeatures;
    }
}
}