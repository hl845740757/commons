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

using System.Numerics;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class Vector3Codec : IDsonCodec<Vector3>
{
    public void WriteObject(IDsonObjectWriter writer, ref Vector3 inst, Type declaredType, ObjectStyle style) {
        writer.WriteFloat("x", inst.X);
        writer.WriteFloat("y", inst.Y);
        writer.WriteFloat("z", inst.Z);
    }

    public Vector3 ReadObject(IDsonObjectReader reader, Func<Vector3>? factory = null) {
        return new Vector3(
            reader.ReadFloat("x"),
            reader.ReadFloat("y"),
            reader.ReadFloat("z"));
    }
}