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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec.Codecs
{
public class DateTimeOffsetCodec : IDsonCodec<DateTimeOffset>
{
    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, ref DateTimeOffset inst, Type declaredType, ObjectStyle style) {
        int offset = (int)inst.Offset.TotalSeconds;
        ExtDateTime extDateTime = ExtDateTime.OfDateTime(inst.DateTime).WithOffset(offset);
        writer.WriteExtDateTime(null, extDateTime);
    }

    public DateTimeOffset ReadObject(IDsonObjectReader reader, Type declaredType, Func<DateTimeOffset>? factory = null) {
        ExtDateTime extDateTime = reader.ReadExtDateTime(reader.CurrentName);
        DateTime dateTime = extDateTime.ToDateTime();
        return new DateTimeOffset(dateTime, TimeSpan.FromSeconds(extDateTime.Offset));
    }
}
}