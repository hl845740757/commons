#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Leaf;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.BTree;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class RunningCodec<T> : AbstractDsonCodec<Running<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";

    public override Type GetEncoderType() => typeof(Running<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in Running<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
    }

    protected override Running<T> NewInstance(IDsonObjectReader reader) {
        return new Running<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Running<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
            }
        }
    }
}
}
