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
public class FloatCodec implements DsonCodec<Float> {
    @Override
    public boolean isWriteAsArray() {
        return false;
    }

    @Override
    public boolean autoStartEnd() {
        return false;
    }

    @Nonnull
    @Override
    public Class<Float> getEncoderClass() {
        return Float.class;
    }

    @Override
    public void writeObject(DsonObjectWriter writer, Float instance, TypeInfo<?> typeInfo, ObjectStyle style) {
        NumberStyle numberStyle = (typeInfo.rawType == Float.class || typeInfo.rawType == float.class) ?
                NumberStyle.SIMPLE : NumberStyle.TYPED;
        writer.writeFloat(null, instance, numberStyle);
    }

    @Override
    public Float readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends Float> factory) {
        return reader.readFloat(reader.getCurrentName());
    }

}
