#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests.Apt
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class MyStructCodec : AbstractDsonCodec<MyStruct>
{
    public const string names_x = "x";
    public const string names_y = "y";

    public override Type GetEncoderType() => typeof(MyStruct);

    protected override void BeforeEncode(IDsonObjectWriter writer, ref MyStruct inst) {
        inst.BeforeEncode(writer.Options);
    }

    protected override void WriteFields(IDsonObjectWriter writer, in MyStruct inst) {
        writer.WriteFloat(names_x, inst.x, NumberStyles.Simple);
        writer.WriteFloat(names_y, inst.y, NumberStyles.Simple);
    }

    protected override MyStruct NewInstance(IDsonObjectReader reader) {
        return default;
    }

    protected override void ReadFields(IDsonObjectReader reader, ref MyStruct inst) {
        inst.x = reader.ReadFloat(names_x);
        inst.y = reader.ReadFloat(names_y);
    }
}
}