#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class JoinSelectorNCodec<T> : AbstractDsonCodec<JoinSelectorN<T>> where T : class 
{
    public const string names_required = "required";
    public const string names_failFast = "failFast";
    public const string names_sequence = "sequence";

    public override Type GetEncoderType() => typeof(JoinSelectorN<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in JoinSelectorN<T> inst) {
        writer.WriteInt(names_required, inst.Required, NumberStyles.Simple);
        writer.WriteBool(names_failFast, inst.FailFast);
        writer.WriteInt(names_sequence, inst.Sequence, NumberStyles.Simple);
    }

    protected override JoinSelectorN<T> NewInstance(IDsonObjectReader reader) {
        return new JoinSelectorN<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinSelectorN<T> inst) {
        if (reader.ReadName(names_required)) inst.Required = reader.ReadInt(null);
        if (reader.ReadName(names_failFast)) inst.FailFast = reader.ReadBool(null);
        if (reader.ReadName(names_sequence)) inst.Sequence = reader.ReadInt(null);
    }
}
}
