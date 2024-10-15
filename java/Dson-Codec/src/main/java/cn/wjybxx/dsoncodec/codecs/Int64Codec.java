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

import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/5/9
 */
@DsonCodecScanIgnore
public class Int64Codec implements DsonCodec<Long> {

    @Nonnull
    @Override
    public TypeInfo getEncoderType() {
        return TypeInfo.BOXED_LONG;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Long inst, TypeInfo declaredType, ObjectStyle style) {
        NumberStyle numberStyle = (declaredType.rawType == Long.class || declaredType.rawType == long.class) ?
                NumberStyle.SIMPLE : NumberStyle.TYPED;
        writer.writeLong(null, inst, WireType.VARINT, numberStyle);
    }

    @Override
    public Long readObject(DsonObjectReader reader, Supplier<? extends Long> factory) {
        return reader.readLong(null);
    }

}
