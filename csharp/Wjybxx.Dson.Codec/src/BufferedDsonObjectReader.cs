#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 先将输入流转换为<see cref="DsonObject{TK}"/>再进行解码，以支持用户随机读。
/// </summary>
internal class BufferedDsonObjectReader : AbstractDsonObjectReader
{
    public BufferedDsonObjectReader(IDsonConverter converter, DsonCollectionReader<string> reader)
        : base(converter, reader) {
    }

    public override bool ReadName(string? name) {
        IDsonReader<string> reader = this.reader;
        // array
        if (reader.ContextType.IsArrayLike()) {
            if (reader.IsAtValue) {
                return true;
            }
            if (reader.IsAtType) {
                return reader.ReadDsonType() != DsonType.EndOfObject;
            }
            return reader.CurrentDsonType != DsonType.EndOfObject;
        }
        // object
        if (reader.IsAtValue) {
            if (name == null || reader.CurrentName == name) {
                return true;
            }
            reader.SkipValue();
        }
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (reader.IsAtType) {
            // 用户尚未调用readDsonType，可指定下一个key的值
            Context context = (Context)reader.Attachment();
            if (context.Contains(name)) {
                context.SetNext(name);
                reader.ReadDsonType();
                reader.ReadName();
                return true;
            }
            return false;
        } else {
            if (reader.CurrentDsonType == DsonType.EndOfObject) {
                return false;
            }
            return name == reader.ReadName(); // 不抛出异常
        }
    }

    public override int ReadStartObject() {
        DsonCollectionReader<string> reader = (DsonCollectionReader<string>)this.reader;
        if (!reader.IsWaitingStart()) { // 尚未读取header
            ReadHeader(DsonType.Object, out string? _, out int _);
        } else {
            reader.ReadStartObject();
        }
        Context context = contextPool.Acquire();
        context.SetKeySet(reader.Keys());
        reader.SetKeyItr(context, DsonNull.NULL);
        reader.Attach(context);
        return reader.Count;
    }

    public override void ReadEndObject() {
        // 需要在readEndObject之前保存下来
        Context context = (Context)reader.Attachment();
        base.ReadEndObject();

        contextPool.Release(context);
    }

    public override int ReadStartArray() {
        DsonCollectionReader<string> reader = (DsonCollectionReader<string>)this.reader;
        if (!reader.IsWaitingStart()) { // 尚未读取header
            ReadHeader(DsonType.Array, out string? _, out int _);
        } else {
            reader.ReadStartArray();
        }
        return reader.Count;
    }

    /// <summary>
    /// <see cref="LinkedHashSet{T}"/>还是由于<see cref="IDeque{T}"/>，
    /// 虽然多数情况下我们都是按照写入的顺序读取，但当Key不存在的时候，Deque删除元素的效率很差。
    /// 考虑到这块尚不稳定，因此不开放给用户设置。
    /// </summary>
    private static readonly ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<Context>(
        () => new Context(), context => context.Dispose(), 256);

#nullable disable
    private class Context : ISequentialEnumerator<string>
    {
        private readonly LinkedHashSet<string> keyQueue = new LinkedHashSet<string>();
        private ICollection<string> _keySet;
        private string? _current;

        public void SetKeySet(ICollection<string> keySet) {
            this._keySet = keySet;
            foreach (string name in this._keySet) {
                keyQueue.Add(name);
            }
        }

        public void SetNext(string nextName) {
            if (nextName == null) throw new ArgumentNullException(nameof(nextName));
            if (keyQueue.TryPeekFirst(out string name) && name == nextName) {
                return;
            }
            keyQueue.AddFirst(nextName);
        }

        public bool Contains(string name) => _keySet.Contains(name);

        public bool HasNext() {
            return !keyQueue.IsEmpty;
        }

        public bool MoveNext() {
            return keyQueue.TryRemoveFirst(out _current);
        }

        public void Reset() {
        }

        public void Dispose() {
            keyQueue.Clear();
            _keySet = null!;
            _current = null!;
        }

        public string? Current => _current;
        object? IEnumerator.Current => Current;
    }
}
}