#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using System.Numerics;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests.Apt
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class Vector2Codec : AbstractDsonCodec<Vector2>
{
    public const string names_X = "X";
    public const string names_Y = "Y";

    public override Type GetEncoderType() => typeof(Vector2);

    protected override void BeforeEncode(IDsonObjectWriter writer, ref Vector2 inst) {
        Vector2CodecProxy.BeforeEncode(ref inst, writer.Options);
    }

    protected override void WriteFields(IDsonObjectWriter writer, in Vector2 inst) {
        Vector2CodecProxy.WriteObject(in inst, writer);
        writer.WriteFloat(names_X, inst.X, NumberStyles.Simple);
        writer.WriteFloat(names_Y, inst.Y, NumberStyles.Simple);
    }

    protected override Vector2 NewInstance(IDsonObjectReader reader) {
        return default;
    }

    protected override void ReadFields(IDsonObjectReader reader, ref Vector2 inst) {
        Vector2CodecProxy.ReadObject(ref inst, reader);
        inst.X = reader.ReadFloat(names_X);
        inst.Y = reader.ReadFloat(names_Y);
    }

    protected override void AfterDecode(IDsonObjectReader reader, ref Vector2 inst) {
        Vector2CodecProxy.AfterDecode(ref inst, reader.Options);
    }
}
}