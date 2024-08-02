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
public class Int32Codec : IDsonCodec<int>
{
    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, ref int inst, Type declaredType, ObjectStyle style) {
        // 由字典或List调用时，declaredType可能是匹配的
        INumberStyle numberStyle = declaredType == typeof(int)
            ? NumberStyles.Simple
            : NumberStyles.Typed;
        writer.WriteInt(null, inst, WireType.VarInt, numberStyle);
    }

    public int ReadObject(IDsonObjectReader reader, Type declaredType, Func<int>? factory = null) {
        return reader.ReadInt(reader.CurrentName);
    }
}
}