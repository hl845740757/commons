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
using Wjybxx.BTree.Leaf;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.BTree;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class WaitFrame1Codec<T> : AbstractDsonCodec<WaitFrame<T>> where T : class
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_required = "required";

    public override Type GetEncoderType() => typeof(WaitFrame<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref WaitFrame<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, typeof(Task<T>), null);
        writer.WriteInt(names_flags, inst.Flags, WireType.VarInt, NumberStyles.Simple);
        writer.WriteInt(names_required, inst.Required, WireType.VarInt, NumberStyles.Simple);
    }

    protected override WaitFrame<T> NewInstance(IDsonObjectReader reader) {
        return new WaitFrame<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref WaitFrame<T> inst) {
        inst.Guard = reader.ReadObject<Task<T>>(names_guard, typeof(Task<T>), null);
        inst.Flags = reader.ReadInt(names_flags);
        inst.Required = reader.ReadInt(names_required);
    }
}
}