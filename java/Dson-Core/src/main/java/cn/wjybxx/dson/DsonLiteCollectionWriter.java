package cn.wjybxx.dson;

import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.types.*;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/13
 */
public class DsonLiteCollectionWriter extends AbstractDsonLiteWriter {

    private final DsonArray<Integer> outList;

    public DsonLiteCollectionWriter(DsonWriterSettings settings, DsonArray<Integer> outList) {
        super(settings);
        this.outList = Objects.requireNonNull(outList);

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        context.container = outList;
        setContext(context);
    }

    /** 获取传入的OutList */
    public DsonArray<Integer> getOutList() {
        return outList; // 不能通过Context查询，close后context会被清理
    }

    @Override
    protected Context getContext() {
        return (Context) context;
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

    // region 简单值

    @Override
    protected void doWriteInt32(int value) {
        getContext().add(DsonInt32.valueOf(value));
    }

    @Override
    protected void doWriteInt64(long value) {
        getContext().add(DsonInt64.valueOf(value));
    }

    @Override
    protected void doWriteFloat(float value) {
        getContext().add(DsonFloat.valueOf(value));
    }

    @Override
    protected void doWriteDouble(double value) {
        getContext().add(DsonDouble.valueOf(value));
    }

    @Override
    protected void doWriteBool(boolean value) {
        getContext().add(DsonBool.valueOf(value));
    }

    @Override
    protected void doWriteString(String value) {
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
    protected void doWriteDateTime(ExtDateTime dateTime) {
        getContext().add(new DsonDateTime(dateTime));
    }

    @Override
    protected void doWriteTimestamp(Timestamp timestamp) {
        getContext().add(new DsonTimestamp(timestamp));
    }

    @Override
    protected void doWriteDouble4(Double4 double4) {
        getContext().add(new DsonDouble4(double4));
    }
    //endregion

    //region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType) {
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

    protected static class Context extends AbstractDsonLiteWriter.Context {

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

        public DsonHeader<Integer> getHeader() {
            if (container.getDsonType() == DsonType.OBJECT) {
                return container.asObjectLite().getHeader();
            } else {
                return container.asArrayLite().getHeader();
            }
        }

        @SuppressWarnings("unchecked")
        public void add(DsonValue value) {
            if (container.getDsonType() == DsonType.OBJECT) {
                ((DsonObject<Integer>) container).put(curName, value);
            } else if (container.getDsonType() == DsonType.ARRAY) {
                ((DsonArray<Integer>) container).add(value);
            } else {
                ((DsonHeader<Integer>) container).put(curName, value);
            }
        }

    }
    // endregion
}