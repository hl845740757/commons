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
import cn.wjybxx.dson.ext.SingleValueIterator;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.types.*;

import java.util.*;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public final class DsonLiteCollectionReader extends AbstractDsonLiteReader {

    private int nextName = 0; // 0是无效值
    private DsonValue nextValue;
    private boolean singleValue;

    public DsonLiteCollectionReader(DsonReaderSettings settings, DsonArray<Integer> dsonArray) {
        super(settings);

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        context.header = dsonArray.getHeader().size() > 0 ? dsonArray.getHeader() : null;
        context.container = dsonArray;
        context.arrayIterator.setBaseIterator(dsonArray.iterator());
        setContext(context);
    }

    private DsonLiteCollectionReader() {
        super(null);
    }

    public void unsafeInit(DsonReaderSettings settings, DsonValue dsonValue, boolean singleValue) {
        this.settings = Objects.requireNonNull(settings);
        this.singleValue = singleValue;
        Objects.requireNonNull(dsonValue);

        // 这里仍然是标准的数组上下文，但我们使用单值迭代器避免额外的封装开销
        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        if (singleValue) {
            context.header = null;
            context.container = dsonValue;
            context.arrayIterator.setBaseIterator(new SingleValueIterator<>(dsonValue));
        } else {
            DsonArray<Integer> dsonArray = dsonValue.asArrayLite();
            context.header = dsonArray.getHeader().size() > 0 ? dsonArray.getHeader() : null;
            context.container = dsonArray;
            context.arrayIterator.setBaseIterator(dsonArray.iterator());
        }
        setContext(context);
    }

    /** 用于支持池化 */
    public static DsonLiteCollectionReader unsafeCreate() {
        return new DsonLiteCollectionReader();
    }

    /** 适用读取顶层集合的单个值的情况 */
    public static DsonLiteCollectionReader unsafeCreate(DsonReaderSettings settings, DsonValue dsonValue, boolean singleValue) {
        DsonLiteCollectionReader reader = new DsonLiteCollectionReader();
        reader.unsafeInit(settings, dsonValue, singleValue);
        return reader;
    }

    /**
     * 设置key的迭代顺序
     *
     * @param defValue key不存在时的返回值；可选择{@link DsonNull#UNDEFINE}
     */
    public void setKeyItr(Iterator<Integer> keyItr, DsonValue defValue) {
        Objects.requireNonNull(keyItr);
        Objects.requireNonNull(defValue);
        Context context = getContext();
        context.setKeyItr(keyItr, defValue);
    }

    public Set<Integer> getkeySet() {
        Context context = getContext();
        return switch (context.contextType) {
            case HEADER -> context.container.asHeaderLite().keySet();
            case OBJECT -> context.container.asObjectLite().keySet();
            default -> throw new IllegalStateException();
        };
    }

    /** 获取当前容器 */
    public DsonValue getContainer() {
        Context context = getContext();
        return context.container;
    }

    /** 是否是单值集合（顶层上下文） */
    public boolean isSingleValueCollection() {
        return singleValue;
    }

    @Override
    protected Context getContext() {
        return (Context) context;
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
        nextName = 0;
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

    private void pushNextName(Integer nextName) {
        this.nextName = Objects.requireNonNull(nextName);
    }

    private int popNextName() {
        int r = this.nextName;
        this.nextName = 0;
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
            Map.Entry<Integer, DsonValue> nextElement = context.nextElement();
            if (nextElement == null) {
                dsonType = DsonType.END_OF_OBJECT;
            } else {
                pushNextName(nextElement.getKey());
                pushNextValue(nextElement.getValue());
                dsonType = nextElement.getValue().getDsonType();
            }
        }

        this.currentDsonType = dsonType;
        this.currentWireType = WireType.UINT;
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
            Map.Entry<Integer, DsonValue> nextElement = context.nextElement();
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
    protected ExtDateTime doReadDateTime() {
        return popNextValue().asDateTime();
    }

    @Override
    protected Timestamp doReadTimestamp() {
        return popNextValue().asTimestamp();
    }

    @Override
    protected Double4 doReadDouble4() {
        return popNextValue().asDouble4();
    }
    // endregion

    // region 容器

    @Override
    protected void doReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = newContext(getContext(), contextType, dsonType);
        DsonValue dsonValue = popNextValue();
        if (dsonValue.getDsonType() == DsonType.OBJECT) {
            DsonObject<Integer> dsonObject = dsonValue.asObjectLite();
            newContext.header = dsonObject.getHeader().size() > 0 ? dsonObject.getHeader() : null;
            newContext.container = dsonObject;
            newContext.objectIterator.setBaseIterator(dsonObject.entrySet().iterator());
        } else if (dsonValue.getDsonType() == DsonType.ARRAY) {
            DsonArray<Integer> dsonArray = dsonValue.asArrayLite();
            newContext.header = dsonArray.getHeader().size() > 0 ? dsonArray.getHeader() : null;
            newContext.container = dsonArray;
            newContext.arrayIterator.setBaseIterator(dsonArray.iterator());
        } else {
            // header
            DsonHeader<Integer> header = dsonValue.asHeaderLite();
            newContext.container = header;
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
        clearWaitStartContext();
    }

    @Override
    protected void doSkipToEndOfObject() {
        clearWaitStartContext();
        //
        Context context = getContext();
        context.header = null;
        if (context.contextType.isArrayLike()) {
            context.arrayIterator.close();
            context.arrayIterator.setBaseIterator(Collections.emptyIterator());
        } else {
            context.objectIterator.close();
            context.objectIterator.setBaseIterator(Collections.emptyIterator());
        }
    }

    @Override
    protected byte[] doReadValueAsBytes() {
        throw new UnsupportedOperationException();
    }

    private void clearWaitStartContext() {
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

    protected static class Context extends AbstractDsonLiteReader.Context
            implements Iterator<Map.Entry<Integer, DsonValue>> {

        /** 如果不为null，则表示需要先读取header */
        private DsonHeader<Integer> header;
        /** 当前读取的容器 -- 顶层对象不一定是array */
        private DsonValue container;
        /** 随着Context池化 */
        private final MarkableIterator<Map.Entry<Integer, DsonValue>> objectIterator = new MarkableIterator<>(null);
        private final MarkableIterator<DsonValue> arrayIterator = new MarkableIterator<>(null);

        /** 按照外部key迭代 -- 避免再封装一层增加开销 */
        private Iterator<Integer> keyItr;
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
            container = null;
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
            if (contextType.isArrayLike()) {
                return arrayIterator.hasNext();
            }
            return objectIterator.hasNext();
        }

        public void markItr() {
            if (contextType.isArrayLike()) {
                arrayIterator.mark();
            } else {
                objectIterator.mark();
            }
        }

        public void resetItr() {
            if (contextType.isArrayLike()) {
                arrayIterator.reset();
            } else {
                objectIterator.reset();
            }
        }

        public DsonValue nextValue() {
            return arrayIterator.hasNext() ? arrayIterator.next() : null;
        }

        public Map.Entry<Integer, DsonValue> nextElement() {
            return objectIterator.hasNext() ? objectIterator.next() : null;
        }

        // key-itr

        public void setKeyItr(Iterator<Integer> keyItr, DsonValue defValue) {
            if (contextType.isArrayLike()) throw new IllegalStateException("container is not an object");
            if (objectIterator.isMarking()) throw new IllegalStateException("reader is in marking state");

            this.keyItr = keyItr;
            this.defValue = defValue;
            objectIterator.close();
            objectIterator.setBaseIterator(this);
        }

        @Override
        public Map.Entry<Integer, DsonValue> next() {
            Integer key = keyItr.next();
            DsonValue dsonValue = container.asObjectLite().get(key);
            if (dsonValue == null) {
                return Map.entry(key, defValue);
            } else {
                return Map.entry(key, dsonValue);
            }
        }
    }
    // endregion

}