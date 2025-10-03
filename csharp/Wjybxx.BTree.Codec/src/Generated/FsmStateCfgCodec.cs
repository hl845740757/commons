#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;
using Wjybxx.Commons;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class FsmStateCfgCodec<T> : AbstractDsonCodec<FsmStateCfg<T>> where T : class 
{
    public const string names_name = "name";
    public const string names_path = "path";
    public const string names_props = "props";

    public override Type GetEncoderType() => typeof(FsmStateCfg<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in FsmStateCfg<T> inst) {
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteObject(names_path, inst.Path, null);
        writer.WriteObject(names_props, inst.Props, null);
    }

    protected override FsmStateCfg<T> NewInstance(IDsonObjectReader reader) {
        return new FsmStateCfg<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref FsmStateCfg<T> inst) {
        if (reader.ContextType == DsonContextType.Array) {
            inst.Name = reader.ReadString(null);
            inst.Path = reader.ReadObject<ObjectPath>(null, null);
            inst.Props = reader.ReadObject<object>(null, null);
            return;
        }
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            switch (reader.ReadName()) {
                case names_name: inst.Name = reader.ReadString(null); break;
                case names_path: inst.Path = reader.ReadObject<ObjectPath>(null, null); break;
                case names_props: inst.Props = reader.ReadObject<object>(null, null); break;
            }
        }
    }
}
}
