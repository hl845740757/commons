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
using System.Collections.Generic;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
public class PairCodec<K, V> : IDsonCodec<KeyValuePair<K, V>>
{
    public void WriteObject(IDsonObjectWriter writer, KeyValuePair<K, V> inst, Type declaredType, SerializeFeatures features) {
        Type encoderType = typeof(KeyValuePair<K, V>);
        if ((features & SerializeFeatures.WriteAsArray) != 0) {
            writer.WriteStartArray(encoderType, features);
            writer.WriteObject(inst.Key);
            writer.WriteObject(inst.Value);
            writer.WriteEndArray();
        } else {
            writer.WriteStartObject(encoderType, features);
            writer.WriteObject("key", inst.Key);
            writer.WriteObject("value", inst.Value);
            writer.WriteEndObject();
        }
    }

    public KeyValuePair<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        if (reader.CurrentDsonType == DsonType.Object) {
            reader.ReadStartObject();
            K key = reader.ReadObject<K>("key");
            V value = reader.ReadObject<V>("value");
            reader.ReadEndObject();
            return new KeyValuePair<K, V>(key, value);
        } else {
            // Array
            reader.ReadStartArray();
            K key = reader.ReadObject<K>();
            V value = reader.ReadObject<V>();
            reader.ReadEndArray();
            return new KeyValuePair<K, V>(key, value);
        }
    }
}
}