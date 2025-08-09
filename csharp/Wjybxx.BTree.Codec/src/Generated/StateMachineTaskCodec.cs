#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.BTree;
using System.Collections.Generic;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class StateMachineTaskCodec<T> : AbstractDsonCodec<StateMachineTask<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";
    public const string names_name = "name";
    public const string names_initStateName = "initStateName";
    public const string names_stateCfgs = "stateCfgs";
    public const string names_handler = "handler";

    public override Type GetEncoderType() => typeof(StateMachineTask<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in StateMachineTask<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, null);
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteString(names_initStateName, inst.InitStateName, StringStyle.Auto);
        writer.WriteObject(names_stateCfgs, inst.StateCfgs, null);
        writer.WriteObject(names_handler, inst.Handler, null);
    }

    protected override StateMachineTask<T> NewInstance(IDsonObjectReader reader) {
        return new StateMachineTask<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref StateMachineTask<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.Child = reader.ReadObject<Task<T>>(null, null);
            inst.Name = reader.ReadString(null);
            inst.InitStateName = reader.ReadString(null);
            inst.StateCfgs = reader.ReadObject<List<FsmStateCfg<T>>>(null, null);
            inst.Handler = reader.ReadObject<IStateMachineHandler<T>>(null, null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_child: inst.Child = reader.ReadObject<Task<T>>(null, null); break;
                case names_name: inst.Name = reader.ReadString(null); break;
                case names_initStateName: inst.InitStateName = reader.ReadString(null); break;
                case names_stateCfgs: inst.StateCfgs = reader.ReadObject<List<FsmStateCfg<T>>>(null, null); break;
                case names_handler: inst.Handler = reader.ReadObject<IStateMachineHandler<T>>(null, null); break;
            }
        }
    }
}
}
