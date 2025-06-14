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

using Wjybxx.Dson.Tests.Apt;

namespace Wjybxx.Dson.Tests.Codec;

/// <summary>
/// 这是提交给Rider的测试用例
/// </summary>
public class AptSymbolTest
{
    public void Test() {
        // 如果在编译时没有注释该行代码，则Rider会提示无法访问对应的符号
        // 如果在编译时先注释该行代码，就可以解析对应的符号
        Console.WriteLine(ThirdPartyBean2Codec.names_age);
    }
}