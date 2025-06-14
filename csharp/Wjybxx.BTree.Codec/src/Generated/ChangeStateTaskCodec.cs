#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.BTree;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class ChangeStateTaskCodec<T> : AbstractDsonCodec<ChangeStateTask<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_nextStateGuid = "nextStateGuid";
    public const string names_stateProps = "stateProps";
    public const string names_machineName = "machineName";
    public const string names_delayMode = "delayMode";
    public const string names_delayArg = "delayArg";

    public override Type GetEncoderType() => typeof(ChangeStateTask<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in ChangeStateTask<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteString(names_nextStateGuid, inst.NextStateGuid, StringStyle.Auto);
        writer.WriteObject(names_stateProps, inst.StateProps, null);
        writer.WriteString(names_machineName, inst.MachineName, StringStyle.Auto);
        writer.WriteByte(names_delayMode, inst.DelayMode, NumberStyles.Simple);
        writer.WriteInt(names_delayArg, inst.DelayArg, NumberStyles.Simple);
    }

    protected override ChangeStateTask<T> NewInstance(IDsonObjectReader reader) {
        return new ChangeStateTask<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref ChangeStateTask<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_nextStateGuid)) inst.NextStateGuid = reader.ReadString(null);
        if (reader.ReadName(names_stateProps)) inst.StateProps = reader.ReadObject<object>(null, null);
        if (reader.ReadName(names_machineName)) inst.MachineName = reader.ReadString(null);
        if (reader.ReadName(names_delayMode)) inst.DelayMode = reader.ReadByte(null);
        if (reader.ReadName(names_delayArg)) inst.DelayArg = reader.ReadInt(null);
    }
}
}
