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

package cn.wjybxx.dson;

import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.ext.MarkableIterator;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.types.*;

import java.util.*;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public final class DsonCollectionReader extends AbstractDsonReader {

    private String nextName;
    private DsonValue nextValue;

    public DsonCollectionReader(DsonReaderSettings settings, DsonArray<String> dsonArray) {
        super(settings);
        Objects.requireNonNull(dsonArray);

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        context.header = !dsonArray.getHeader().isEmpty() ? dsonArray.getHeader() : null;
        context.arrayIterator.setBaseIterator(dsonArray.iterator());
        setContext(context);
    }

    /**
     * 设置key的迭代顺序
     * 注意：这期间不能触发{@link #peekDsonType()}等可能导致mark的操作，
     * mark操作会导致key缓存到本地，从而使得外部的keyItr无效。
     *
     * @param defValue key不存在时的返回值；可选择{@link DsonNull#UNDEFINE}
     */
    public void setKeyItr(Iterator<String> keyItr, DsonValue defValue) {
        Objects.requireNonNull(keyItr);
        Objects.requireNonNull(defValue);
        Context context = getContext();
        if (context.dsonObject == null) {
            throw DsonIOException.contextError(List.of(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        context.setKeyItr(keyItr, defValue);
    }

    /**
     * 获取当前对象的所有key。
     * 注意：不可修改返回的集合。
     */
    public Set<String> getkeySet() {
        Context context = getContext();
        if (context.dsonObject == null) {
            throw DsonIOException.contextError(List.of(DsonContextType.OBJECT, DsonContextType.HEADER), context.contextType);
        }
        return context.dsonObject.keySet();
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public void close() {
        Context context = getContext();
        setContext(null);
        while (context != null) {
            Context parent = context.getParent();
            contextPool.release(context);
            context = parent;
        }
        nextName = null;
        nextValue = null;
        super.close();
    }

    // region state

    private void pushNextValue(DsonValue nextValue) {
        this.nextValue = Objects.requireNonNull(nextValue);
    }

    private DsonValue popNextValue() {
        DsonValue r = this.nextValue;
        this.nextValue = null;
        return r;
    }

    private void pushNextName(String nextName) {
        this.nextName = Objects.requireNonNull(nextName);
    }

    private String popNextName() {
        String r = this.nextName;
        this.nextName = null;
        return r;
    }

    @Override
    public DsonType readDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        popNextName();
        popNextValue();

        DsonType dsonType;
        if (context.header != null) { // 需要先读取header
            dsonType = DsonType.HEADER;
            pushNextValue(context.header);
            context.header = null;
        } else if (context.contextType.isArrayLike()) {
            DsonValue nextValue = context.nextValue();
            if (nextValue == null) {
                dsonType = DsonType.END_OF_OBJECT;
            } else {
                pushNextValue(nextValue);
                dsonType = nextValue.getDsonType();
            }
        } else {
            Map.Entry<String, DsonValue> nextElement = context.nextElement();
            if (nextElement == null) {
                dsonType = DsonType.END_OF_OBJECT;
            } else {
                pushNextName(nextElement.getKey());
                pushNextValue(nextElement.getValue());
                dsonType = nextElement.getValue().getDsonType();
            }
        }

        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VARINT;
        this.currentName = INVALID_NAME;

        onReadDsonType(context, dsonType);
        return dsonType;
    }

    @Override
    public DsonType peekDsonType() {
        Context context = this.getContext();
        checkReadDsonTypeState(context);

        if (context.header != null) {
            return DsonType.HEADER;
        }
        if (!context.hasNext()) {
            return DsonType.END_OF_OBJECT;
        }
        if (context.contextType.isArrayLike()) {
            context.markItr();
            DsonValue nextValue = context.nextValue();
            context.resetItr();
            return nextValue.getDsonType();
        } else {
            context.markItr();
            Map.Entry<String, DsonValue> nextElement = context.nextElement();
            context.resetItr();
            return nextElement.getValue().getDsonType();
        }
    }

    @Override
    protected void doReadName() {
        currentName = popNextName();
    }

    // endregion

    // region 简单值

    @Override
    protected int doReadInt32() {
        return popNextValue().asInt32(); // as顺带null检查
    }

    @Override
    protected long doReadInt64() {
        return popNextValue().asInt64();
    }

    @Override
    protected float doReadFloat() {
        return popNextValue().asFloat();
    }

    @Override
    protected double doReadDouble() {
        return popNextValue().asDouble();
    }

    @Override
    protected boolean doReadBool() {
        return popNextValue().asBool();
    }

    @Override
    protected String doReadString() {
        return popNextValue().asString();
    }

    @Override
    protected void doReadNull() {
        popNextValue();
    }

    @Override
    protected Binary doReadBinary() {
        return popNextValue().asBinary().deepCopy(); // 需要拷贝
    }

    @Override
    protected ObjectPtr doReadPtr() {
        return popNextValue().asPointer();
    }

    @Override
    protected ObjectLitePtr doReadLitePtr() {
        return popNextValue().asLitePointer();
    }

    @Override
    protected ExtDateTime doReadDateTime() {
        return popNextValue().asDateTime();
    }

    @Override
    protected Timestamp doReadTimestamp() {
        return popNextValue().asTimestamp();
    }

    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = newContext(getContext(), contextType, dsonType);
        DsonValue dsonValue = popNextValue();
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            DsonObject<String> dsonObject = dsonValue.asObject();
            newContext.header = !dsonObject.getHeader().isEmpty() ? dsonObject.getHeader() : null;
            newContext.dsonObject = dsonObject;
            newContext.objectIterator.setBaseIterator(dsonObject.entrySet().iterator());
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            DsonArray<String> dsonArray = dsonValue.asArray();
            newContext.header = !dsonArray.getHeader().isEmpty() ? dsonArray.getHeader() : null;
            newContext.arrayIterator.setBaseIterator(dsonArray.iterator());
        } else {
            // header
            DsonHeader<String> header = dsonValue.asHeader();
            newContext.dsonObject = header;
            newContext.objectIterator.setBaseIterator(header.entrySet().iterator());
        }
        newContext.name = currentName;

        this.recursionDepth++;
        setContext(newContext);
    }

    @Override
    protected void doReadEndContainer() {
        Context context = getContext();

        // 恢复上下文
        recoverDsonType(context);
        this.recursionDepth--;
        setContext(context.parent);
        returnContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doSkipName() {
        popNextName();
    }

    @Override
    protected void doSkipValue() {
        popNextValue();
    }

    @Override
    protected void doSkipToEndOfObject() {
        Context context = getContext();
        context.header = null;
        if (context.dsonObject != null) {
            context.objectIterator.close();
            context.objectIterator.setBaseIterator(Collections.emptyIterator());
        } else {
            context.arrayIterator.close();
            context.arrayIterator.setBaseIterator(Collections.emptyIterator());
        }
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        throw new UnsupportedOperationException();
    }

    // endregion

    // region context

    private static final ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<>(Context::new, Context::reset,
            DsonInternals.CONTEXT_POOL_SIZE);

    private static Context newContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = contextPool.acquire();
        context.init(parent, contextType, dsonType);
        return context;
    }

    private static void returnContext(Context context) {
        contextPool.release(context);
    }

    protected static class Context extends AbstractDsonReader.Context
            implements Iterator<Map.Entry<String, DsonValue>> {

        /** 如果不为null，则表示需要先读取header */
        private DsonHeader<String> header;
        /** 如果不为null，则表示读取Header和Object上下文 */
        private AbstractDsonObject<String> dsonObject;
        /** 迭代器和Context一起缓存 */
        private final MarkableIterator<Map.Entry<String, DsonValue>> objectIterator = new MarkableIterator<>(Collections.emptyIterator());
        private final MarkableIterator<DsonValue> arrayIterator = new MarkableIterator<>(Collections.emptyIterator());

        /** 按照外部key迭代 -- 避免再封装一层增加开销 */
        private Iterator<String> keyItr;
        private DsonValue defValue;

        public Context() {
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

        @Override
        public void reset() {
            super.reset();
            header = null;
            dsonObject = null;
            objectIterator.close();
            arrayIterator.close();
            keyItr = null;
            defValue = null;
        }

        /** 该方法重合了迭代器的hasNext，需要兼容 */
        @Override
        public boolean hasNext() {
            if (keyItr != null) {
                return keyItr.hasNext();
            }
            if (dsonObject != null) {
                return objectIterator.hasNext();
            }
            return arrayIterator.hasNext();
        }

        public void markItr() {
            if (dsonObject != null) {
                objectIterator.mark();
            } else {
                arrayIterator.mark();
            }
        }

        public void resetItr() {
            if (dsonObject != null) {
                objectIterator.reset();
            } else {
                arrayIterator.reset();
            }
        }

        public DsonValue nextValue() {
            return arrayIterator.hasNext() ? arrayIterator.next() : null;
        }

        public Map.Entry<String, DsonValue> nextElement() {
            return objectIterator.hasNext() ? objectIterator.next() : null;
        }

        // key-itr

        public void setKeyItr(Iterator<String> keyItr, DsonValue defValue) {
            if (dsonObject == null) throw new IllegalStateException();
            if (objectIterator.isMarking()) throw new IllegalStateException("reader is in marking state");

            this.keyItr = keyItr;
            this.defValue = defValue;
            objectIterator.close();
            objectIterator.setBaseIterator(this);
        }

        @Override
        public Map.Entry<String, DsonValue> next() {
            String key = keyItr.next();
            DsonValue dsonValue = dsonObject.get(key);
            if (dsonValue == null) {
                return Map.entry(key, defValue);
            } else {
                return Map.entry(key, dsonValue);
            }
        }
    }

    // endregion

}