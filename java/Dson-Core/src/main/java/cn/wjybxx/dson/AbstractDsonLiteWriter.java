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

import cn.wjybxx.base.io.ByteBufferUtils;
import cn.wjybxx.dson.io.DsonIOException;
import cn.wjybxx.dson.types.*;

import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
public abstract class AbstractDsonLiteWriter implements DsonLiteWriter {

    protected final DsonWriterSettings settings;
    protected Context context;
    protected int recursionDepth;

    protected AbstractDsonLiteWriter(DsonWriterSettings settings) {
        this.settings = Objects.requireNonNull(settings, "settings");
    }

    protected Context getContext() {
        return context;
    }

    protected void setContext(Context context) {
        this.context = context;
    }

    @Override
    public void close() {
        context = null;
        recursionDepth = 0;
    }

    // region state

    @Override
    public DsonContextType getContextType() {
        return context.contextType;
    }

    @Override
    public int getContextDepth() {
        return recursionDepth;
    }

    @Override
    public int getCurrentName() {
        Context context = this.context;
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
        return context.curName;
    }

    @Override
    public boolean isAtName() {
        return context.state == DsonWriterState.NAME;
    }

    @Override
    public void writeName(int name) {
        if (name < 0) {
            throw new IllegalArgumentException("name cant be negative, but found: " + name);
        }
        Context context = this.context;
        if (context.state != DsonWriterState.NAME) {
            throw invalidState(List.of(DsonWriterState.NAME), context.state);
        }
        context.curName = name;
        context.state = DsonWriterState.VALUE;
        doWriteName(name);
    }

    /** 执行{@link #writeName(int)}时调用 */
    protected void doWriteName(int name) {

    }

    protected final void advanceToValueState(int name) {
        Context context = this.context;
        if (context.state == DsonWriterState.NAME) {
            writeName(name);
        }
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    protected final void ensureValueState(Context context) {
        if (context.state != DsonWriterState.VALUE) {
            throw invalidState(List.of(DsonWriterState.VALUE), context.state);
        }
    }

    protected final void setNextState() {
        switch (context.contextType) {
            case OBJECT, HEADER -> context.setState(DsonWriterState.NAME);
            case TOP_LEVEL, ARRAY -> context.setState(DsonWriterState.VALUE);
        }
    }

    protected final DsonIOException invalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonIOException.invalidState(context.contextType, expected, state);
    }
    // endregion

    // region 简单值
    @Override
    public void writeInt32(int name, int value) {
        advanceToValueState(name);
        doWriteInt32(value);
        setNextState();
    }

    @Override
    public void writeInt64(int name, long value) {
        advanceToValueState(name);
        doWriteInt64(value);
        setNextState();
    }

    @Override
    public void writeFloat(int name, float value) {
        advanceToValueState(name);
        doWriteFloat(value);
        setNextState();
    }

    @Override
    public void writeDouble(int name, double value) {
        advanceToValueState(name);
        doWriteDouble(value);
        setNextState();
    }

    @Override
    public void writeBool(int name, boolean value) {
        advanceToValueState(name);
        doWriteBool(value);
        setNextState();
    }

    @Override
    public void writeString(int name, String value) {
        Objects.requireNonNull(value);
        advanceToValueState(name);
        doWriteString(value);
        setNextState();
    }

    @Override
    public void writeNull(int name) {
        advanceToValueState(name);
        doWriteNull();
        setNextState();
    }

    @Override
    public void writeBinary(int name, Binary binary) {
        Objects.requireNonNull(binary);
        advanceToValueState(name);
        doWriteBinary(binary);
        setNextState();
    }

    @Override
    public void writeBinary(int name, byte[] bytes, int offset, int len) {
        ByteBufferUtils.checkBuffer(bytes, offset, len);
        advanceToValueState(name);
        doWriteBinary(bytes, offset, len);
        setNextState();
    }

    @Override
    public void writePtr(int name, ObjectPtr objectPtr) {
        Objects.requireNonNull(objectPtr);
        advanceToValueState(name);
        doWritePtr(objectPtr);
        setNextState();
    }

    @Override
    public void writeDateTime(int name, ExtDateTime dateTime) {
        Objects.requireNonNull(dateTime);
        advanceToValueState(name);
        doWriteDateTime(dateTime);
        setNextState();
    }

    @Override
    public void writeTimestamp(int name, Timestamp timestamp) {
        Objects.requireNonNull(timestamp);
        advanceToValueState(name);
        doWriteTimestamp(timestamp);
        setNextState();
    }

    @Override
    public void writeDouble4(int name, Double4 double4) {
        Objects.requireNonNull(double4);
        advanceToValueState(name);
        doWriteDouble4(double4);
        setNextState();
    }

    // region 无name版
    @Override
    public void writeInt32(int value) {
        ensureValueState(context);
        doWriteInt32(value);
        setNextState();
    }

    @Override
    public void writeInt64(long value) {
        ensureValueState(context);
        doWriteInt64(value);
        setNextState();
    }

    @Override
    public void writeFloat(float value) {
        ensureValueState(context);
        doWriteFloat(value);
        setNextState();
    }

    @Override
    public void writeDouble(double value) {
        ensureValueState(context);
        doWriteDouble(value);
        setNextState();
    }

    @Override
    public void writeBool(boolean value) {
        ensureValueState(context);
        doWriteBool(value);
        setNextState();
    }

    @Override
    public void writeString(String value) {
        Objects.requireNonNull(value);
        ensureValueState(context);
        doWriteString(value);
        setNextState();
    }

    @Override
    public void writeNull() {
        ensureValueState(context);
        doWriteNull();
        setNextState();
    }

    @Override
    public void writeBinary(Binary binary) {
        Objects.requireNonNull(binary);
        ensureValueState(context);
        doWriteBinary(binary);
        setNextState();
    }

    @Override
    public void writeBinary(byte[] bytes, int offset, int len) {
        ByteBufferUtils.checkBuffer(bytes, offset, len);
        ensureValueState(context);
        doWriteBinary(bytes, offset, len);
        setNextState();
    }

    @Override
    public void writePtr(ObjectPtr objectPtr) {
        Objects.requireNonNull(objectPtr);
        ensureValueState(context);
        doWritePtr(objectPtr);
        setNextState();
    }

    @Override
    public void writeDateTime(ExtDateTime dateTime) {
        Objects.requireNonNull(dateTime);
        ensureValueState(context);
        doWriteDateTime(dateTime);
        setNextState();
    }

    @Override
    public void writeTimestamp(Timestamp timestamp) {
        Objects.requireNonNull(timestamp);
        ensureValueState(context);
        doWriteTimestamp(timestamp);
        setNextState();
    }

    @Override
    public void writeDouble4(Double4 double4) {
        Objects.requireNonNull(double4);
        ensureValueState(context);
        doWriteDouble4(double4);
        setNextState();
    }

    protected abstract void doWriteInt32(int value);

    protected abstract void doWriteInt64(long value);

    protected abstract void doWriteFloat(float value);

    protected abstract void doWriteDouble(double value);

    protected abstract void doWriteBool(boolean value);

    protected abstract void doWriteString(String value);

    protected abstract void doWriteNull();

    protected abstract void doWriteBinary(Binary binary);

    protected abstract void doWriteBinary(byte[] bytes, int offset, int len);

    protected abstract void doWritePtr(ObjectPtr objectPtr);

    protected abstract void doWriteDateTime(ExtDateTime dateTime);

    protected abstract void doWriteTimestamp(Timestamp timestamp);

    protected abstract void doWriteDouble4(Double4 double4);
    // endregion

    // region 容器
    @Override
    public void writeStartArray() {
        writeStartContainer(DsonContextType.ARRAY, DsonType.ARRAY);
    }

    @Override
    public void writeEndArray() {
        writeEndContainer(DsonContextType.ARRAY, DsonWriterState.VALUE);
    }

    @Override
    public void writeStartObject() {
        writeStartContainer(DsonContextType.OBJECT, DsonType.OBJECT);
    }

    @Override
    public void writeEndObject() {
        writeEndContainer(DsonContextType.OBJECT, DsonWriterState.NAME);
    }

    @Override
    public void writeStartHeader() {
        // object下默认是name状态
        Context context = this.context;
        if (context.contextType == DsonContextType.OBJECT && context.state == DsonWriterState.NAME) {
            context.setState(DsonWriterState.VALUE);
        }
        writeStartContainer(DsonContextType.HEADER, DsonType.HEADER);
    }

    @Override
    public void writeEndHeader() {
        writeEndContainer(DsonContextType.HEADER, DsonWriterState.NAME);
    }

    private void writeStartContainer(DsonContextType contextType, DsonType dsonType) {
        if (recursionDepth >= settings.recursionLimit) {
            throw DsonIOException.recursionLimitExceeded();
        }
        Context context = this.context;
        autoStartTopLevel(context);
        ensureValueState(context);
        doWriteStartContainer(contextType, dsonType);
        setNextState(); // 设置新上下文状态
    }

    private void writeEndContainer(DsonContextType contextType, DsonWriterState expectedState) {
        Context context = this.context;
        checkEndContext(context, contextType, expectedState);
        doWriteEndContainer();
        setNextState(); // parent前进一个状态
    }

    protected void autoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TOP_LEVEL
                && context.state == DsonWriterState.INITIAL) {
            context.setState(DsonWriterState.VALUE);
        }
    }

    protected void checkEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context.contextType != contextType) {
            throw DsonIOException.contextError(contextType, context.contextType);
        }
        if (context.state != state) {
            throw invalidState(List.of(state), context.state);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void doWriteStartContainer(DsonContextType contextType, DsonType dsonType);

    /** 弹出上下文 */
    protected abstract void doWriteEndContainer();

    // endregion
    // region sp

    @Override
    public void writeValueBytes(int name, DsonType type, byte[] data) {
        DsonReaderUtils.checkWriteValueAsBytes(type);
        advanceToValueState(name);
        doWriteValueBytes(type, data);
        setNextState();
    }

    @Override
    public Object attach(Object userData) {
        return context.attach(userData);
    }

    @Override
    public Object attachment() {
        return context.userData;
    }

    protected abstract void doWriteValueBytes(DsonType type, byte[] data);

    // endregion

    // region context

    protected static abstract class Context {

        public Context parent;
        public DsonContextType contextType;
        public DsonType dsonType; // 用于在Object/Array模式下写入内置数据结构
        public DsonWriterState state = DsonWriterState.INITIAL;
        public int curName;
        public Object userData;

        public Context() {
        }

        public Context init(Context parent, DsonContextType contextType, DsonType dsonType) {
            this.parent = parent;
            this.contextType = contextType;
            this.dsonType = dsonType;
            return this;
        }

        public void reset() {
            parent = null;
            contextType = null;
            dsonType = null;
            state = DsonWriterState.INITIAL;
            curName = 0;
            userData = null;
        }

        public Object attach(Object userData) {
            Object r = this.userData;
            this.userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void setState(DsonWriterState state) {
            this.state = state;
        }

        public Context getParent() {
            return parent;
        }
    }
    // endregion
}