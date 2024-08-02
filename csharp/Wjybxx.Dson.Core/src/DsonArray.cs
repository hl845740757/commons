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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson数组
/// </summary>
/// <typeparam name="TK">header的key类型</typeparam>
public class DsonArray<TK> : AbstractDsonArray
{
    private readonly DsonHeader<TK> _header;

    public DsonArray()
        : this(new List<DsonValue>(), new DsonHeader<TK>()) {
    }

    public DsonArray(int capacity)
        : this(new List<DsonValue>(capacity), new DsonHeader<TK>()) {
    }

    public DsonArray(DsonArray<TK> src) // 需要拷贝
        : this(new List<DsonValue>(src._values), new DsonHeader<TK>(src._header)) {
    }

    private DsonArray(IList<DsonValue> values, DsonHeader<TK> header)
        : base(values) {
        _header = header;
    }

    public override DsonType DsonType => DsonType.Array;
    public DsonHeader<TK> Header => _header;

    public new DsonArray<TK> Append(DsonValue item) {
        base.Append(item);
        return this;
    }

    /// <summary>
    /// 注意：对切片进行的修改是独立的，不影响原始的数据
    /// </summary>
    /// <param name="skip"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DsonArray<TK> Slice(int skip) {
        if (skip < 0) {
            throw new ArgumentException("skip cant be negative");
        }
        if (skip >= _values.Count) {
            return new DsonArray<TK>(0);
        }
        List<DsonValue> slice = _values.Skip(skip).ToList();
        return new DsonArray<TK>(slice, new DsonHeader<TK>());
    }

    public DsonArray<TK> Slice(int skip, int count) {
        if (skip < 0) {
            throw new ArgumentException("skip cant be negative");
        }
        if (count < 0) {
            throw new ArgumentException("count cant be negative");
        }
        if (skip >= _values.Count) {
            return new DsonArray<TK>(0);
        }
        List<DsonValue> slice;
        if (_values.Count <= skip + count) {
            slice = _values.Skip(skip).ToList();
        } else {
            slice = _values.Skip(skip).Take(count).ToList();
        }
        return new DsonArray<TK>(slice, new DsonHeader<TK>());
    }

    public override string ToString() {
        return $"{base.ToString()}, {nameof(Header)}: {Header}";
    }
}
}