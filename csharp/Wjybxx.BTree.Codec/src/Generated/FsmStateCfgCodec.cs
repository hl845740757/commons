#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class FsmStateCfgCodec<T> : AbstractDsonCodec<FsmStateCfg<T>> where T : class 
{
    public const string names_name = "name";
    public const string names_guid = "guid";
    public const string names_props = "props";

    public override Type GetEncoderType() => typeof(FsmStateCfg<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in FsmStateCfg<T> inst) {
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteString(names_guid, inst.Guid, StringStyle.Auto);
        writer.WriteObject(names_props, inst.Props, null);
    }

    protected override FsmStateCfg<T> NewInstance(IDsonObjectReader reader) {
        return new FsmStateCfg<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref FsmStateCfg<T> inst) {
        if (reader.ReadName(names_name)) inst.Name = reader.ReadString(null);
        if (reader.ReadName(names_guid)) inst.Guid = reader.ReadString(null);
        if (reader.ReadName(names_props)) inst.Props = reader.ReadObject<object>(null, null);
    }
}
}
