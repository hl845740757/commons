#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System;
using System.Collections.Generic;

namespace Wjybxx.Dson.Tests.Apt
{
  [Generated("Wjybxx.Dson.Apt.CodecProcessor")]
  public sealed class MyDictionary2Codec<TKey, TValue> : AbstractDsonCodec<MyDictionary<TKey, TValue>>
  {
    public const string names_dictionary = "dictionary";

    public override Type GetEncoderType() => typeof(MyDictionary<TKey, TValue>);

    protected override void WriteFields(IDsonObjectWriter writer, ref MyDictionary<TKey, TValue> inst) {
      writer.WriteObject(names_dictionary, inst.dictionary, typeof(Dictionary<TKey, TValue>), null);
    }

    protected override MyDictionary<TKey, TValue> NewInstance(IDsonObjectReader reader) {
      return new MyDictionary<TKey, TValue>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref MyDictionary<TKey, TValue> inst) {
      inst.dictionary = reader.ReadObject<Dictionary<TKey, TValue>>(names_dictionary, typeof(Dictionary<TKey, TValue>), null);
    }
  }
}
