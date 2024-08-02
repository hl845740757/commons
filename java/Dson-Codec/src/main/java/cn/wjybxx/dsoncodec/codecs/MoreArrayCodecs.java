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
import cn.wjybxx.dsoncodec.*;
import cn.wjybxx.dsoncodec.annotations.DsonCodecScanIgnore;
import it.unimi.dsi.fastutil.chars.CharArrayList;
import it.unimi.dsi.fastutil.doubles.DoubleArrayList;
import it.unimi.dsi.fastutil.floats.FloatArrayList;
import it.unimi.dsi.fastutil.ints.IntArrayList;
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

    @DsonCodecScanIgnore
    public static class IntArrayCodec implements DsonCodec<int[]> {

        @Nonnull
        @Override
        public Class<int[]> getEncoderClass() {
            return int[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, int[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (int e : instance) {
                writer.writeInt(null, e);
            }
        }

        @Override
        public int[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends int[]> factory) {
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
        public Class<long[]> getEncoderClass() {
            return long[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, long[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (long e : instance) {
                writer.writeLong(null, e);
            }
        }

        @Override
        public long[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends long[]> factory) {
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
        public Class<float[]> getEncoderClass() {
            return float[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, float[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (float e : instance) {
                writer.writeFloat(null, e, NumberStyle.SIMPLE);
            }
        }

        @Override
        public float[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends float[]> factory) {
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
        public Class<double[]> getEncoderClass() {
            return double[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, double[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (double e : instance) {
                writer.writeDouble(null, e, NumberStyle.SIMPLE);
            }
        }

        @Override
        public double[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends double[]> factory) {
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
        public Class<boolean[]> getEncoderClass() {
            return boolean[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, boolean[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (boolean e : instance) {
                writer.writeBoolean(null, e);
            }
        }

        @Override
        public boolean[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends boolean[]> factory) {
            ArrayList<Boolean> result = new ArrayList<>();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readBoolean(null));
            }
            return DsonConverterUtils.convertList2Array(result, boolean[].class);
        }
    }

    @DsonCodecScanIgnore
    public static class StringArrayCodec implements DsonCodec<String[]> {

        @Nonnull
        @Override
        public Class<String[]> getEncoderClass() {
            return String[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, String[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (String e : instance) {
                writer.writeString(null, e, StringStyle.AUTO);
            }
        }

        @Override
        public String[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends String[]> factory) {
            ArrayList<String> result = new ArrayList<>();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readString(null));
            }
            return result.toArray(new String[result.size()]);
        }
    }


    @DsonCodecScanIgnore
    public static class ShortArrayCodec implements DsonCodec<short[]> {

        @Nonnull
        @Override
        public Class<short[]> getEncoderClass() {
            return short[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, short[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (short e : instance) {
                writer.writeShort(null, e);
            }
        }

        @Override
        public short[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends short[]> factory) {
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
        public Class<char[]> getEncoderClass() {
            return char[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, char[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            for (char e : instance) {
                writer.writeChar(null, e);
            }
        }

        @Override
        public char[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends char[]> factory) {
            CharArrayList result = new CharArrayList();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readChar(null));
            }
            return result.toCharArray();
        }
    }

    @DsonCodecScanIgnore
    public static class ObjectArrayCodec implements DsonCodec<Object[]> {

        @Nonnull
        @Override
        public Class<Object[]> getEncoderClass() {
            return Object[].class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, Object[] instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            TypeInfo<?> componentArgInfo = typeInfo.isArray() ? typeInfo.getComponentType() : TypeInfo.OBJECT;

            for (Object e : instance) {
                writer.writeObject(null, e, componentArgInfo, null);
            }
        }

        @Override
        public Object[] readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends Object[]> factory) {
            TypeInfo<?> componentArgInfo = typeInfo.isArray() ? typeInfo.getComponentType() : TypeInfo.OBJECT;

            ArrayList<Object> result = new ArrayList<>();
            while (reader.readDsonType() != DsonType.END_OF_OBJECT) {
                result.add(reader.readObject(null, componentArgInfo));
            }
            // 一定不是基础类型数组
            return (Object[]) DsonConverterUtils.convertList2Array(result, typeInfo.rawType);
        }
    }
}
