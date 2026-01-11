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
import cn.wjybxx.base.io.StringBuilderWriter;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.io.*;
import cn.wjybxx.dson.text.DsonTextReader;
import cn.wjybxx.dson.text.DsonTextWriter;
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.io.Reader;
import java.io.Writer;
import java.util.Arrays;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/2
 */
class DefaultDsonConverter implements DsonConverter {

    private final TypeMetaRegistry typeMetaRegistry;
    private final DsonCodecRegistry codecRegistry;
    private final GenericHelper genericHelper;
    private final TypeWriteHelper typeWriteHelper;
    private final ConverterOptions options;

    DefaultDsonConverter(TypeMetaRegistry typeMetaRegistry,
                         DsonCodecRegistry codecRegistry,
                         GenericHelper genericHelper,
                         TypeWriteHelper typeWriteHelper,
                         ConverterOptions options) {
        this.codecRegistry = codecRegistry;
        this.typeMetaRegistry = typeMetaRegistry;
        this.genericHelper = genericHelper;
        this.typeWriteHelper = typeWriteHelper;
        this.options = options;
    }

    @Override
    public DsonCodecRegistry codecRegistry() {
        return codecRegistry;
    }

    @Override
    public TypeMetaRegistry typeMetaRegistry() {
        return typeMetaRegistry;
    }

    @Override
    public GenericHelper genericCodecHelper() {
        return genericHelper;
    }

    @Override
    public ConverterOptions options() {
        return options;
    }

    @Override
    public DsonConverter withOptions(ConverterOptions options) {
        Objects.requireNonNull(options);
        return new DefaultDsonConverter(typeMetaRegistry, codecRegistry, genericHelper, typeWriteHelper, options);
    }
    // region binary

    @Nonnull
    @Override
    public byte[] write(Object value, @Nonnull TypeInfo declaredType) {
        Objects.requireNonNull(value);
        // 外部销毁，确保buffer归还
        try (var outputStream = DsonOutputs.newInstance(options.bufferPool, options.bufferLength, options.maxBufferLength)) {
            encodeObject(outputStream, value, declaredType);
            return Arrays.copyOfRange(outputStream.getBuffer(), 0, outputStream.getPosition());
        }
    }

    @Override
    public <T> T read(byte[] source, @Nonnull TypeInfo declaredType, Supplier<? extends T> factory) {
        try (DsonInput inputStream = DsonInputs.newInstance(source)) {
            return decodeObject(inputStream, declaredType, factory);
        }
    }

    @Override
    public void write(Object value, TypeInfo declaredType, DsonChunk chunk) {
        Objects.requireNonNull(value);
        try (DsonOutput outputStream = DsonOutputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength())) {
            encodeObject(outputStream, value, declaredType);
            chunk.setUsed(outputStream.getPosition());
        }
    }

    @Override
    public <T> T read(DsonChunk chunk, TypeInfo declaredType, Supplier<? extends T> factory) {
        try (DsonInput inputStream = DsonInputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength())) {
            T result = decodeObject(inputStream, declaredType, factory);
            chunk.setUsed(inputStream.getPosition());
            return result;
        }
    }

    @Override
    public void write(Object value, TypeInfo declaredType, DsonOutput output) {
        Objects.requireNonNull(value);
        Objects.requireNonNull(output, "output");
        encodeObject(output, value, declaredType);
    }

    @Override
    public <T> T read(DsonInput input, TypeInfo declaredType, @Nullable Supplier<? extends T> factory) {
        Objects.requireNonNull(input, "input");
        return decodeObject(input, declaredType, factory);
    }

    @Override
    public <T> T cloneObject(Object value, TypeInfo declaredType, TypeInfo targetType, Supplier<? extends T> factory) {
        if (value == null) return null;
        try (var outputStream = DsonOutputs.newInstance(options.bufferPool, options.bufferLength, options.maxBufferLength)) {
            encodeObject(outputStream, value, declaredType);
            // 不销毁
            DsonInput inputStream = DsonInputs.newInstance(outputStream.getBuffer(), 0, outputStream.getPosition());
            return decodeObject(inputStream, targetType, factory);
        }
    }

    /** 注意：由外部销毁输出流 */
    private void encodeObject(DsonOutput outputStream, Object value, TypeInfo typeInfo) {
        DsonBinaryWriter binaryWriter = new DsonBinaryWriter(options.binWriterSettings, outputStream, false);
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, typeWriteHelper, binaryWriter)) {
            wrapper.writeObject(null, value, typeInfo, null);
            wrapper.flush();
        }
    }

    /** 注意：由外部销毁输入流 */
    private <T> T decodeObject(DsonInput inputStream, TypeInfo typeInfo, Supplier<? extends T> factory) {
        DsonBinaryReader binaryReader = new DsonBinaryReader(options.binReaderSettings, inputStream, false);
        try (DsonObjectReader wrapper = wrapReader(binaryReader)) {
            return wrapper.readObject(null, typeInfo, factory);
        }
    }

    private DsonObjectReader wrapReader(DsonReader reader) {
        return new DefaultDsonObjectReader(this, toDsonCollectionReader(reader));
    }

    private DsonCollectionReader toDsonCollectionReader(DsonReader dsonReader) {
        assert !(dsonReader instanceof DsonCollectionReader);
        DsonValue dsonValue = Dsons.readTopDsonValue(dsonReader);
        return DsonCollectionReader.unsafeCreate(options.binReaderSettings, dsonValue, true);
    }
    // endregion

    // region text
    @Nonnull
    @Override
    public String writeAsDson(Object value, @Nonnull TypeInfo declaredType, ObjectStyle style) {
        StringBuilder stringBuilder = options.stringBuilderPool.acquire();
        try {
            writeAsDson(value, declaredType, new StringBuilderWriter(stringBuilder), style);
            return stringBuilder.toString();
        } finally {
            options.stringBuilderPool.release(stringBuilder);
        }
    }

    @Override
    public <T> T readFromDson(CharSequence source, @Nonnull TypeInfo declaredType, Supplier<? extends T> factory) {
        try (DsonObjectReader wrapper = wrapReader(new DsonTextReader(options.textReaderSettings, source))) {
            return wrapper.readObject(null, declaredType, factory);
        }
    }

    @Override
    public void writeAsDson(Object value, @Nonnull TypeInfo declaredType, Writer writer, ObjectStyle style) {
        Objects.requireNonNull(writer, "writer");
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, typeWriteHelper,
                new DsonTextWriter(options.textWriterSettings, writer, false))) {
            wrapper.writeObject(null, value, declaredType, style);
            wrapper.flush();
        }
    }

    @Override
    public <T> T readFromDson(Reader source, @Nonnull TypeInfo declaredType, Supplier<? extends T> factory) {
        try (DsonObjectReader wrapper = wrapReader(
                new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source, false)))) {
            return wrapper.readObject(null, declaredType, factory);
        }
    }

    @Override
    public DsonValue writeAsDsonValue(Object value, TypeInfo declaredType) {
        Objects.requireNonNull(value);
        DsonArray<String> outList = new DsonArray<>(1);
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this, typeWriteHelper,
                new DsonCollectionWriter(options.binWriterSettings, outList))) {
            wrapper.writeObject(null, value, declaredType, ObjectStyle.INDENT);
            DsonValue dsonValue = outList.get(0);
            if (dsonValue.getDsonType().isContainer()) {
                return dsonValue;
            }
            throw new IllegalArgumentException("value must be container");
        }
    }

    @Override
    public <T> T readFromDsonValue(DsonValue source, @Nonnull TypeInfo declaredType, Supplier<? extends T> factory) {
        if (!source.getDsonType().isContainer()) {
            throw new IllegalArgumentException("value must be container");
        }
        try (DsonObjectReader wrapper = new DefaultDsonObjectReader(this,
                DsonCollectionReader.unsafeCreate(options.binReaderSettings, source, true))) {
            return wrapper.readObject(null, declaredType, factory);
        }
    }

    @Override
    public DsonValue readAsDsonValue(Reader source) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source, false))) {
            return Dsons.readTopDsonValue(textReader);
        }
    }

    @Override
    public DsonValue readAsDsonValue(DsonInput source) {
        try (DsonReader binaryReader = new DsonBinaryReader(options.binReaderSettings, source, false)) {
            return Dsons.readTopDsonValue(binaryReader);
        }
    }

    // endregion
}