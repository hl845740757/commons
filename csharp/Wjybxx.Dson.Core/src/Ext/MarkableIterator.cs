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
using System.Collections;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Dson.Ext
{
/// <summary>
/// C#和Java的迭代器差异较大；虽然写Java的时候更多，但不能说C#的迭代器有问题。
/// 对于单线程数据结构，先测试是否有数据，再Move体验更好；而对于并发数据结构，先Move再获取数据则更安全。
/// </summary>
/// <typeparam name="T">元素的类型</typeparam>
public struct MarkableIterator<T> : ISequentialEnumerator<T>
{
#nullable disable
    private IEnumerator<T> _baseIterator;
    private bool _marking;

    private readonly List<T> _buffer;
    /** buffer当前value的索引 */
    private int _bufferIndex;
    /** buffer起始偏移 -- 使用双指针法避免删除队首导致的频繁拷贝 */
    private int _bufferOffset;

    private T _current;
    private T _markedValue;
#nullable enable

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseIterator">外部迭代器</param>
    /// <param name="buffer">buffer，方便外部池化</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MarkableIterator(IEnumerator<T> baseIterator, List<T>? buffer = null) {
        if (buffer == null) {
            buffer = new List<T>();
        } else if (buffer.Count > 0) {
            throw new ArgumentException("buffer is not empty");
        }
        this._baseIterator = baseIterator ?? throw new ArgumentNullException(nameof(baseIterator));
        this._marking = false;

        this._buffer = buffer;
        this._bufferIndex = -1; // c#移动后更新索引
        this._bufferOffset = 0;

        _current = default;
        _markedValue = default;
    }

    /// <summary>
    /// 当前是否是Null实例（默认实例）
    /// </summary>
    public bool IsNull => _buffer == null; // 测试readonly属性

    /// <summary>
    /// 迭代器是否处于干净状态
    /// 1.返回true表示可替换外部迭代器<see cref="SetBaseIterator"/>
    /// 2.<see cref="Dispose"/>后一定为true，用于复用对象
    /// </summary>
    /// <returns></returns>
    public bool IsClean() => _buffer != null && _buffer.IsEmpty();

    public void SetBaseIterator(IEnumerator<T> baseIterator) {
        if (baseIterator == null) throw new ArgumentNullException(nameof(baseIterator));
        if (!IsClean()) {
            throw new InvalidOperationException();
        }
        _baseIterator = baseIterator;
    }

    /// <summary>
    /// 当前是否处于标记中
    /// </summary>
    public bool IsMarking => _marking;

    /// <summary>
    /// 标记位置
    /// </summary>
    public void Mark() {
        if (_marking) throw new InvalidOperationException();
        _marking = true;
        _markedValue = _current;
    }

    /// <summary>
    /// 倒回到Mark位置，不清理Mark状态
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Rewind() {
        if (!_marking) throw new InvalidOperationException();
        _bufferIndex = _bufferOffset - 1; // 指向未开始位置
        _current = _markedValue; // 指向前一个值
    }

    /// <summary>
    /// 倒回到Mark位置，并清理mark状态
    /// </summary>
    public void Reset() {
        if (!_marking) throw new InvalidOperationException();
        _marking = false;
        _bufferIndex = _bufferOffset - 1; // 指向未开始位置
        _current = _markedValue; // 指向前一个值
    }

    /// <summary>
    /// 当前是否有缓存的数据
    /// </summary>
    /// <returns></returns>
    public bool HasBuffer() {
        return _bufferIndex + 1 < _buffer.Count;
    }

    /// <summary>
    /// 测试是否有下一个元素
    /// </summary>
    /// <returns></returns>
    public bool HasNext() {
        // 记录
        T? prev = _current;
        bool marking = _marking;
        if (!marking) {
            _marking = true;
        }
        bool hasNext = MoveNext();
        if (hasNext) {
            _bufferIndex--; // 索引虽然减了，但数据保存在了Buffer中
        }
        // 还原
        _current = prev;
        _marking = marking;
        return hasNext;
    }

    /// <summary>
    /// 获取下一个元素
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public T Next() {
        if (MoveNext()) {
            return _current;
        }
        throw new InvalidOperationException();
    }

    /// <summary>
    /// 获取下一个元素
    /// </summary>
    /// <param name="val"></param>
    /// <returns>是否包含下一个元素</returns>
    public bool Next(out T? val) {
        if (MoveNext()) {
            val = _current;
            return true;
        }
        val = default;
        return false;
    }

    /// <summary>
    /// 对剩下的元素执行给定的操作
    /// </summary>
    /// <param name="action"></param>
    public void ForEachRemaining(Action<T> action) {
        while (MoveNext()) {
            action.Invoke(_current);
        }
    }

    object IEnumerator.Current => _current;

    public T Current => _current;

    /// <inheritdoc />
    public bool MoveNext() {
        List<T> buffer = this._buffer;
        if (_bufferIndex + 1 < buffer.Count) {
            _current = buffer[++_bufferIndex];
            if (_marking) {
                return true;
            }
            buffer[_bufferOffset++] = default; // 使用双指针法避免频繁的拷贝
            if (_bufferOffset == buffer.Count || _bufferOffset >= 8) {
                if (_bufferOffset == buffer.Count) {
                    buffer.Clear();
                } else {
                    buffer.RemoveRange(0, _bufferOffset);
                }
                _bufferIndex = -1;
                _bufferOffset = 0;
            }
            return true;
        } else {
            if (_baseIterator.MoveNext()) {
                _current = _baseIterator.Current;
                if (_marking) { // 所有读取的值要保存下来
                    buffer.Add(_current);
                    _bufferIndex++;
                }
                return true;
            }
            _current = default;
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        // 如果想调用baseIterator的Dispose，最好增加开关
        this._baseIterator = null;
        this._marking = false;

        this._buffer.Clear();
        this._bufferIndex = -1;
        this._bufferOffset = 0;

        this._current = default;
        this._markedValue = default;
    }
}
}