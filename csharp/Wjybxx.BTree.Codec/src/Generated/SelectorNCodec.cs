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
public sealed class SelectorNCodec<T> : AbstractDsonCodec<SelectorN<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";
    public const string names_required = "required";
    public const string names_failFast = "failFast";

    public override Type GetEncoderType() => typeof(SelectorN<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in SelectorN<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, null);
        writer.WriteInt(names_required, inst.Required, NumberStyles.Simple);
        writer.WriteBool(names_failFast, inst.FailFast);
    }

    protected override SelectorN<T> NewInstance(IDsonObjectReader reader) {
        return new SelectorN<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref SelectorN<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_children)) inst.Children = reader.ReadObject<List<Task<T>>>(null, null);
        if (reader.ReadName(names_required)) inst.Required = reader.ReadInt(null);
        if (reader.ReadName(names_failFast)) inst.FailFast = reader.ReadBool(null);
    }
}
}
