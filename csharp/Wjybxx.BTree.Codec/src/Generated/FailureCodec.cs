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
public sealed class FailureCodec<T> : AbstractDsonCodec<Failure<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_failureStatus = "failureStatus";

    public override Type GetEncoderType() => typeof(Failure<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in Failure<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteInt(names_failureStatus, inst.FailureStatus, NumberStyles.Simple);
    }

    protected override Failure<T> NewInstance(IDsonObjectReader reader) {
        return new Failure<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Failure<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_failureStatus)) inst.FailureStatus = reader.ReadInt(null);
    }
}
}
