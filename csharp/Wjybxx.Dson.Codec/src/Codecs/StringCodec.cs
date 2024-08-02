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
public class StringCodec : IDsonCodec<string>
{
    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, ref string inst, Type declaredType, ObjectStyle style) {
        writer.WriteString(null, inst, StringStyle.Auto);
    }

    public string ReadObject(IDsonObjectReader reader, Type declaredType, Func<string>? factory = null) {
        return reader.ReadString(reader.CurrentName);
    }
}
}