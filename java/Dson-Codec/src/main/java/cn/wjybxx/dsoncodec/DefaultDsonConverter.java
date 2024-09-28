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
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/2
 */
class DefaultDsonConverter implements DsonConverter {

    private final TypeMetaRegistry typeMetaRegistry;
    private final DsonCodecRegistry codecRegistry;
    private final GenericCodecHelper genericCodecHelper;
    private final ConverterOptions options;

    DefaultDsonConverter(TypeMetaRegistry typeMetaRegistry,
                         DsonCodecRegistry codecRegistry,
                         GenericCodecHelper genericCodecHelper,
                         ConverterOptions options) {
        this.codecRegistry = codecRegistry;
        this.typeMetaRegistry = typeMetaRegistry;
        this.genericCodecHelper = genericCodecHelper;
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
    public GenericCodecHelper genericCodecHelper() {
        return genericCodecHelper;
    }

    @Override
    public ConverterOptions options() {
        return options;
    }

    @Override
    public DsonConverter withOptions(ConverterOptions options) {
        Objects.requireNonNull(options);
        return new DefaultDsonConverter(typeMetaRegistry, codecRegistry, genericCodecHelper, options);
    }

    @Nonnull
    @Override
    public byte[] write(Object value, @Nonnull TypeInfo typeInfo) {
        Objects.requireNonNull(value);
        final byte[] localBuffer = options.bufferPool.acquire(options.bufferSize);
        try {
            final DsonChunk chunk = new DsonChunk(localBuffer);
            write(value, typeInfo, chunk);
            return chunk.usedPayload();
        } finally {
            options.bufferPool.release(localBuffer);
        }
    }

    @Override
    public <T> T read(byte[] source, @Nonnull TypeInfo typeInfo, Supplier<? extends T> factory) {
        final DsonInput inputStream = DsonInputs.newInstance(source);
        return decodeObject(inputStream, typeInfo, factory);
    }

    @Override
    public void write(Object value, TypeInfo typeInfo, DsonChunk chunk) {
        Objects.requireNonNull(value);
        final DsonOutput outputStream = DsonOutputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        encodeObject(outputStream, value, typeInfo);
        chunk.setUsed(outputStream.getPosition());
    }

    @Override
    public <T> T read(DsonChunk chunk, TypeInfo typeInfo, Supplier<? extends T> factory) {
        final DsonInput inputStream = DsonInputs.newInstance(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
        return decodeObject(inputStream, typeInfo, factory);
    }

    @Override
    public <T> T cloneObject(Object value, TypeInfo typeInfo, Supplier<? extends T> factory) {
        if (value == null) return null;
        final byte[] localBuffer = options.bufferPool.acquire(options.bufferSize);
        try {
            final DsonOutput outputStream = DsonOutputs.newInstance(localBuffer);
            encodeObject(outputStream, value, typeInfo);

            final DsonInput inputStream = DsonInputs.newInstance(localBuffer, 0, outputStream.getPosition());
            return decodeObject(inputStream, typeInfo, factory);
        } finally {
            options.bufferPool.release(localBuffer);
        }
    }

    private void encodeObject(DsonOutput outputStream, @Nullable Object value, TypeInfo typeInfo) {
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this,
                new DsonBinaryWriter(options.binWriterSettings, outputStream))) {
            wrapper.writeObject(null, value, typeInfo, null);
            wrapper.flush();
        }
    }

    private <T> T decodeObject(DsonInput inputStream, TypeInfo typeInfo, Supplier<? extends T> factory) {
        try (DsonObjectReader wrapper = wrapReader(new DsonBinaryReader(options.binReaderSettings, inputStream))) {
            return wrapper.readObject(null, typeInfo, factory);
        }
    }

    private DsonObjectReader wrapReader(DsonReader reader) {
        if (options.randomRead) {
            return new BufferedDsonObjectReader(this, toDsonCollectionReader(reader));
        } else {
            return new DefaultDsonObjectReader(this, reader);
        }
    }

    private DsonCollectionReader toDsonCollectionReader(DsonReader dsonReader) {
        assert !(dsonReader instanceof DsonCollectionReader);
        DsonValue dsonValue = Dsons.readTopDsonValue(dsonReader);
        return new DsonCollectionReader(options.binReaderSettings, new DsonArray<String>().append(dsonValue));
    }

    @Nonnull
    @Override
    public String writeAsDson(Object value, @Nonnull TypeInfo typeInfo, ObjectStyle style) {
        StringBuilder stringBuilder = options.stringBuilderPool.acquire();
        try {
            writeAsDson(value, typeInfo, new StringBuilderWriter(stringBuilder), style);
            return stringBuilder.toString();
        } finally {
            options.stringBuilderPool.release(stringBuilder);
        }
    }

    @Override
    public <T> T readFromDson(CharSequence source, @Nonnull TypeInfo typeInfo, Supplier<? extends T> factory) {
        try (DsonObjectReader wrapper = wrapReader(new DsonTextReader(options.textReaderSettings, source))) {
            return wrapper.readObject(null, typeInfo, factory);
        }
    }

    @Override
    public void writeAsDson(Object value, @Nonnull TypeInfo typeInfo, Writer writer, ObjectStyle style) {
        Objects.requireNonNull(writer, "writer");
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this,
                new DsonTextWriter(options.textWriterSettings, writer, false))) {
            wrapper.writeObject(null, value, typeInfo, style);
            wrapper.flush();
        }
    }

    @Override
    public <T> T readFromDson(Reader source, @Nonnull TypeInfo typeInfo, Supplier<? extends T> factory) {
        try (DsonObjectReader wrapper = wrapReader(
                new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source, false)))) {
            return wrapper.readObject(null, typeInfo, factory);
        }
    }

    @Override
    public DsonValue writeAsDsonValue(Object value, TypeInfo typeInfo) {
        Objects.requireNonNull(value);
        DsonArray<String> outList = new DsonArray<>(1);
        try (DsonObjectWriter wrapper = new DefaultDsonObjectWriter(this,
                new DsonCollectionWriter(options.binWriterSettings, outList))) {
            wrapper.writeObject(null, value, typeInfo, ObjectStyle.INDENT);
            DsonValue dsonValue = outList.get(0);
            if (dsonValue.getDsonType().isContainer()) {
                return dsonValue;
            }
            throw new IllegalArgumentException("value must be container");
        }
    }

    @Override
    public <T> T readFromDsonValue(DsonValue source, @Nonnull TypeInfo typeInfo, Supplier<? extends T> factory) {
        if (!source.getDsonType().isContainer()) {
            throw new IllegalArgumentException("value must be container");
        }
        DsonArray<String> dsonArray = new DsonArray<String>().append(source);
        try (DsonObjectReader wrapper = new BufferedDsonObjectReader(this,
                new DsonCollectionReader(options.binReaderSettings, dsonArray))) {
            return wrapper.readObject(null, typeInfo, factory);
        }
    }

    @Override
    public DsonValue readAsDsonValue(Reader source) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source, false))) {
            return Dsons.readTopDsonValue(textReader);
        }
    }

    @Override
    public DsonArray<String> readAsDsonCollection(Reader source) {
        try (DsonReader textReader = new DsonTextReader(options.textReaderSettings, Dsons.newStreamScanner(source, false))) {
            return Dsons.readCollection(textReader);
        }
    }
}