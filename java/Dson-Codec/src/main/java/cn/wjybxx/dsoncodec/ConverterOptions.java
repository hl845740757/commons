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

import javax.annotation.Nullable;
import javax.annotation.concurrent.Immutable;
import java.util.ArrayDeque;
import java.util.Objects;

/**
 * 允许继承扩展，子类应继续保持不可变。
 *
 * @author wjybxx
 * date - 2023/4/17
 */
@Immutable
public class ConverterOptions {

    /** classId的写入策略 */
    public final ClassIdPolicy classIdPolicy;
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
     * 是否把Map编码为普通对象（文档）
     * 1.只在文档编解码中生效(DsonCodec)
     * 2.如果要将一个Map结构编码为普通对象，<b>Key的运行时必须和声明类型相同</b>，且只支持String、Integer、Long、EnumLite。
     * 3.在不启用该选项的情况下，用户可通过字段写代理将字段转换为{@link MapEncodeProxy}，实现更精确的控制。
     *
     * <h3>Map不是Object</h3>
     * 本质上讲，Map是数组，而不是普通的Object，因为标准的Map是允许复杂key的，因此Map默认应该序列化为数组。但存在两个特殊的场景：
     * 1.与脚本语言通信
     * 脚本语言通常没有静态语言中的字典结构，由object充当，但object不支持复杂的key作为键，通常仅支持数字和字符串作为key。
     * 因此在与脚本语言通信时，要求将Map序列化为简单的object。
     * 2.配置文件读写
     * 配置文件通常是无类型的，因此读取到内存中通常是一个字典结构；程序在输出配置文件时，同样需要将字典结构输出为object结构。
     */
    public final boolean writeMapAsDocument;
    /**
     * 是否将枚举写为字符串
     * 1.只在文档编码中生效(DsonCodec)
     * 2.对字典的Key也生效
     */
    public final boolean writeEnumAsString;
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
     * 是否启用{@code void beforeEncode(ConverterOptions)}钩子方法。
     * 默认不启用！因为启用该特性要求同一个Bean不能被多线程同时序列化 -- 只适用单线程序列化场景，
     */
    public final boolean enableBeforeEncode;
    /**
     * 是否启用{@code void afterDecode(ConverterOptions)}钩子方法。
     * 默认启用！因为我们假设afterDecode仅依赖自身数据。
     */
    public final boolean enableAfterDecode;
    /**
     * 集合转换器，主要用于读取为不可变集合。
     * 当使用Dson读取配置文件时，保持配置对象的不可变性是非常重要的。
     * 交给用户处理，使得可以支持特殊的集合实现。
     */
    @Nullable
    public final CollectionConverter collectionConverter;

    /** protoBuf对应的二进制子类型 -- 其它模块依赖 */
    public final int pbBinaryType;
    /** converter的用途 -- 用于判断是临时序列化，还是持久化入库等 */
    public final int usage;

    /** 序列化申请的字节数组大小 */
    public final int bufferSize;
    /** 字节数组缓存池 -- 多线程下需要注意线程安全问题 */
    public final ArrayPool<byte[]> bufferPool;
    /** 字典key队列缓存池 */
    public final ObjectPool<ArrayDeque<String>> keySetPool;
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
        this.classIdPolicy = builder.classIdPolicy;
        this.appendDef = builder.appendDef;
        this.appendNull = builder.appendNull;
        this.writeMapAsDocument = builder.writeMapAsDocument;
        this.writeEnumAsString = builder.writeEnumAsString;
        this.randomRead = builder.randomRead;
        this.enableBeforeEncode = builder.enableBeforeEncode;
        this.enableAfterDecode = builder.enableAfterDecode;
        this.collectionConverter = builder.collectionConverter;

        this.pbBinaryType = builder.pbBinaryType;
        this.usage = builder.usage;

        this.bufferSize = builder.bufferSize;
        this.bufferPool = Objects.requireNonNull(builder.bufferPool);
        this.keySetPool = Objects.requireNonNull(builder.keySetPool);
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
        builder.classIdPolicy = classIdPolicy;
        builder.appendDef = appendDef;
        builder.appendNull = appendNull;
        builder.writeMapAsDocument = writeMapAsDocument;
        builder.writeEnumAsString = writeEnumAsString;
        builder.randomRead = randomRead;
        builder.enableBeforeEncode = enableBeforeEncode;
        builder.enableAfterDecode = enableAfterDecode;
        builder.collectionConverter = collectionConverter;

        builder.pbBinaryType = pbBinaryType;
        builder.usage = usage;

        builder.bufferSize = bufferSize;
        builder.bufferPool = bufferPool;
        builder.keySetPool = keySetPool;
        builder.stringBuilderPool = stringBuilderPool;

        builder.binReaderSettings = binReaderSettings;
        builder.binWriterSettings = binWriterSettings;
        builder.textReaderSettings = textReaderSettings;
        builder.textWriterSettings = textWriterSettings;
    }

    /** 全局共享的key队列 */
    public static final ObjectPool<ArrayDeque<String>> SHARED_KEY_SET_POOL = new ConcurrentObjectPool<>(
            ArrayDeque::new, ArrayDeque::clear, 64);
    /** 默认的Options */
    public static ConverterOptions DEFAULT = newBuilder().build(); // 有初始化顺序依赖

    public static Builder newBuilder() {
        return new Builder();
    }

    public static class Builder {

        private ClassIdPolicy classIdPolicy = ClassIdPolicy.OPTIMIZED;
        private boolean appendDef = true;
        private boolean appendNull = true;
        private boolean writeMapAsDocument = false;
        private boolean writeEnumAsString = false;
        private boolean randomRead = true;
        private boolean enableBeforeEncode = false;
        private boolean enableAfterDecode = true;
        private CollectionConverter collectionConverter = null;

        private int pbBinaryType = 127;
        private int usage;

        private int bufferSize = 8192;
        private ArrayPool<byte[]> bufferPool = ConcurrentArrayPool.SHARED_BYTE_ARRAY_POOL;
        private ObjectPool<ArrayDeque<String>> keySetPool = SHARED_KEY_SET_POOL;
        private ObjectPool<StringBuilder> stringBuilderPool = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL;

        private DsonReaderSettings binReaderSettings = DsonReaderSettings.DEFAULT;
        private DsonWriterSettings binWriterSettings = DsonWriterSettings.DEFAULT;
        private DsonTextReaderSettings textReaderSettings = DsonTextReaderSettings.DEFAULT;
        private DsonTextWriterSettings textWriterSettings = DsonTextWriterSettings.DEFAULT;

        public ConverterOptions build() {
            return new ConverterOptions(this);
        }

        public ClassIdPolicy getClassIdPolicy() {
            return classIdPolicy;
        }

        public Builder setClassIdPolicy(ClassIdPolicy classIdPolicy) {
            this.classIdPolicy = Objects.requireNonNull(classIdPolicy);
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

        public boolean isWriteMapAsDocument() {
            return writeMapAsDocument;
        }

        public Builder setWriteMapAsDocument(boolean writeMapAsDocument) {
            this.writeMapAsDocument = writeMapAsDocument;
            return this;
        }

        public boolean isWriteEnumAsString() {
            return writeEnumAsString;
        }

        public Builder setWriteEnumAsString(boolean writeEnumAsString) {
            this.writeEnumAsString = writeEnumAsString;
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

        public CollectionConverter getCollectionConverter() {
            return collectionConverter;
        }

        public Builder setCollectionConverter(CollectionConverter collectionConverter) {
            this.collectionConverter = collectionConverter;
            return this;
        }

        public int getBufferSize() {
            return bufferSize;
        }

        public Builder setBufferSize(int bufferSize) {
            this.bufferSize = bufferSize;
            return this;
        }

        public ArrayPool<byte[]> getBufferPool() {
            return bufferPool;
        }

        public Builder setBufferPool(ArrayPool<byte[]> bufferPool) {
            this.bufferPool = bufferPool;
            return this;
        }

        public ObjectPool<ArrayDeque<String>> getKeySetPool() {
            return keySetPool;
        }

        public Builder setKeySetPool(ObjectPool<ArrayDeque<String>> keySetPool) {
            this.keySetPool = keySetPool;
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