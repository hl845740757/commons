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

namespace Wjybxx.BTree.Codec;

/// <summary>
/// 测试继承第三方程序集时，反序列化数据是否正确
/// </summary>
[DsonSerializable]
public class TaskGetName : LeafTask<object>
{
    private string? _name;
    [DsonIgnore] private string? _cache;

    protected override void Execute() {
        throw new System.NotImplementedException();
    }

    protected override void OnEventImpl(object eventObj) {
        throw new System.NotImplementedException();
    }

    public string? Name {
        get => _name;
        set => _name = value;
    }

    public string? Cache {
        get => _cache;
        set => _cache = value;
    }
}