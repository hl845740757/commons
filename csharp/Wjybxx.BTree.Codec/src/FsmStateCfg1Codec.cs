#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM;
using Wjybxx.Dson.Codec;
using System;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class FsmStateCfg1Codec<T> : AbstractDsonCodec<FsmStateCfg<T>> where T : class
{
    public const string names_name = "name";
    public const string names_guid = "guid";
    public const string names_props = "props";

    public override Type GetEncoderType() => typeof(FsmStateCfg<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in FsmStateCfg<T> inst) {
        writer.WriteString(names_name, inst.Name, StringStyle.Auto);
        writer.WriteString(names_guid, inst.Guid, StringStyle.Auto);
        writer.WriteObject(names_props, inst.Props, typeof(object), null);
    }

    protected override FsmStateCfg<T> NewInstance(IDsonObjectReader reader) {
        return new FsmStateCfg<T>();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref FsmStateCfg<T> inst) {
        if (reader.ReadName(names_name)) inst.Name = reader.ReadString(null);
        if (reader.ReadName(names_guid)) inst.Guid = reader.ReadString(null);
        if (reader.ReadName(names_props)) inst.Props = reader.ReadObject<object>(null, typeof(object), null);
    }
}
}