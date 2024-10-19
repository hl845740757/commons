#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;

namespace Wjybxx.Dson.Tests.Apt
{
  [Generated("Wjybxx.Dson.Apt.CodecProcessor")]
  public sealed class ThirdPartyBean2Codec : AbstractDsonCodec<ThirdPartyBean2>
  {
    public const string names_age = "age";
    public const string names_name = "name";
    public const string names_Sex = "Sex";

    public override Type GetEncoderType() => typeof(ThirdPartyBean2);

    protected override void BeforeEncode(IDsonObjectWriter writer, ref ThirdPartyBean2 inst) {
      LinkerBeanExample.BeforeEncode(inst, writer.Options);
    }

    protected override void WriteFields(IDsonObjectWriter writer, ref ThirdPartyBean2 inst) {
      LinkerBeanExample.WriteAge(inst, writer, names_age);
      writer.WriteString(names_name, inst.Name, StringStyle.Unquote);
      writer.WriteInt(names_Sex, inst.Sex, WireType.Uint, NumberStyles.Simple);
    }

    protected override ThirdPartyBean2 NewInstance(IDsonObjectReader reader) {
      return new ThirdPartyBean2();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref ThirdPartyBean2 inst) {
      LinkerBeanExample.ReadAge(inst, reader, names_age);
      inst.Name = reader.ReadString(names_name);
      inst.Sex = reader.ReadInt(names_Sex);
    }

    protected override void AfterDecode(IDsonObjectReader reader, ref ThirdPartyBean2 inst) {
      LinkerBeanExample.AfterDecode(inst, reader.Options);
    }
  }
}
