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

import cn.wjybxx.base.TypeInfo;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
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
        // array
        if (reader.getContextType().isArrayLike()) {
            if (reader.isAtValue()) {
                return true;
            }
            if (reader.isAtType()) {
                return reader.readDsonType() != DsonType.END_OF_OBJECT;
            }
            return reader.getCurrentDsonType() != DsonType.END_OF_OBJECT;
        }
        // object
        if (reader.isAtValue()) {
            if (name == null || reader.getCurrentName().equals(name)) {
                return true;
            }
            reader.skipValue();
        }
        Objects.requireNonNull(name, "name");
        if (reader.isAtType()) {
            // 用户尚未调用readDsonType，可指定下一个key的值
            Context context = (Context) reader.attachment();
            if (context.contains(name)) {
                context.setNext(name);
                reader.readDsonType();
                reader.readName();
                return true;
            }
            return false;
        } else {
            if (reader.getCurrentDsonType() == DsonType.END_OF_OBJECT) {
                return false;
            }
            return name.equals(reader.readName()); // 不抛出异常
        }
    }

    @Override
    public int readStartObject() {
        DsonCollectionReader reader = (DsonCollectionReader) this.reader;
        if (!reader.isWaitingStart()) { // 尚未读取header
            readHeader(DsonType.OBJECT);
        } else {
            reader.readStartObject();
        }
        Context context = contextPool.acquire();
        context.setKeySet(reader.getkeySet());
        reader.setKeyItr(context, DsonNull.NULL);
        reader.attach(context);
        return reader.count();
    }

    @Override
    public void readEndObject() {
        // 需要在readEndObject之前保存下来
        Context context = (Context) reader.attach(null);
        super.readEndObject();

        contextPool.release(context);
    }

    @Override
    public int readStartArray() {
        DsonCollectionReader reader = (DsonCollectionReader) this.reader;
        if (!reader.isWaitingStart()) { // 尚未读取header
            readHeader(DsonType.ARRAY);
        } else {
            reader.readStartArray();
        }
        return reader.count();
    }

    @Override
    public void setEncoderType(TypeInfo encoderType) {
        Object attachment = reader.attachment();
        if (attachment instanceof Context context) {
            context.encoderType = encoderType;
        } else {
            reader.attach(encoderType);
        }
    }

    @Override
    public TypeInfo getEncoderType() {
        Object attachment = reader.attachment();
        if (attachment instanceof Context context) {
            return context.encoderType;
        }
        return (TypeInfo) attachment;
    }

    /**
     * {@link LinkedHashSet}还是优于{@link ArrayDeque}，
     * 虽然多数情况下我们都是按照写入的顺序读取，但当Key不存在的时候，Deque删除元素的效率很差。
     * 考虑到这块尚不稳定，因此不开放给用户设置。
     */
    private static final ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<>(
            Context::new, Context::dispose, 256);

    private static class Context implements Iterator<String> {

        final LinkedHashSet<String> keyQueue = new LinkedHashSet<>(16);
        Set<String> keySet;
        TypeInfo encoderType;


        public void setKeySet(Set<String> keySet) {
            this.keySet = keySet;
            this.keyQueue.addAll(keySet);
        }

        public void setNext(String nextName) {
            Objects.requireNonNull(nextName);
            if (keyQueue.size() > 0 && keyQueue.getFirst().equals(nextName)) {
                return;
            }
            keyQueue.addFirst(nextName);
        }

        public boolean contains(String name) {
            return keySet.contains(name);
        }

        @Override
        public boolean hasNext() {
            return !keyQueue.isEmpty();
        }

        @Override
        public String next() {
            return keyQueue.removeFirst();
        }

        public void dispose() {
            keySet = null;
            encoderType = null;
        }
    }
}