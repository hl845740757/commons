#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.BTree;
using Wjybxx.Commons;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class ChangeStateTaskCodec<T> : AbstractDsonCodec<ChangeStateTask<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_statePath = "statePath";
    public const string names_stateProps = "stateProps";
    public const string names_machineName = "machineName";
    public const string names_delayMode = "delayMode";
    public const string names_delayArg = "delayArg";

    public override Type GetEncoderType() => typeof(ChangeStateTask<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in ChangeStateTask<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_statePath, inst.StatePath, null);
        writer.WriteObject(names_stateProps, inst.StateProps, null);
        writer.WriteString(names_machineName, inst.MachineName, StringStyle.Auto);
        writer.WriteByte(names_delayMode, inst.DelayMode, NumberStyles.Simple);
        writer.WriteInt(names_delayArg, inst.DelayArg, NumberStyles.Simple);
    }

    protected override ChangeStateTask<T> NewInstance(IDsonObjectReader reader) {
        return new ChangeStateTask<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref ChangeStateTask<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.StatePath = reader.ReadObject<ObjectPath>(null, null);
            inst.StateProps = reader.ReadObject<object>(null, null);
            inst.MachineName = reader.ReadString(null);
            inst.DelayMode = reader.ReadByte(null);
            inst.DelayArg = reader.ReadInt(null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_statePath: inst.StatePath = reader.ReadObject<ObjectPath>(null, null); break;
                case names_stateProps: inst.StateProps = reader.ReadObject<object>(null, null); break;
                case names_machineName: inst.MachineName = reader.ReadString(null); break;
                case names_delayMode: inst.DelayMode = reader.ReadByte(null); break;
                case names_delayArg: inst.DelayArg = reader.ReadInt(null); break;
            }
        }
    }
}
}
