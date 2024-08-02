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
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// 简单的Dson对象仓库实现 -- 提供简单的引用解析功能。
/// </summary>
public class DsonRepository
{
    private readonly Dictionary<string, DsonValue> _indexMap = new();
    private readonly DsonArray<string> _collection;

    public DsonRepository() {
        _collection = new();
    }

    public DsonRepository(DsonArray<string> container) {
        _collection = container ?? throw new ArgumentNullException(nameof(container));
        foreach (var dsonValue in container) {
            string localId = Dsons.GetLocalId(dsonValue);
            if (localId != null) {
                _indexMap[localId] = dsonValue;
            }
        }
    }

    /** 获取索引信息 -- 勿修改返回的对象 */
    public Dictionary<string, DsonValue> IndexMap => _indexMap;

    /** 获取顶层集合 -- 勿修改返回的对象 */
    public DsonArray<string> Collection => _collection;

    /** 获取顶层元素数量 */
    public int Count => _collection.Count;

    /** 获取指定下标的元素 */
    public DsonValue this[int index] => _collection[index];

    /// <summary>
    /// 添加元素到仓库
    /// </summary>
    /// <param name="value">必须是顶层元素类型</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public DsonRepository Add(DsonValue value) {
        if (!value.DsonType.IsContainerOrHeader()) {
            throw new ArgumentException();
        }
        _collection.Add(value);

        string localId = Dsons.GetLocalId(value);
        if (localId != null) {
            if (_indexMap.Remove(localId, out DsonValue exist)) {
                CollectionUtil.RemoveRef(_collection, exist);
            }
            _indexMap[localId] = value;
        }
        return this;
    }

    /// <summary>
    /// 删除指定下标的元素
    /// </summary>
    /// <param name="idx">元素下标</param>
    /// <returns>删除的元素</returns>
    public DsonValue RemoveAt(int idx) {
        DsonValue dsonValue = _collection[idx];
        _collection.RemoveAt(idx); // 居然没返回值...

        string localId = Dsons.GetLocalId(dsonValue);
        if (localId != null) {
            _indexMap.Remove(localId, out DsonValue _);
        }
        return dsonValue;
    }

    /// <summary>
    /// 删除指定元素
    /// 注意：通过引用相等判断是否存在。
    /// </summary>
    /// <param name="dsonValue">要删除的元素</param>
    /// <returns>删除成功则返回true，否则返回false</returns>
    public bool Remove(DsonValue dsonValue) {
        int idx = CollectionUtil.IndexOfRef(_collection, dsonValue);
        if (idx >= 0) {
            RemoveAt(idx);
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// 通过对象的本地id删除元素
    /// </summary>
    /// <param name="localId">要删除的元素id</param>
    /// <returns>被删除的元素，不存在时返回null</returns>
    /// <exception cref="ArgumentNullException">id为null</exception>
    public DsonValue? RemoveById(string localId) {
        if (localId == null) throw new ArgumentNullException(nameof(localId));
        if (_indexMap.Remove(localId, out DsonValue exist)) {
            CollectionUtil.RemoveRef(_collection, exist);
        }
        return exist;
    }

    /// <summary>
    /// 通过localId查找元素
    /// </summary>
    /// <param name="localId">要查找的元素的id</param>
    /// <returns>id关联的元素，不存在时返回null</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public DsonValue? Find(string localId) {
        if (localId == null) throw new ArgumentNullException(nameof(localId));
        _indexMap.TryGetValue(localId, out DsonValue exist);
        return exist;
    }

    /// <summary>
    /// 解析仓库中对象之间的引用
    /// </summary>
    /// <returns></returns>
    public DsonRepository ResolveReference() {
        foreach (DsonValue dsonValue in _collection) {
            ResolveReference(dsonValue);
        }
        return this;
    }

    private void ResolveReference(DsonValue dsonValue) {
        if (dsonValue is AbstractDsonObject<string>
            dsonObject) { // 支持header...
            foreach (KeyValuePair<string, DsonValue> entry in dsonObject) {
                DsonValue value = entry.Value;
                if (value.DsonType == DsonType.Pointer) {
                    ObjectPtr objectPtr = value.AsPointer();
                    if (_indexMap.TryGetValue(objectPtr.LocalId, out DsonValue targetObj)) {
                        dsonObject[entry.Key] = targetObj; // 迭代时覆盖值是安全的
                    }
                } else if (value.DsonType.IsContainer()) {
                    ResolveReference(value);
                }
            }
        } else if (dsonValue is DsonArray<string> dsonArray) {
            for (int i = 0; i < dsonArray.Count; i++) {
                DsonValue value = dsonArray[i];
                if (value.DsonType == DsonType.Pointer) {
                    ObjectPtr objectPtr = value.AsPointer();
                    if (_indexMap.TryGetValue(objectPtr.LocalId, out DsonValue targetObj)) {
                        dsonArray[i] = targetObj;
                    }
                } else if (value.DsonType.IsContainer()) {
                    ResolveReference(value);
                }
            }
        }
    }

    /// <summary>
    /// 将Reader中的所有元素读取到仓库
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="resolveRef">是否解析引用</param>
    /// <returns></returns>
    public static DsonRepository FromDson(IDsonReader<string> reader, bool resolveRef = false) {
        using (reader) {
            DsonRepository repository = new DsonRepository(Dsons.ReadCollection(reader));
            if (resolveRef) {
                repository.ResolveReference();
            }
            return repository;
        }
    }

    // 解析引用后可能导致循环，因此equals等不实现
    public override string ToString() {
        // 解析引用后可能导致死循环，因此不输出
        return "DsonRepository:" + base.ToString();
    }
}
}