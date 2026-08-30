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
using System.Collections;
using System.Collections.Generic;

namespace Wjybxx.Dson.Ext
{
/// <summary>
/// 单值迭代器
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SingleValueEnumerator<T> : IEnumerator<T>
{
#nullable disable
    private T _value;
    private bool _moved;

    public SingleValueEnumerator(T value) {
        _value = value;
    }

    public T Current => _moved ? _value : throw new InvalidOperationException();
    object IEnumerator.Current => Current;

    public bool MoveNext() {
        return !_moved && (_moved = true); // 仅允许移动一次
    }

    public void Reset() {
        _moved = false;
    }

    public void Dispose() {
    }
}
}