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

namespace Wjybxx.Dson.Tests.Codec;

[DsonSerializable(Singleton = "Inst")]
public class SingletonTest
{
    public readonly int age;
    public readonly string name;

    private SingletonTest(int age, string name) {
        this.age = age;
        this.name = name;
    }

    public static SingletonTest Inst { get; } = new SingletonTest(30, "wjybxx");
}