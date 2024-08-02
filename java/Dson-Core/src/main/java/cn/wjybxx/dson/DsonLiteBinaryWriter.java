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
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.io.DsonOutput;
import cn.wjybxx.dson.types.*;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonLiteBinaryWriter extends AbstractDsonLiteWriter {

    private DsonOutput output;
    private final boolean autoClose;

    public DsonLiteBinaryWriter(DsonWriterSettings settings, DsonOutput output) {
        this(settings, output, settings.autoClose);
    }

    public DsonLiteBinaryWriter(DsonWriterSettings settings, DsonOutput output, boolean autoClose) {
        super(settings);
        this.output = Objects.requireNonNull(output);
        this.autoClose = autoClose;

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        setContext(context);
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public void flush() {
        if (output != null) {
            output.flush();
        }
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
        if (output != null) {
            output.flush();
            if (autoClose) {
                output.close();
            }
            output = null;
        }
        super.close();
    }

    // region state

    private void writeFullTypeAndCurrentName(DsonOutput output, DsonType dsonType, int wireType) {
        output.writeRawByte((byte) Dsons.makeFullType(dsonType.getNumber(), wireType));
        if (dsonType == DsonType.HEADER) { // header是匿名属性
            return;
        }
        Context context = getContext();
        if (context.contextType == DsonContextType.OBJECT
                || context.contextType == DsonContextType.HEADER) {
            output.writeUint32(context.curName);
        }
    }

    // endregion

    // region 简单值

    @Override
    protected void doWriteInt32(int value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT32, wireType.getNumber());
        wireType.writeInt32(output, value);
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.INT64, wireType.getNumber());
        wireType.writeInt64(output, value);
    }

    @Override
    protected void doWriteFloat(float value) {
        int wireType = DsonReaderUtils.wireTypeOfFloat(value);
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.FLOAT, wireType);
        DsonReaderUtils.writeFloat(output, value, wireType);
    }

    @Override
    protected void doWriteDouble(double value) {
        int wireType = DsonReaderUtils.wireTypeOfDouble(value);
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.DOUBLE, wireType);
        DsonReaderUtils.writeDouble(output, value, wireType);
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BOOL, value ? 1 : 0); // 内联到wireType
    }

    @Override
    protected void doWriteString(String value) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.STRING, 0);
        output.writeString(value);
    }

    @Override
    protected void doWriteNull() {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.NULL, 0);
    }

    @Override
    protected void doWriteBinary(Binary binary) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, binary);
    }

    @Override
    protected void doWriteBinary(DsonChunk chunk) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.BINARY, 0);
        DsonReaderUtils.writeBinary(output, chunk);
    }

    @Override
    protected void doWritePtr(ObjectPtr objectPtr) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.POINTER, DsonReaderUtils.wireTypeOfPtr(objectPtr));
        DsonReaderUtils.writePtr(output, objectPtr);
    }

    @Override
    protected void doWriteLitePtr(ObjectLitePtr objectLitePtr) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.LITE_POINTER, DsonReaderUtils.wireTypeOfLitePtr(objectLitePtr));
        DsonReaderUtils.writeLitePtr(output, objectLitePtr);
    }

    @Override
    protected void doWriteDateTime(ExtDateTime dateTime) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.DATETIME, dateTime.getEnables());
        DsonReaderUtils.writeDateTime(output, dateTime);
    }

    @Override
    protected void doWriteTimestamp(Timestamp timestamp) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, DsonType.TIMESTAMP, 0);
        DsonReaderUtils.writeTimestamp(output, timestamp);
    }

    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, dsonType, 0);

        Context newContext = newContext(getContext(), contextType, dsonType);
        newContext.preWritten = output.getPosition();
        if (contextType == DsonContextType.HEADER) {
            output.writeFixed16(0);
        } else {
            output.writeFixed32(0);
        }

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = getContext();
        int preWritten = context.preWritten;

        int len;
        if (context.contextType == DsonContextType.HEADER) {
            len = output.getPosition() - preWritten - 2;
            if (len >= 65535) throw new DsonIOException("header is too large");
            output.setFixedInt16(preWritten, len);
        } else {
            len = output.getPosition() - preWritten - 4;
            output.setFixedInt32(preWritten, len);
        }

        this.recursionDepth--;
        setContext(context.parent);
        returnContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        DsonOutput output = this.output;
        writeFullTypeAndCurrentName(output, type, 0);
        DsonReaderUtils.writeValueBytes(output, type, data);
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

        int preWritten = 0;

        public Context() {
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

        @Override
        public void reset() {
            super.reset();
            preWritten = 0;
        }
    }

    // endregion

}