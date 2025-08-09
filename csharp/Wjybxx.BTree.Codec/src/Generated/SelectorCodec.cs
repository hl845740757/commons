#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.BTree;
using System.Collections.Generic;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class SelectorCodec<T> : AbstractDsonCodec<Selector<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";

    public override Type GetEncoderType() => typeof(Selector<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in Selector<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, null);
    }

    protected override Selector<T> NewInstance(IDsonObjectReader reader) {
        return new Selector<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Selector<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Guard = reader.ReadObject<Task<T>>(null, null);
            inst.Flags = reader.ReadInt(null);
            inst.Children = reader.ReadObject<List<Task<T>>>(null, null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_guard: inst.Guard = reader.ReadObject<Task<T>>(null, null); break;
                case names_flags: inst.Flags = reader.ReadInt(null); break;
                case names_children: inst.Children = reader.ReadObject<List<Task<T>>>(null, null); break;
            }
        }
    }
}
}
