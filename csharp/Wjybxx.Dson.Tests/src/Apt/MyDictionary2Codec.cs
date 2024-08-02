#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System.Collections.Generic;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests.Apt;

[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class MyDictionary2Codec<TKey, TValue> : AbstractDsonCodec<MyDictionary<TKey, TValue>>
{
    public const string names_dictionary = "dictionary";
    public static readonly Func<Dictionary<TKey, TValue>> factories_dictionary = () => new Dictionary<TKey, TValue>();

    public override Type GetEncoderClass() => typeof(MyDictionary<TKey, TValue>);

    protected override void WriteFields(IDsonObjectWriter writer, ref MyDictionary<TKey, TValue> inst, Type declaredType, ObjectStyle style) {
        writer.WriteObject(names_dictionary, inst.dictionary, typeof(Dictionary<TKey, TValue>), null);
    }

    protected override MyDictionary<TKey, TValue> NewInstance(IDsonObjectReader reader, Type declaredType) {
        return new MyDictionary<TKey, TValue>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref MyDictionary<TKey, TValue> inst, Type declaredType) {
        inst.dictionary = reader.ReadObject<Dictionary<TKey, TValue>>(names_dictionary, typeof(Dictionary<TKey, TValue>), factories_dictionary);
    }
}