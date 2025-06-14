#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Decorator;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.BTree;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class RepeatCodec<T> : AbstractDsonCodec<Repeat<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";
    public const string names_maxLoop = "maxLoop";
    public const string names_countMode = "countMode";
    public const string names_required = "required";

    public override Type GetEncoderType() => typeof(Repeat<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in Repeat<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, null);
        writer.WriteInt(names_maxLoop, inst.MaxLoop, NumberStyles.Simple);
        writer.WriteInt(names_countMode, inst.CountMode, NumberStyles.Simple);
        writer.WriteInt(names_required, inst.Required, NumberStyles.Simple);
    }

    protected override Repeat<T> NewInstance(IDsonObjectReader reader) {
        return new Repeat<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Repeat<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_child)) inst.Child = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_maxLoop)) inst.MaxLoop = reader.ReadInt(null);
        if (reader.ReadName(names_countMode)) inst.CountMode = reader.ReadInt(null);
        if (reader.ReadName(names_required)) inst.Required = reader.ReadInt(null);
    }
}
}
