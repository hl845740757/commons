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
public sealed class SequenceCodec<T> : AbstractDsonCodec<Sequence<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";

    public override Type GetEncoderType() => typeof(Sequence<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in Sequence<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, null);
    }

    protected override Sequence<T> NewInstance(IDsonObjectReader reader) {
        return new Sequence<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Sequence<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_children)) inst.Children = reader.ReadObject<List<Task<T>>>(null, null);
    }
}
}
