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

using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Tests.Apt;

/// <summary>
/// 测试内部类能否正常生成
/// </summary>
public static class OuterClass
{
    [DsonSerializable]
    public class NestedBean
    {
        private int age;
        private string? name;

        public int Age {
            get => age;
            set => age = value;
        }
        public string? Name {
            get => name;
            set => name = value;
        }
    }
}