using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;

namespace Wjybxx.Dson.Tests.Apt;

[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class ThirdPartyBeanCodec : AbstractDsonCodec<ThirdPartyBean>
{
    public const string names_age = "age";
    public const string names_name = "name";

    public override Type GetEncoderClass() => typeof(ThirdPartyBean);

    protected override void WriteFields(IDsonObjectWriter writer, ref ThirdPartyBean inst, Type declaredType, ObjectStyle style) {
        writer.WriteInt(names_age, inst.Age, WireType.VarInt, NumberStyles.Simple);
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
    }

    protected override ThirdPartyBean NewInstance(IDsonObjectReader reader, Type declaredType) {
        return new ThirdPartyBean();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref ThirdPartyBean inst, Type declaredType) {
        inst.Age = reader.ReadInt(names_age);
        inst.Name = reader.ReadString(names_name);
    }
}