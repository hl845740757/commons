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

import cn.wjybxx.base.pool.ArrayPool;
import cn.wjybxx.base.pool.ConcurrentArrayPool;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.base.pool.ObjectPool;
import cn.wjybxx.dson.DsonReaderSettings;
import cn.wjybxx.dson.DsonWriterSettings;
import cn.wjybxx.dson.text.DsonTextReaderSettings;
import cn.wjybxx.dson.text.DsonTextWriterSettings;

import javax.annotation.concurrent.Immutable;
import java.util.Objects;

/**
 * 允许继承扩展，子类应继续保持不可变。
 *
 * @author wjybxx
 * date - 2023/4/17
 */
@Immutable
public class ConverterOptions {

    /** 类型信息的写入策略 */
    public final TypeWritePolicy typeWritePolicy;
    /**
     * 是否写入对象基础类型字段的默认值
     * 1.数值类型默认值为0，bool类型默认值为false
     * 2.只在Object上下文生效
     * <p>
     * 基础值类型需要单独控制，因为有时候我们仅想不输出null，但要输出基础类型字段的默认值 -- 通常是在文本模式下。
     */
    public final boolean appendDef;
    /**
     * 是否写入对象内的null值
     * 1.只在Object上下文生效
     * 2.对于一般的对象可不写入，因为ObjectReader是支持随机读的
     */
    public final boolean appendNull;
    /**
     * 字典的编码策略
     */
    public final MapEncodePolicy mapEncodePolicy;
    /**
     * 是否将枚举写为字符串
     * 注：通常不建议开启，兼容性不好；如果个别字段的字典想定制编码，可通过字段编解码代理实现。
     */
    public final boolean writeEnumAsString;
    /**
     * 是否将普通object编码为数组
     * 1.如果开启该选项，将不写入object的字段名，只是顺序写入object的所有字段值。
     * 2.这可以避免大量的字符串编解码，从而提升性能 - 适用于非持久化场景。
     * 3.该选项仅对{@link DsonCodec#autoStartEnd()}为true的编码器有效。
     * 4.不可以有基于name进行Switch编解码的codec。
     * 5.对象字段不可以有特殊的初始值 -- 否则会被反序列化覆盖。
     */
    public final boolean writeObjectAsArray;

    /**
     * 是否启用随机读。
     * 启用随机读会增加较多的开销，需要先读取为中间结构，再解码为对象；但启用随机读的数据兼容性更好。
     * 如果不写入默认值和null值的，通常都需要启用该特性。
     * 如果需要反复反序列化一个对象(通常是配置文件)，可以先解码为中间对象，将中间对象保存下来。
     * 另一种方式是先反序列化，然后完整序列化为字节数组，再通过字节数组反序列化 -- 可关闭随机读。
     * 注意：启用该特性后，不再支持{@link DsonObjectReader#readValueAsBytes(String)}接口。
     */
    public final boolean randomRead;
    /**
     * 集合类型是否读取为不可变
     * 其它类型的对象也可以使用该设置
     */
    public final boolean readAsImmutable;
    /**
     * 是否启用{@code void beforeEncode(ConverterOptions)}钩子方法。
     * 默认不启用！因为启用该特性要求同一个Bean不能被多线程同时序列化 -- 只适用单线程序列化场景，
     */
    public final boolean enableBeforeEncode;
    /**
     * 是否启用{@code void afterDecode(ConverterOptions)}钩子方法。
     * 默认启用！因为我们假设afterDecode仅依赖自身数据。
     */
    public final boolean enableAfterDecode;

    /** protoBuf对应的二进制子类型 -- 其它模块依赖 */
    public final int pbBinaryType;
    /** converter的用途 -- 用于判断是临时序列化，还是持久化入库等 */
    public final int usage;

    /** 序列化申请的字节数组大小 */
    public final int bufferLength;
    /** 序列化申请的最大字节数组大小 */
    public final int maxBufferLength;
    /** 字节数组缓存池 -- 多线程下需要注意线程安全问题 */
    public final ArrayPool<byte[]> bufferPool;
    /** 字符串缓存池 -- 多线程下需要注意线程安全问题 */
    public final ObjectPool<StringBuilder> stringBuilderPool;

    /** 二进制解码设置 */
    public final DsonReaderSettings binReaderSettings;
    /** 二进制编码设置 */
    public final DsonWriterSettings binWriterSettings;
    /** 文本解码设置 */
    public final DsonTextReaderSettings textReaderSettings;
    /** 文本编码设置 */
    public final DsonTextWriterSettings textWriterSettings;

    public ConverterOptions(Builder builder) {
        this.typeWritePolicy = builder.typeWritePolicy;
        this.appendDef = builder.appendDef;
        this.appendNull = builder.appendNull;
        this.mapEncodePolicy = builder.mapEncodePolicy;
        this.writeEnumAsString = builder.writeEnumAsString;
        this.writeObjectAsArray = builder.writeObjectAsArray;

        this.randomRead = builder.randomRead;
        this.readAsImmutable = builder.readAsImmutable;
        this.enableBeforeEncode = builder.enableBeforeEncode;
        this.enableAfterDecode = builder.enableAfterDecode;

        this.pbBinaryType = builder.pbBinaryType;
        this.usage = builder.usage;

        this.bufferLength = builder.bufferLength;
        this.maxBufferLength = builder.maxBufferLength;
        this.bufferPool = Objects.requireNonNull(builder.bufferPool);
        this.stringBuilderPool = Objects.requireNonNull(builder.stringBuilderPool);

        this.binReaderSettings = Objects.requireNonNull(builder.binReaderSettings);
        this.binWriterSettings = Objects.requireNonNull(builder.binWriterSettings);
        this.textReaderSettings = Objects.requireNonNull(builder.textReaderSettings);
        this.textWriterSettings = Objects.requireNonNull(builder.textWriterSettings);
    }

    /** 用于快速构建少许差异的options */
    public Builder toBuilder() {
        Builder builder = new Builder();
        assignToBuilder(builder);
        return builder;
    }

    /** 子类可覆盖该方法 */
    public void assignToBuilder(Builder builder) {
        builder.typeWritePolicy = typeWritePolicy;
        builder.appendDef = appendDef;
        builder.appendNull = appendNull;
        builder.mapEncodePolicy = mapEncodePolicy;
        builder.writeEnumAsString = writeEnumAsString;
        builder.writeObjectAsArray = writeObjectAsArray;

        builder.randomRead = randomRead;
        builder.readAsImmutable = readAsImmutable;
        builder.enableBeforeEncode = enableBeforeEncode;
        builder.enableAfterDecode = enableAfterDecode;

        builder.pbBinaryType = pbBinaryType;
        builder.usage = usage;

        builder.bufferLength = bufferLength;
        builder.bufferPool = bufferPool;
        builder.stringBuilderPool = stringBuilderPool;

        builder.binReaderSettings = binReaderSettings;
        builder.binWriterSettings = binWriterSettings;
        builder.textReaderSettings = textReaderSettings;
        builder.textWriterSettings = textWriterSettings;
    }

    /** 默认的Options */
    public static ConverterOptions DEFAULT = newBuilder().build(); // 有初始化顺序依赖

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private TypeWritePolicy typeWritePolicy = TypeWritePolicy.OPTIMIZED;
        private boolean appendDef = true;
        private boolean appendNull = true;
        private MapEncodePolicy mapEncodePolicy = MapEncodePolicy.ARRAY;
        private boolean writeEnumAsString = false;
        private boolean writeObjectAsArray = false;
        private boolean randomRead = true;
        private boolean enableBeforeEncode = false;
        private boolean enableAfterDecode = true;
        private boolean readAsImmutable = false;

        private int pbBinaryType = 127;
        private int usage;

        private int bufferLength = 8192;
        private int maxBufferLength = 1024 * 1024;
        private ArrayPool<byte[]> bufferPool = ConcurrentArrayPool.SHARED_BYTE_ARRAY_POOL;
        private ObjectPool<StringBuilder> stringBuilderPool = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL;

        private DsonReaderSettings binReaderSettings = DsonReaderSettings.DEFAULT;
        private DsonWriterSettings binWriterSettings = DsonWriterSettings.DEFAULT;
        private DsonTextReaderSettings textReaderSettings = DsonTextReaderSettings.DEFAULT;
        private DsonTextWriterSettings textWriterSettings = DsonTextWriterSettings.DEFAULT;

        public ConverterOptions build() {
            return new ConverterOptions(this);
        }

        public TypeWritePolicy getTypeWritePolicy() {
            return typeWritePolicy;
        }

        public Builder setTypeWritePolicy(TypeWritePolicy typeWritePolicy) {
            this.typeWritePolicy = typeWritePolicy;
            return this;
        }

        public boolean isAppendDef() {
            return appendDef;
        }

        public Builder setAppendDef(boolean appendDef) {
            this.appendDef = appendDef;
            return this;
        }

        public boolean isAppendNull() {
            return appendNull;
        }

        public Builder setAppendNull(boolean appendNull) {
            this.appendNull = appendNull;
            return this;
        }

        public MapEncodePolicy getMapEncodePolicy() {
            return mapEncodePolicy;
        }

        public Builder setMapEncodePolicy(MapEncodePolicy mapEncodePolicy) {
            this.mapEncodePolicy = mapEncodePolicy;
            return this;
        }

        public boolean isWriteEnumAsString() {
            return writeEnumAsString;
        }

        public Builder setWriteEnumAsString(boolean writeEnumAsString) {
            this.writeEnumAsString = writeEnumAsString;
            return this;
        }

        public boolean isWriteObjectAsArray() {
            return writeObjectAsArray;
        }

        public Builder setWriteObjectAsArray(boolean writeObjectAsArray) {
            this.writeObjectAsArray = writeObjectAsArray;
            return this;
        }

        public boolean isRandomRead() {
            return randomRead;
        }

        public Builder setRandomRead(boolean randomRead) {
            this.randomRead = randomRead;
            return this;
        }

        public int getPbBinaryType() {
            return pbBinaryType;
        }

        public Builder setPbBinaryType(int pbBinaryType) {
            this.pbBinaryType = pbBinaryType;
            return this;
        }

        public int getUsage() {
            return usage;
        }

        public Builder setUsage(int usage) {
            this.usage = usage;
            return this;
        }

        public boolean isReadAsImmutable() {
            return readAsImmutable;
        }

        public Builder setReadAsImmutable(boolean readAsImmutable) {
            this.readAsImmutable = readAsImmutable;
            return this;
        }

        public boolean isEnableBeforeEncode() {
            return enableBeforeEncode;
        }

        public Builder setEnableBeforeEncode(boolean enableBeforeEncode) {
            this.enableBeforeEncode = enableBeforeEncode;
            return this;
        }

        public boolean isEnableAfterDecode() {
            return enableAfterDecode;
        }

        public Builder setEnableAfterDecode(boolean enableAfterDecode) {
            this.enableAfterDecode = enableAfterDecode;
            return this;
        }

        public int getBufferLength() {
            return bufferLength;
        }

        public Builder setBufferLength(int bufferLength) {
            this.bufferLength = bufferLength;
            return this;
        }

        public int getMaxBufferLength() {
            return maxBufferLength;
        }

        public void setMaxBufferLength(int maxBufferLength) {
            this.maxBufferLength = maxBufferLength;
        }

        public ArrayPool<byte[]> getBufferPool() {
            return bufferPool;
        }

        public Builder setBufferPool(ArrayPool<byte[]> bufferPool) {
            this.bufferPool = bufferPool;
            return this;
        }

        public ObjectPool<StringBuilder> getStringBuilderPool() {
            return stringBuilderPool;
        }

        public Builder setStringBuilderPool(ObjectPool<StringBuilder> stringBuilderPool) {
            this.stringBuilderPool = stringBuilderPool;
            return this;
        }

        public DsonReaderSettings getBinReaderSettings() {
            return binReaderSettings;
        }

        public Builder setBinReaderSettings(DsonReaderSettings binReaderSettings) {
            this.binReaderSettings = binReaderSettings;
            return this;
        }

        public DsonWriterSettings getBinWriterSettings() {
            return binWriterSettings;
        }

        public Builder setBinWriterSettings(DsonWriterSettings binWriterSettings) {
            this.binWriterSettings = binWriterSettings;
            return this;
        }

        public DsonTextReaderSettings getTextReaderSettings() {
            return textReaderSettings;
        }

        public Builder setTextReaderSettings(DsonTextReaderSettings textReaderSettings) {
            this.textReaderSettings = textReaderSettings;
            return this;
        }

        public DsonTextWriterSettings getTextWriterSettings() {
            return textWriterSettings;
        }

        public Builder setTextWriterSettings(DsonTextWriterSettings textWriterSettings) {
            this.textWriterSettings = textWriterSettings;
            return this;
        }
    }

}