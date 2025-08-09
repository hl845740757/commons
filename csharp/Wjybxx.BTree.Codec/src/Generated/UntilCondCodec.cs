#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Decorator;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.BTree;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class UntilCondCodec<T> : AbstractDsonCodec<UntilCond<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";
    public const string names_maxLoop = "maxLoop";
    public const string names_cond = "cond";

    public override Type GetEncoderType() => typeof(UntilCond<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in UntilCond<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, null);
        writer.WriteInt(names_maxLoop, inst.MaxLoop, NumberStyles.Simple);
        writer.WriteObject(names_cond, inst.Cond, null);
    }

    protected override UntilCond<T> NewInstance(IDsonObjectReader reader) {
        return new UntilCond<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref UntilCond<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.Child = reader.ReadObject<Task<T>>(null, null);
            inst.MaxLoop = reader.ReadInt(null);
            inst.Cond = reader.ReadObject<Task<T>>(null, null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_child: inst.Child = reader.ReadObject<Task<T>>(null, null); break;
                case names_maxLoop: inst.MaxLoop = reader.ReadInt(null); break;
                case names_cond: inst.Cond = reader.ReadObject<Task<T>>(null, null); break;
            }
        }
    }
}
}
