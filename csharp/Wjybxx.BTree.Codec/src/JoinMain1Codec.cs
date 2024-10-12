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
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTreeCodec.Codecs
{
[Generated("Wjybxx.Dson.Apt.CodecProcessor")]
public sealed class JoinMain1Codec<T> : AbstractDsonCodec<JoinMain<T>> where T : class
{
    public override Type GetEncoderType() => typeof(JoinMain<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref JoinMain<T> inst) {
    }

    protected override JoinMain<T> NewInstance(IDsonObjectReader reader) {
        return JoinMain<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinMain<T> inst) {
    }
}
}