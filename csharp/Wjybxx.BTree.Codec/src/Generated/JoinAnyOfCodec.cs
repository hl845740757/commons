#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class JoinAnyOfCodec<T> : AbstractDsonCodec<JoinAnyOf<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(JoinAnyOf<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in JoinAnyOf<T> inst) {
    }

    protected override JoinAnyOf<T> NewInstance(IDsonObjectReader reader) {
        return JoinAnyOf<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinAnyOf<T> inst) {
    }
}
}
