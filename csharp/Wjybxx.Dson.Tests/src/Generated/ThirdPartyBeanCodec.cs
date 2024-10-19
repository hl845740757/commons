#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests.Apt
{
  [Generated("Wjybxx.Dson.Apt.CodecProcessor")]
  public sealed class ThirdPartyBeanCodec : AbstractDsonCodec<ThirdPartyBean>
  {
    public const string names_age = "age";
    public const string names_name = "name";

    public override Type GetEncoderType() => typeof(ThirdPartyBean);

    protected override void WriteFields(IDsonObjectWriter writer, ref ThirdPartyBean inst) {
      writer.WriteInt(names_age, inst.Age, WireType.VarInt, NumberStyles.Simple);
      writer.WriteString(names_name, inst.Name, StringStyle.Auto);
    }

    protected override ThirdPartyBean NewInstance(IDsonObjectReader reader) {
      return new ThirdPartyBean();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref ThirdPartyBean inst) {
      inst.Age = reader.ReadInt(names_age);
      inst.Name = reader.ReadString(names_name);
    }
  }
}
