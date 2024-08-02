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
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.BTree;
using Wjybxx.Dson;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class StateMachineTask1Codec<T> : AbstractDsonCodec<StateMachineTask<T>> where T : class
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";
    public const string names_name = "name";
    public const string names_noneChildStatus = "noneChildStatus";
    public const string names_initState = "initState";
    public const string names_initStateProps = "initStateProps";

    public override Type GetEncoderClass() => typeof(StateMachineTask<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref StateMachineTask<T> inst, Type declaredType, ObjectStyle style) {
        writer.WriteObject(names_guard, inst.Guard, typeof(Task<T>), null);
        writer.WriteInt(names_flags, inst.Flags, WireType.VarInt, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, typeof(Task<T>), null);
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteInt(names_noneChildStatus, inst.NoneChildStatus, WireType.VarInt, NumberStyles.Simple);
        writer.WriteObject(names_initState, inst.InitState, typeof(Task<T>), null);
        writer.WriteObject(names_initStateProps, inst.InitStateProps, typeof(object), null);
    }

    protected override StateMachineTask<T> NewInstance(IDsonObjectReader reader, Type declaredType) {
        return new StateMachineTask<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref StateMachineTask<T> inst, Type declaredType) {
        inst.Guard = reader.ReadObject<Task<T>>(names_guard, typeof(Task<T>), null);
        inst.Flags = reader.ReadInt(names_flags);
        inst.Child = reader.ReadObject<Task<T>>(names_child, typeof(Task<T>), null);
        inst.Name = reader.ReadString(names_name);
        inst.NoneChildStatus = reader.ReadInt(names_noneChildStatus);
        inst.InitState = reader.ReadObject<Task<T>>(names_initState, typeof(Task<T>), null);
        inst.InitStateProps = reader.ReadObject<object>(names_initStateProps, typeof(object), null);
    }
}
}