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
using Wjybxx.Dson.Text;
using Wjybxx.BTree;
using Wjybxx.Dson;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class Failure1Codec<T> : AbstractDsonCodec<Failure<T>> where T : class
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_failureStatus = "failureStatus";

    public override Type GetEncoderClass() => typeof(Failure<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref Failure<T> inst, Type declaredType, ObjectStyle style) {
        writer.WriteObject(names_guard, inst.Guard, typeof(Task<T>), null);
        writer.WriteInt(names_flags, inst.Flags, WireType.VarInt, NumberStyles.Simple);
        writer.WriteInt(names_failureStatus, inst.FailureStatus, WireType.VarInt, NumberStyles.Simple);
    }

    protected override Failure<T> NewInstance(IDsonObjectReader reader, Type declaredType) {
        return new Failure<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Failure<T> inst, Type declaredType) {
        inst.Guard = reader.ReadObject<Task<T>>(names_guard, typeof(Task<T>), null);
        inst.Flags = reader.ReadInt(names_flags);
        inst.FailureStatus = reader.ReadInt(names_failureStatus);
    }
}
}