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

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * 更多基础类型Codec
 *
 * @author wjybxx
 * date - 2024/10/15
 */
public final class MorePrimitiveCodecs {

    public static class ShortCodec implements DsonCodec<Short> {
        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.BOXED_SHORT;
        }

        @Override
        public boolean autoStartEnd() {
            return false;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, Short inst, TypeInfo declaredType, ObjectStyle style) {
            NumberStyle numberStyle = (declaredType.rawType == Short.class || declaredType.rawType == short.class) ?
                    NumberStyle.SIMPLE : NumberStyle.TYPED;
            writer.writeInt(null, inst, WireType.SINT, numberStyle);
        }

        @Override
        public Short readObject(DsonObjectReader reader, Supplier<? extends Short> factory) {
            return (short) reader.readInt(null);
        }
    }

    public static class ByteCodec implements DsonCodec<Byte> {
        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.BOXED_BYTE;
        }

        @Override
        public boolean autoStartEnd() {
            return false;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, Byte inst, TypeInfo declaredType, ObjectStyle style) {
            NumberStyle numberStyle = (declaredType.rawType == Byte.class || declaredType.rawType == byte.class) ?
                    NumberStyle.SIMPLE : NumberStyle.TYPED;
            writer.writeInt(null, inst, WireType.SINT, numberStyle);
        }

        @Override
        public Byte readObject(DsonObjectReader reader, Supplier<? extends Byte> factory) {
            return (byte) reader.readInt(null);
        }
    }

    public static class CharacterCodec implements DsonCodec<Character> {
        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.BOXED_CHAR;
        }

        @Override
        public boolean autoStartEnd() {
            return false;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, Character inst, TypeInfo declaredType, ObjectStyle style) {
            NumberStyle numberStyle = (declaredType.rawType == Character.class || declaredType.rawType == char.class) ?
                    NumberStyle.SIMPLE : NumberStyle.TYPED;
            writer.writeInt(null, inst, WireType.UINT, numberStyle);
        }

        @Override
        public Character readObject(DsonObjectReader reader, Supplier<? extends Character> factory) {
            return (char) reader.readInt(null);
        }
    }
}
