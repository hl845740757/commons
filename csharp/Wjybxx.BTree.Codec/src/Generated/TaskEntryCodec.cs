#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class TaskEntryCodec<T> : AbstractDsonCodec<TaskEntry<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_name = "name";
    public const string names_rootTask = "rootTask";
    public const string names_type = "type";

    public override Type GetEncoderType() => typeof(TaskEntry<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in TaskEntry<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteObject(names_rootTask, inst.RootTask, null);
        writer.WriteByte(names_type, inst.Type, NumberStyles.Simple);
    }

    protected override TaskEntry<T> NewInstance(IDsonObjectReader reader) {
        return new TaskEntry<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref TaskEntry<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_name)) inst.Name = reader.ReadString(null);
        if (reader.ReadName(names_rootTask)) inst.RootTask = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_type)) inst.Type = reader.ReadByte(null);
    }
}
}
