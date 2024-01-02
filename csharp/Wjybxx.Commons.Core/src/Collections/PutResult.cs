#region LICENSE

//  Copyright 2023 wjybxx
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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Collections;

/// <summary>
/// 用于表示字典Put操作的结果，避免过多的参数
/// </summary>
/// <typeparam name="TValue"></typeparam>
public readonly struct PutResult<TValue>
{
    private readonly bool _isInsert;
    private readonly TValue _prevValue;

    public PutResult(bool isInsert, TValue prevValue) {
        _isInsert = isInsert;
        _prevValue = prevValue;
    }

    /// <summary>
    /// 本次操作是否是insert操作（即没有旧值）
    /// </summary>
    public bool IsInsert => _isInsert;

    /// <summary>
    /// Key关联的旧值
    /// （Insert下是关联的默认值）
    /// </summary>
    public TValue PrevValue => _prevValue;
}