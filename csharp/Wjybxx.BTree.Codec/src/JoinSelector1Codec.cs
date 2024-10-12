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
public sealed class JoinSelector1Codec<T> : AbstractDsonCodec<JoinSelector<T>> where T : class
{
    public override Type GetEncoderType() => typeof(JoinSelector<T>);

    protected override void WriteFields(IDsonObjectWriter writer, ref JoinSelector<T> inst) {
    }

    protected override JoinSelector<T> NewInstance(IDsonObjectReader reader) {
        return JoinSelector<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinSelector<T> inst) {
    }
}
}