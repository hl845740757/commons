#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
public class Int32Codec : IDsonCodec<int>, IKeyCodec<int>
{
    public string EncodeKey(int value, SerializeFeatures features) {
        return value.ToString();
        // return features.ToNumberStyle().ToString(value).Value;
    }

    public int DecodeKey(string keyString) {
        return DsonTexts.ParseInt32(keyString);
    }

    public void WriteObject(IDsonObjectWriter writer, int inst, Type declaredType, SerializeFeatures features) {
        if (declaredType != typeof(int)) {
            features |= SerializeFeatures.NumberTyped;
        }
        writer.WriteInt(inst, features);
    }

    public int ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        return reader.ReadInt();
    }
}
}