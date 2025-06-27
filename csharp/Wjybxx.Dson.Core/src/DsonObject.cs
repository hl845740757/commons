#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson
{
/// <summary>
/// DsonObject
/// </summary>
public class DsonObject<TK> : AbstractDsonObject<TK>
{
    // TODO 如何降低Header的开销
    private readonly DsonHeader<TK> _header = new DsonHeader<TK>();

    public DsonObject(int capacity = 0) 
        : base(capacity) {
    }

    public DsonObject(DsonObject<TK> src) // 需要拷贝
        : base(src.Count) {
        if (src._header.Count > 0) {
            _header.PutAll(src._header);
        }
    }

    public override DsonType DsonType => DsonType.Object;
    public DsonHeader<TK> Header => _header;

    public new DsonObject<TK> Append(TK key, DsonValue value) {
        base.Append(key, value);
        return this;
    }

    public override string ToString() {
        return $"{base.ToString()}, {nameof(Header)}: {Header}";
    }
}
}