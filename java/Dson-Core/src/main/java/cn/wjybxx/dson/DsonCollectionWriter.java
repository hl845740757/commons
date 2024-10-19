package cn.wjybxx.dson;

import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.text.INumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.*;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public class DsonCollectionWriter extends AbstractDsonWriter {

    private final DsonArray<String> outList;

    public DsonCollectionWriter(DsonWriterSettings settings, DsonArray<String> outList) {
        super(settings);
        this.outList = Objects.requireNonNull(outList);

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        context.container = outList;
        setContext(context);
    }

    /** 获取传入的OutList */
    public DsonArray<String> getOutList() {
        return outList; // 不能通过Context查询，close后context会被清理
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public void flush() {

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
        super.close();
    }

    //region 简单值
    @Override
    protected void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        getContext().add(new DsonInt32(value));
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        getContext().add(new DsonInt64(value));
    }

    @Override
    protected void doWriteFloat(float value, INumberStyle style) {
        getContext().add(new DsonFloat(value));
    }

    @Override
    protected void doWriteDouble(double value, INumberStyle style) {
        getContext().add(new DsonDouble(value));
    }

    @Override
    protected void doWriteBool(boolean value) {
        getContext().add(DsonBool.valueOf(value));
    }

    @Override
    protected void doWriteString(String value, StringStyle style) {
        getContext().add(new DsonString(value));
    }

    @Override
    protected void doWriteNull() {
        getContext().add(DsonNull.NULL);
    }

    @Override
    protected void doWriteBinary(Binary binary) {
        getContext().add(new DsonBinary(binary)); // binary默认为可共享的
    }

    @Override
    protected void doWriteBinary(byte[] bytes, int offset, int len) {
        getContext().add(new DsonBinary(Binary.copyFrom(bytes, offset, len)));
    }

    @Override
    protected void doWritePtr(ObjectPtr objectPtr) {
        getContext().add(new DsonPointer(objectPtr));
    }

    @Override
    protected void doWriteLitePtr(ObjectLitePtr objectLitePtr) {
        getContext().add(new DsonLitePointer(objectLitePtr));
    }

    @Override
    protected void doWriteDateTime(ExtDateTime dateTime) {
        getContext().add(new DsonDateTime(dateTime));
    }

    @Override
    protected void doWriteTimestamp(Timestamp timestamp) {
        getContext().add(new DsonTimestamp(timestamp));
    }
    //endregion

    //region 容器
    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        Context parent = getContext();
        Context newContext = newContext(parent, contextType, dsonType);
        newContext.container = switch (contextType) {
            case HEADER -> parent.getHeader();
            case ARRAY -> new DsonArray<>();
            case OBJECT -> new DsonObject<>();
            default -> throw new AssertionError();
        };

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        Context context = getContext();
        if (context.contextType != DsonContextType.HEADER) {
            context.getParent().add(context.container);
        }

        this.recursionDepth--;
        setContext(context.parent);
        returnContext(context);
    }
    // endregion

    // region 特殊接口

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
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

    protected static class Context extends AbstractDsonWriter.Context {

        DsonValue container;

        public Context() {
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

        @Override
        public void reset() {
            super.reset();
            container = null;
        }

        public DsonHeader<String> getHeader() {
            if (container.getDsonType() == DsonType.OBJECT) {
                return container.asObject().getHeader();
            } else {
                return container.asArray().getHeader();
            }
        }

        @SuppressWarnings("unchecked")
        public void add(DsonValue value) {
            if (container.getDsonType() == DsonType.OBJECT) {
                ((DsonObject<String>) container).put(curName, value);
            } else if (container.getDsonType() == DsonType.ARRAY) {
                ((DsonArray<String>) container).add(value);
            } else {
                ((DsonHeader<String>) container).put(curName, value);
            }
        }
    }
    // endregion
}