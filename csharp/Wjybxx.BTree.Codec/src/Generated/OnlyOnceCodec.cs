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
public sealed class OnlyOnceCodec<T> : AbstractDsonCodec<OnlyOnce<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";

    public override Type GetEncoderType() => typeof(OnlyOnce<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in OnlyOnce<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, null);
    }

    protected override OnlyOnce<T> NewInstance(IDsonObjectReader reader) {
        return new OnlyOnce<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref OnlyOnce<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.Child = reader.ReadObject<Task<T>>(null, null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_child: inst.Child = reader.ReadObject<Task<T>>(null, null); break;
            }
        }
    }
}
}
