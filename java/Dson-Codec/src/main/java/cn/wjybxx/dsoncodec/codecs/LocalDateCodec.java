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

package cn.wjybxx.dsoncodec.codecs;

import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.ExtDateTime;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/1/7
 */
@DsonCodecScanIgnore
public class LocalDateCodec implements DsonCodec<LocalDate> {

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return TypeInfo.of(LocalDate.class);
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, LocalDate inst, TypeInfo declaredType, ObjectStyle style) {
        ExtDateTime dateTime = toDateTime(inst);
        writer.writeExtDateTime(null, dateTime);
    }

    @Override
    public LocalDate readObject(DsonObjectReader reader, Supplier<? extends LocalDate> factory) {
        ExtDateTime dateTime = reader.readExtDateTime(null);
        return ofDateTime(dateTime);
    }

    private static LocalDate ofDateTime(ExtDateTime dateTime) {
        LocalDateTime localDateTime = LocalDateTime.ofEpochSecond(dateTime.getSeconds(), 0, ZoneOffset.UTC);
        return localDateTime.toLocalDate();
    }

    private static ExtDateTime toDateTime(LocalDate localDate) {
        long epochSecond = localDate.toEpochSecond(LocalTime.MIN, ZoneOffset.UTC);
        return new ExtDateTime(epochSecond, 0, 0, ExtDateTime.MASK_DATE);
    }
}