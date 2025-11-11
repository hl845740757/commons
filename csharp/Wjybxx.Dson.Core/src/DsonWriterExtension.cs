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
using System.Runtime.CompilerServices;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson
{
/// <summary>
/// Reader/Writer的扩展方法
/// </summary>
public static class DsonWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartArray<TName>(this IDsonWriter<TName> writer, TName name,
                                              ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        writer.WriteName(name);
        writer.WriteStartArray(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStartObject<TName>(this IDsonWriter<TName> writer, TName name,
                                               ObjectStyle style = ObjectStyle.Indent) where TName : IEquatable<TName> {
        writer.WriteName(name);
        writer.WriteStartObject(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStartArray<TName>(this IDsonReader<TName> writer, TName name) where TName : IEquatable<TName> {
        writer.ReadName(name);
        writer.ReadStartArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReadStartObject<TName>(this IDsonReader<TName> writer, TName name) where TName : IEquatable<TName> {
        writer.ReadName(name);
        writer.ReadStartObject();
    }
}
}