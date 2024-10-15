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

import cn.wjybxx.dson.DsonType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;
import cn.wjybxx.dson.types.Binary;
import cn.wjybxx.dsoncodec.DsonCodec;
import cn.wjybxx.dsoncodec.DsonObjectReader;
import cn.wjybxx.dsoncodec.DsonObjectWriter;
import cn.wjybxx.dsoncodec.TypeInfo;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import it.unimi.dsi.fastutil.chars.CharArrayList;
import it.unimi.dsi.fastutil.doubles.DoubleArrayList;
import it.unimi.dsi.fastutil.floats.FloatArrayList;
import it.unimi.dsi.fastutil.ints.IntArrayList;
import it.unimi.dsi.fastutil.ints.IntList;
import it.unimi.dsi.fastutil.longs.LongArrayList;
import it.unimi.dsi.fastutil.shorts.ShortArrayList;

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.function.Supplier;

/**
 * 特化数组支持
 *
 * @author wjybxx
 * date 2023/4/4
 */
public final class MoreArrayCodecs {

    /** 字节数组需要转Binary */
    public static class ByteArrayCodec implements DsonCodec<byte[]> {
        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_BYTE;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, byte[] bytes, TypeInfo declaredType, ObjectStyle style) {
            writer.writeBinary(null, Binary.copyFrom(bytes)); // 默认拷贝
        }

        @Override
        public byte[] readObject(DsonObjectReader reader, Supplier<? extends byte[]> factory) {
            Binary binary = reader.readBinary(reader.getCurrentName());
            return binary.unsafeBuffer();
        }
    }

    @DsonCodecScanIgnore
    public static class IntArrayCodec implements DsonCodec<int[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_INT;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, int[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (int e : inst) {
                writer.writeInt(null, e);
            }
        }

        @Override
        public int[] readObject(DsonObjectReader reader, Supplier<? extends int[]> factory) {
            IntArrayList result = new IntArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readInt(null));
            }
            return result.toIntArray();
        }
    }

    @DsonCodecScanIgnore
    public static class LongArrayCodec implements DsonCodec<long[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_LONG;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, long[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (long e : inst) {
                writer.writeLong(null, e);
            }
        }

        @Override
        public long[] readObject(DsonObjectReader reader, Supplier<? extends long[]> factory) {
            LongArrayList result = new LongArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readLong(null));
            }
            return result.toLongArray();
        }
    }

    @DsonCodecScanIgnore
    public static class FloatArrayCodec implements DsonCodec<float[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_FLOAT;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, float[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (float e : inst) {
                writer.writeFloat(null, e, NumberStyle.SIMPLE);
            }
        }

        @Override
        public float[] readObject(DsonObjectReader reader, Supplier<? extends float[]> factory) {
            FloatArrayList result = new FloatArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readFloat(null));
            }
            return result.toFloatArray();
        }
    }

    @DsonCodecScanIgnore
    public static class DoubleArrayCodec implements DsonCodec<double[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_DOUBLE;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, double[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (double e : inst) {
                writer.writeDouble(null, e, NumberStyle.SIMPLE);
            }
        }

        @Override
        public double[] readObject(DsonObjectReader reader, Supplier<? extends double[]> factory) {
            DoubleArrayList result = new DoubleArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readDouble(null));
            }
            return result.toDoubleArray();
        }
    }

    @DsonCodecScanIgnore
    public static class BooleanArrayCodec implements DsonCodec<boolean[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_BOOL;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, boolean[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (boolean e : inst) {
                writer.writeBoolean(null, e);
            }
        }

        @Override
        public boolean[] readObject(DsonObjectReader reader, Supplier<? extends boolean[]> factory) {
            IntList result = new IntArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                int v = reader.readBoolean(null) ? 1 : 0;
                result.add(v);
            }
            // 手动转换
            boolean[] array = new boolean[result.size()];
            for (int i = 0; i < result.size(); i++) {
                array[i] = result.getInt(i) == 1;
            }
            return array;
        }
    }

    @DsonCodecScanIgnore
    public static class StringArrayCodec implements DsonCodec<String[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_STRING;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, String[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (String e : inst) {
                writer.writeString(null, e, StringStyle.AUTO);
            }
        }

        @Override
        public String[] readObject(DsonObjectReader reader, Supplier<? extends String[]> factory) {
            ArrayList<String> result = new ArrayList<>();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readString(null));
            }
            String[] array = new String[result.size()];
            return result.toArray(array);
        }
    }

    @DsonCodecScanIgnore
    public static class ShortArrayCodec implements DsonCodec<short[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_SHORT;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, short[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (short e : inst) {
                writer.writeShort(null, e);
            }
        }

        @Override
        public short[] readObject(DsonObjectReader reader, Supplier<? extends short[]> factory) {
            ShortArrayList result = new ShortArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readShort(null));
            }
            return result.toShortArray();
        }
    }

    @DsonCodecScanIgnore
    public static class CharArrayCodec implements DsonCodec<char[]> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.ARRAY_CHAR;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, char[] inst, TypeInfo declaredType, ObjectStyle style) {
            for (char e : inst) {
                writer.writeChar(null, e);
            }
        }

        @Override
        public char[] readObject(DsonObjectReader reader, Supplier<? extends char[]> factory) {
            CharArrayList result = new CharArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readChar(null));
            }
            return result.toCharArray();
        }
    }
}
