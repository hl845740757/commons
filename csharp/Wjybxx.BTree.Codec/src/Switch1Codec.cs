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
using Wjybxx.BTree.Branch;
using Wjybxx.Dson.Codec;
using Wjybxx.BTree;
using System.Collections.Generic;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class Switch1Codec<T> : AbstractDsonCodec<Switch<T>> where T : class
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";
    public const string names_handler = "handler";
    public static readonly Func<List<Task<T>>> factories_children = () => new List<Task<T>>();

    public override Type GetEncoderClass() => typeof(Switch<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref Switch<T> inst, Type declaredType, ObjectStyle style) {
        writer.WriteObject(names_guard, inst.Guard, typeof(Task<T>), null);
        writer.WriteInt(names_flags, inst.Flags, WireType.VarInt, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, typeof(List<Task<T>>), null);
        writer.WriteObject(names_handler, inst.Handler, typeof(ISwitchHandler<T>), null);
    }

    protected override Switch<T> NewInstance(IDsonObjectReader reader, Type declaredType) {
        return new Switch<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Switch<T> inst, Type declaredType) {
        inst.Guard = reader.ReadObject<Task<T>>(names_guard, typeof(Task<T>), null);
        inst.Flags = reader.ReadInt(names_flags);
        inst.Children = reader.ReadObject<List<Task<T>>>(names_children, typeof(List<Task<T>>), factories_children);
        inst.Handler = reader.ReadObject<ISwitchHandler<T>>(names_handler, typeof(ISwitchHandler<T>), null);
    }
}
}