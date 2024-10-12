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
using System;
using Wjybxx.BTree;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;
using System.Collections.Generic;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class FixedSwitch1Codec<T> : AbstractDsonCodec<FixedSwitch<T>> where T : class
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";
    public const string names_handler = "handler";
    public const string names_branch1 = "branch1";
    public const string names_branch2 = "branch2";
    public const string names_branch3 = "branch3";
    public const string names_branch4 = "branch4";
    public const string names_branch5 = "branch5";

    public override Type GetEncoderType() => typeof(FixedSwitch<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref FixedSwitch<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, typeof(Task<T>), null);
        writer.WriteInt(names_flags, inst.Flags, WireType.VarInt, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, typeof(List<Task<T>>), null);
        writer.WriteObject(names_handler, inst.Handler, typeof(ISwitchHandler<T>), null);
        writer.WriteObject(names_branch1, inst.Branch1, typeof(Task<T>), null);
        writer.WriteObject(names_branch2, inst.Branch2, typeof(Task<T>), null);
        writer.WriteObject(names_branch3, inst.Branch3, typeof(Task<T>), null);
        writer.WriteObject(names_branch4, inst.Branch4, typeof(Task<T>), null);
        writer.WriteObject(names_branch5, inst.Branch5, typeof(Task<T>), null);
    }

    protected override FixedSwitch<T> NewInstance(IDsonObjectReader reader) {
        return new FixedSwitch<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref FixedSwitch<T> inst) {
        inst.Guard = reader.ReadObject<Task<T>>(names_guard, typeof(Task<T>), null);
        inst.Flags = reader.ReadInt(names_flags);
        inst.Children = reader.ReadObject<List<Task<T>>>(names_children, typeof(List<Task<T>>), null);
        inst.Handler = reader.ReadObject<ISwitchHandler<T>>(names_handler, typeof(ISwitchHandler<T>), null);
        inst.Branch1 = reader.ReadObject<Task<T>>(names_branch1, typeof(Task<T>), null);
        inst.Branch2 = reader.ReadObject<Task<T>>(names_branch2, typeof(Task<T>), null);
        inst.Branch3 = reader.ReadObject<Task<T>>(names_branch3, typeof(Task<T>), null);
        inst.Branch4 = reader.ReadObject<Task<T>>(names_branch4, typeof(Task<T>), null);
        inst.Branch5 = reader.ReadObject<Task<T>>(names_branch5, typeof(Task<T>), null);
    }
}
}