#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Tests.Apt;

/// <summary>
/// 测试外部代理
/// </summary>
[DsonCodecLinkerBean(typeof(Vector2), NamespaceAliases = new[]
{
    "Numerics = System.Numerics"
})]
public class Vector2CodecProxy
{
    public static void BeforeEncode(ref Vector2 inst, ConverterOptions options) {
    }

    public static void WriteObject(ref Vector2 inst, IDsonObjectWriter writer) {
    }

    public static void ReadObject(ref Vector2 inst, IDsonObjectReader reader) {
    }

    public static void AfterDecode(ref Vector2 inst, ConverterOptions options) {
    }
}