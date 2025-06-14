#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;
using Wjybxx.BTree;
using System.Collections.Generic;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class FixedSwitchCodec<T> : AbstractDsonCodec<FixedSwitch<T>> where T : class 
{
    public const string names_guard = "guard";
    public const string names_flags = "flags";
    public const string names_children = "children";
    public const string names_handler = "handler";
    public const string names_branch1 = "branch1";
    public const string names_branch2 = "branch2";
    public const string names_branch3 = "branch3";
    public const string names_branch4 = "branch4";
    public const string names_branch5 = "branch5";

    public override Type GetEncoderType() => typeof(FixedSwitch<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in FixedSwitch<T> inst) {
        writer.WriteObject(names_guard, inst.Guard, null);
        writer.WriteInt(names_flags, inst.Flags, NumberStyles.Simple);
        writer.WriteObject(names_children, inst.Children, null);
        writer.WriteObject(names_handler, inst.Handler, null);
        writer.WriteObject(names_branch1, inst.Branch1, null);
        writer.WriteObject(names_branch2, inst.Branch2, null);
        writer.WriteObject(names_branch3, inst.Branch3, null);
        writer.WriteObject(names_branch4, inst.Branch4, null);
        writer.WriteObject(names_branch5, inst.Branch5, null);
    }

    protected override FixedSwitch<T> NewInstance(IDsonObjectReader reader) {
        return new FixedSwitch<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref FixedSwitch<T> inst) {
        if (reader.ReadName(names_guard)) inst.Guard = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_flags)) inst.Flags = reader.ReadInt(null);
        if (reader.ReadName(names_children)) inst.Children = reader.ReadObject<List<Task<T>>>(null, null);
        if (reader.ReadName(names_handler)) inst.Handler = reader.ReadObject<ISwitchHandler<T>>(null, null);
        if (reader.ReadName(names_branch1)) inst.Branch1 = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_branch2)) inst.Branch2 = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_branch3)) inst.Branch3 = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_branch4)) inst.Branch4 = reader.ReadObject<Task<T>>(null, null);
        if (reader.ReadName(names_branch5)) inst.Branch5 = reader.ReadObject<Task<T>>(null, null);
    }
}
}
