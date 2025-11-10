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
        SerializeFeatures selfFeatures = features.ErasureElementFeatures();
        SerializeFeatures elementFeatures = features.GetElementFeatures();
        //
        Type encoderType = typeof(KeyValuePair<K, V>);
        if ((features & SerializeFeatures.WriteAsArray) != 0) {
            writer.WriteStartArray(encoderType, selfFeatures);
            writer.WriteObject(inst.Key);
            writer.WriteObject(inst.Value, elementFeatures);
            writer.WriteEndArray();
        } else {
            writer.WriteStartObject(encoderType, selfFeatures);
            writer.WriteObject("key", inst.Key);
            writer.WriteObject("value", inst.Value, elementFeatures);
            writer.WriteEndObject();
        }
    }

    public KeyValuePair<K, V> ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        DeserializeFeatures selfFeatures = features.ErasureElementFeatures();
        DeserializeFeatures elementFeatures = features.GetElementFeatures();
        //
        Type encoderType = typeof(KeyValuePair<K, V>);
        if (reader.CurrentDsonType == DsonType.Object) {
            reader.ReadStartObject(encoderType, selfFeatures);
            K key = reader.ReadObject<K>("key", 0);
            V value = reader.ReadObject<V>("value", elementFeatures);
            reader.ReadEndObject();
            return new KeyValuePair<K, V>(key, value);
        } else {
            // Array
            reader.ReadStartArray(encoderType, selfFeatures);
            K key = reader.ReadObject<K>(0);
            V value = reader.ReadObject<V>(elementFeatures);
            reader.ReadEndArray();
            return new KeyValuePair<K, V>(key, value);
        }
    }
}
}