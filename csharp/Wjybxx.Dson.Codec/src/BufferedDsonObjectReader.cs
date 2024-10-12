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
        if (reader.ContextType.IsArrayLike()) {
            if (reader.IsAtValue) {
                return true;
            }
            if (reader.IsAtType) {
                return reader.ReadDsonType() != DsonType.EndOfObject;
            }
            return reader.CurrentDsonType != DsonType.EndOfObject;
        }

        if (name == null) throw new ArgumentNullException(nameof(name));
        if (reader.IsAtValue) {
            if (reader.CurrentName == name) {
                return true;
            }
            reader.SkipValue();
        }
        if (reader.IsAtType) {
            // 用户尚未调用readDsonType，可指定下一个key的值
            KeyIterator keyItr = (KeyIterator)reader.Attachment();
            if (keyItr.keySet.Contains(name)) {
                keyItr.SetNext(name);
                reader.ReadDsonType();
                reader.ReadName();
                return true;
            }
            return false;
        } else {
            if (reader.CurrentDsonType == DsonType.EndOfObject) {
                return false;
            }
            reader.ReadName(name);
            return true;
        }
    }

    public override void ReadStartObject() {
        base.ReadStartObject();

        DsonCollectionReader<string> reader = (DsonCollectionReader<string>)this.reader;
        MultiChunkDeque<string> keyQueue = converter.Options.keySetPool.Acquire();
        KeyIterator keyItr = new KeyIterator(reader.Keys(), keyQueue);
        reader.SetKeyItr(keyItr, DsonNull.NULL);
        reader.Attach(keyItr);
    }

    public override void ReadEndObject() {
        // 需要在readEndObject之前保存下来
        KeyIterator ketItr = (KeyIterator)reader.Attachment();
        base.ReadEndObject();

        converter.Options.keySetPool.Release(ketItr.keyQueue);
        ketItr.keyQueue = null!;
    }

    /// <summary>
    /// 我将keyQueue由<see cref="LinkedHashSet{T}"/>替换为<see cref="IDeque{T}"/>，基于这样的一种假设：
    /// 大多数情况下，我们都是按照写入的顺序读取，因此使用<see cref="IDeque{T}"/>并不会造成太大的负面影响，
    /// 而且C#使用的是<see cref="MultiChunkDeque{T}"/>，影响更小。
    /// </summary>
    private class KeyIterator : ISequentialEnumerator<string>
    {
        internal readonly ICollection<string> keySet;
        internal MultiChunkDeque<string> keyQueue;
        private string? _current;

        public KeyIterator(ICollection<string> keySet, MultiChunkDeque<string> keyQueue) {
            this.keySet = keySet;
            this.keyQueue = keyQueue;

            foreach (string name in this.keySet) {
                keyQueue.Enqueue(name);
            }
        }

        public void SetNext(string nextName) {
            if (nextName == null) throw new ArgumentNullException(nameof(nextName));
            if (keyQueue.TryPeekFirst(out string name) && name == nextName) {
                return;
            }
            keyQueue.Remove(nextName);
            keyQueue.AddFirst(nextName);
        }

        public bool HasNext() {
            return !keyQueue.IsEmpty;
        }

        public bool MoveNext() {
            return keyQueue.TryRemoveFirst(out _current);
        }

        public void Reset() {
        }

        public void Dispose() {
        }

        public string Current => _current;
        object IEnumerator.Current => Current;
    }
}
}