#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Decorator;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.BTree;
using Wjybxx.Commons;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class SubtreeRefCodec<T> : AbstractDsonCodec<SubtreeRef<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_child = "child";
    public const string names_path = "path";

    public override Type GetEncoderType() => typeof(SubtreeRef<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in SubtreeRef<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_child, inst.Child, null);
        writer.WriteObject(names_path, inst.Path, null);
    }

    protected override SubtreeRef<T> NewInstance(IDsonObjectReader reader) {
        return new SubtreeRef<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref SubtreeRef<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.Child = reader.ReadObject<Task<T>>(null, null);
            inst.Path = reader.ReadObject<ObjectPath>(null, null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_child: inst.Child = reader.ReadObject<Task<T>>(null, null); break;
                case names_path: inst.Path = reader.ReadObject<ObjectPath>(null, null); break;
            }
        }
    }
}
}
