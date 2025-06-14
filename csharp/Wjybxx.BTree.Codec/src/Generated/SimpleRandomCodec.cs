#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Leaf;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.BTree;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class SimpleRandomCodec<T> : AbstractDsonCodec<SimpleRandom<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_p = "p";

    public override Type GetEncoderType() => typeof(SimpleRandom<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in SimpleRandom<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteFloat(names_p, inst.P, NumberStyles.Simple);
    }

    protected override SimpleRandom<T> NewInstance(IDsonObjectReader reader) {
        return new SimpleRandom<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref SimpleRandom<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_p)) inst.P = reader.ReadFloat(null);
    }
}
}
