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

#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class JoinSelectorN1Codec<T> : AbstractDsonCodec<JoinSelectorN<T>> where T : class
{
    public const string names_required = "required";
    public const string names_failFast = "failFast";
    public const string names_sequence = "sequence";

    public override Type GetEncoderType() => typeof(JoinSelectorN<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref JoinSelectorN<T> inst) {
        writer.WriteInt(names_required, inst.Required, WireType.VarInt, NumberStyles.Simple);
        writer.WriteBool(names_failFast, inst.FailFast);
        writer.WriteInt(names_sequence, inst.Sequence, WireType.VarInt, NumberStyles.Simple);
    }

    protected override JoinSelectorN<T> NewInstance(IDsonObjectReader reader) {
        return new JoinSelectorN<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinSelectorN<T> inst) {
        inst.Required = reader.ReadInt(names_required);
        inst.FailFast = reader.ReadBool(names_failFast);
        inst.Sequence = reader.ReadInt(names_sequence);
    }
}
}