#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.Dson.Codec;
using System.Collections.Generic;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.Dson;

namespace Wjybxx.Dson.Tests.Apt;

[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class BeanExampleCodec : AbstractDsonCodec<BeanExample>
{
  public const string names_name = "_name";
  public const string names_age = "age";
  public const string names_Type = "Type";
  public const string names_hashSet = "hashSet";
  public const string names_hashSet2 = "hashSet2";
  public static readonly Func<HashSet<string>> factories_hashSet = () => new HashSet<string>();
  public static readonly Func<HashSet<string>> factories_hashSet2 = () => new HashSet<string>();

  public override Type GetEncoderClass() => typeof(BeanExample);

  protected override void BeforeEncode(IDsonObjectWriter writer, ref BeanExample inst, Type declaredType, ObjectStyle style) {
    inst.BeforeEncode(writer.Options);
  }

  protected override void WriteFields(IDsonObjectWriter writer, ref BeanExample inst, Type declaredType, ObjectStyle style) {
    inst.WriteObject(writer);
    writer.WriteString(names_name, inst.Name, StringStyle.AutoQuote);
    writer.WriteInt(names_age, inst.Age, WireType.Uint, NumberStyles.Simple);
    inst.WriteType(writer, names_Type);
    writer.WriteObject(names_hashSet, inst.hashSet, typeof(HashSet<string>), null);
    writer.WriteObject(names_hashSet2, inst.hashSet2, typeof(ISet<string>), null);
  }

  protected override BeanExample NewInstance(IDsonObjectReader reader, Type declaredType) {
    return BeanExample.NewInstance(reader);
  }

  protected override void ReadFields(IDsonObjectReader reader, ref BeanExample inst, Type declaredType) {
    inst.ReadObject(reader);
    inst.Name = reader.ReadString(names_name);
    inst.Age = reader.ReadInt(names_age);
    inst.ReadType(reader, names_Type);
    inst.hashSet = reader.ReadObject<HashSet<string>>(names_hashSet, typeof(HashSet<string>), factories_hashSet);
    inst.hashSet2 = reader.ReadObject<HashSet<string>>(names_hashSet2, typeof(ISet<string>), factories_hashSet2);
  }

  protected override void AfterDecode(IDsonObjectReader reader, ref BeanExample inst, Type declaredType) {
    inst.AfterDecode(reader.Options);
  }
}
