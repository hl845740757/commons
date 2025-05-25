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

using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Tests.Apt;

[DsonCodecLinkerGroup]
public class LinkerGroupExample
{
#nullable disable
    public ThirdPartyBean thirdPartyBean;
    // 测试泛型类
    public GenericBean<int> g;
    // 测试无法加载的程序集 -- 警告
    public ExtInt32 _extInt32;
}