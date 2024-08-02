/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dsoncodec;

import cn.wjybxx.dson.*;

import java.util.*;

/**
 * 先将输入流转换为{@link DsonObject}再进行解码，以支持用户随机读。
 *
 * @author wjybxx
 * date - 2023/4/23
 */
final class BufferedDsonObjectReader extends AbstractObjectReader implements DsonObjectReader {

    public BufferedDsonObjectReader(DsonConverter converter, DsonCollectionReader reader) {
        super(converter, reader);
    }

    @Override
    public boolean readName(String name) {
        DsonReader reader = this.reader;
        if (reader.getContextType().isArrayLike()) {
            if (reader.isAtValue()) {
                return true;
            }
            if (reader.isAtType()) {
                return reader.readDsonType() != DsonType.END_OF_OBJECT;
            }
            return reader.getCurrentDsonType() != DsonType.END_OF_OBJECT;
        }
        if (reader.isAtValue()) {
            if (reader.getCurrentName().equals(name)) {
                return true;
            }
            reader.skipValue();
        }
        Objects.requireNonNull(name, "name");
        if (reader.isAtType()) {
            // 用户尚未调用readDsonType，可指定下一个key的值
            KeyIterator keyItr = (KeyIterator) reader.attachment();
            if (keyItr.keySet.contains(name)) {
                keyItr.setNext(name);
                reader.readDsonType();
                reader.readName();
                return true;
            }
            return false;
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            reader.readName(name);
            return true;
        }
    }

    @Override
    public void readStartObject() {
        super.readStartObject();

        DsonCollectionReader reader = (DsonCollectionReader) this.reader;
        ArrayDeque<String> keyQueue = converter.options().keySetPool.acquire();
        KeyIterator keyItr = new KeyIterator(reader.getkeySet(), keyQueue);
        reader.setKeyItr(keyItr, DsonNull.NULL);
        reader.attach(keyItr);
    }

    @Override
    public void readEndObject() {
        // 需要在readEndObject之前保存下来
        KeyIterator keyItr = (KeyIterator) reader.attach(null);
        super.readEndObject();

        converter.options().keySetPool.release(keyItr.keyQueue);
        keyItr.keyQueue = null;
    }

    /**
     * 我将keyQueue由{@link LinkedHashSet}替换为{@link ArrayDeque}，基于这样的一种假设：
     * 大多数情况下，我们都是按照写入的顺序读取，因此使用{@link ArrayDeque}并不会造成太大的负面影响。
     */
    private static class KeyIterator implements Iterator<String> {

        Set<String> keySet;
        ArrayDeque<String> keyQueue;

        public KeyIterator(Set<String> keySet, ArrayDeque<String> keyQueue) {
            this.keySet = keySet;
            this.keyQueue = keyQueue;
            keyQueue.addAll(keySet);
        }

        public void setNext(String nextName) {
            Objects.requireNonNull(nextName);
            if (Objects.equals(keyQueue.peekFirst(), nextName)) {
                return;
            }
            keyQueue.removeFirstOccurrence(nextName);
            keyQueue.addFirst(nextName);
        }

        @Override
        public boolean hasNext() {
            return !keyQueue.isEmpty();
        }

        @Override
        public String next() {
            return keyQueue.removeFirst();
        }
    }
}