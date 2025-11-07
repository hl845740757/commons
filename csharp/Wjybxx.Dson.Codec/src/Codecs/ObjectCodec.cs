#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using System;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 用于支持空对象
/// </summary>
public class ObjectCodec : IDsonCodec<object>
{
    public void WriteObject(IDsonObjectWriter writer, object inst, Type declaredType, SerializeFeatures features) {

    }

    public object ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory = null) {
        return new object();
    }
}
}