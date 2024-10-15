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

using System;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 顺序解码没有额外的开销，但数据兼容性会变差。
/// 如果觉得<see cref="BufferedDsonObjectReader"/>的开销有点大，可以选择顺序解码模式
/// </summary>
public class DefaultDsonObjectReader : AbstractDsonObjectReader
{
    public DefaultDsonObjectReader(IDsonConverter converter, IDsonReader<string> reader)
        : base(converter, reader) {
    }

    public override bool ReadName(string? name) {
        IDsonReader<string> reader = this.reader;
        // array
        if (reader.ContextType.IsArrayLike()) {
            if (reader.IsAtValue) {
                return true;
            }
            if (reader.IsAtType) {
                return reader.ReadDsonType() != DsonType.EndOfObject;
            }
            return reader.CurrentDsonType != DsonType.EndOfObject;
        }
        // object
        if (reader.IsAtValue) {
            return name == null || reader.CurrentName == name;
        }
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (reader.IsAtType) {
            if (reader.ReadDsonType() == DsonType.EndOfObject) {
                return false;
            }
        } else {
            if (reader.CurrentDsonType == DsonType.EndOfObject) {
                return false;
            }
        }
        reader.ReadName(name);
        return true;
    }
}
}