using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;

namespace Wjybxx.Dson.Tests.Apt;

[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class ThirdPartyBean2Codec : AbstractDsonCodec<ThirdPartyBean2>
{
    public const string names_age = "age";
    public const string names_name = "name";
    public const string names_Sex = "Sex";

    public override Type GetEncoderClass() => typeof(ThirdPartyBean2);

    protected override void BeforeEncode(IDsonObjectWriter writer, ref ThirdPartyBean2 inst, Type declaredType, ObjectStyle style) {
        LinkerBeanExample.BeforeEncode(inst, writer.Options);
    }

    protected override void WriteFields(IDsonObjectWriter writer, ref ThirdPartyBean2 inst, Type declaredType, ObjectStyle style) {
        LinkerBeanExample.WriteAge(inst, writer, names_age);
        writer.WriteString(names_name, inst.Name, StringStyle.Unquote);
        writer.WriteInt(names_Sex, inst.Sex, WireType.Uint, NumberStyles.Simple);
    }

    protected override ThirdPartyBean2 NewInstance(IDsonObjectReader reader, Type declaredType) {
        return new ThirdPartyBean2();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref ThirdPartyBean2 inst, Type declaredType) {
        LinkerBeanExample.ReadAge(inst, reader, names_age);
        inst.Name = reader.ReadString(names_name);
        inst.Sex = reader.ReadInt(names_Sex);
    }

    protected override void AfterDecode(IDsonObjectReader reader, ref ThirdPartyBean2 inst, Type declaredType) {
        LinkerBeanExample.AfterDecode(inst, reader.Options);
    }
}