#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.BTree;
using System.Collections.Generic;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class SwitchCodec<T> : AbstractDsonCodec<Switch<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";
    public const string names_handler = "handler";

    public override Type GetEncoderType() => typeof(Switch<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in Switch<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, null);
        writer.WriteObject(names_handler, inst.Handler, null);
    }

    protected override Switch<T> NewInstance(IDsonObjectReader reader) {
        return new Switch<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Switch<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_children)) inst.Children = reader.ReadObject<List<Task<T>>>(null, null);
        if (reader.ReadName(names_handler)) inst.Handler = reader.ReadObject<ISwitchHandler<T>>(null, null);
    }
}
}
