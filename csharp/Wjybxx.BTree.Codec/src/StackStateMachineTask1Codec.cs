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
using Wjybxx.BTree;
using Wjybxx.Dson.Text;
using System.Collections.Generic;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class StackStateMachineTask1Codec<T> : AbstractDsonCodec<StackStateMachineTask<T>> where T : class
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";
    public const string names_name = "name";
    public const string names_initStateName = "initStateName";
    public const string names_stateCfgs = "stateCfgs";
    public const string names_handler = "handler";
    public const string names_undoQueueCapacity = "undoQueueCapacity";
    public const string names_redoQueueCapacity = "redoQueueCapacity";

    public override Type GetEncoderType() => typeof(StackStateMachineTask<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in StackStateMachineTask<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, typeof(Task<T>), null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, typeof(Task<T>), null);
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteString(names_initStateName, inst.InitStateName, StringStyle.Auto);
        writer.WriteObject(names_stateCfgs, inst.StateCfgs, typeof(List<FsmStateCfg<T>>), null);
        writer.WriteObject(names_handler, inst.Handler, typeof(IStateMachineHandler<T>), null);
        writer.WriteInt(names_undoQueueCapacity, inst.UndoQueueCapacity, NumberStyles.Simple);
        writer.WriteInt(names_redoQueueCapacity, inst.RedoQueueCapacity, NumberStyles.Simple);
    }

    protected override StackStateMachineTask<T> NewInstance(IDsonObjectReader reader) {
        return new StackStateMachineTask<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref StackStateMachineTask<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, typeof(Task<T>), null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_child)) inst.Child = reader.ReadObject<Task<T>>(null, typeof(Task<T>), null);
        if (reader.ReadName(names_name)) inst.Name = reader.ReadString(null);
        if (reader.ReadName(names_initStateName)) inst.InitStateName = reader.ReadString(null);
        if (reader.ReadName(names_stateCfgs)) inst.StateCfgs = reader.ReadObject<List<FsmStateCfg<T>>>(null, typeof(List<FsmStateCfg<T>>), null);
        if (reader.ReadName(names_handler)) inst.Handler = reader.ReadObject<IStateMachineHandler<T>>(null, typeof(IStateMachineHandler<T>), null);
        if (reader.ReadName(names_undoQueueCapacity)) inst.UndoQueueCapacity = reader.ReadInt(null);
        if (reader.ReadName(names_redoQueueCapacity)) inst.RedoQueueCapacity = reader.ReadInt(null);
    }
}
}