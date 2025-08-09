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
public sealed class WaitFrameCodec<T> : AbstractDsonCodec<WaitFrame<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_required = "required";

    public override Type GetEncoderType() => typeof(WaitFrame<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in WaitFrame<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteInt(names_required, inst.Required, NumberStyles.Simple);
    }

    protected override WaitFrame<T> NewInstance(IDsonObjectReader reader) {
        return new WaitFrame<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref WaitFrame<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.Required = reader.ReadInt(null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_required: inst.Required = reader.ReadInt(null); break;
            }
        }
    }
}
}
