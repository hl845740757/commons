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

import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.text.StringStyle;

import javax.annotation.Nonnull;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2023/4/28
 */
class CodecStructs {

    /** 该类不添加到类型仓库，也不提供codec -- 直接外部读写 */
    static final class NestStruct {

        public final int intVal;
        public final long longVal;
        public final float floatVal;
        public final double doubleVal;

        NestStruct(int intVal, long longVal, float floatVal, double doubleVal) {
            this.intVal = intVal;
            this.longVal = longVal;
            this.floatVal = floatVal;
            this.doubleVal = doubleVal;
        }

        @Override
        public boolean equals(Object obj) {
            if (obj == this) return true;
            if (obj == null || obj.getClass() != this.getClass()) return false;
            var that = (NestStruct) obj;
            return this.intVal == that.intVal &&
                    this.longVal == that.longVal &&
                    Float.floatToIntBits(this.floatVal) == Float.floatToIntBits(that.floatVal) &&
                    Double.doubleToLongBits(this.doubleVal) == Double.doubleToLongBits(that.doubleVal);
        }

        @Override
        public int hashCode() {
            return Objects.hash(intVal, longVal, floatVal, doubleVal);
        }

        @Override
        public String toString() {
            return "NestStruct[" +
                    "intVal=" + intVal + ", " +
                    "longVal=" + longVal + ", " +
                    "floatVal=" + floatVal + ", " +
                    "doubleVal=" + doubleVal + ']';
        }

    }

    /**
     * Java出的新Record特性有点问题啊。。。
     * 比较字节数组的时候用的不是{@link Arrays#equals(byte[], byte[])}，而是{@link Objects#equals(Object, Object)}...
     * 只能先用record定义，定义完一键转Class，再修改hashCode和equals方法
     */
    static final class MyStruct {
        public final int intVal;
        public final long longVal;
        public final float floatVal;
        public final double doubleVal;
        public final boolean boolVal;
        public final String strVal;
        public final byte[] bytes;
        public final Sex sex;
        public final Map<String, Object> map;
        public final List<String> list;
        public final NestStruct nestStruct;

        public MyStruct(int intVal, long longVal, float floatVal, double doubleVal, boolean boolVal, String strVal,
                        byte[] bytes, Sex sex, Map<String, Object> map, List<String> list, NestStruct nestStruct) {
            this.intVal = intVal;
            this.longVal = longVal;
            this.floatVal = floatVal;
            this.doubleVal = doubleVal;
            this.boolVal = boolVal;
            this.strVal = strVal;
            this.bytes = bytes;
            this.sex = sex;
            this.map = map;
            this.list = list;
            this.nestStruct = nestStruct;
        }

        @Override
        public boolean equals(Object obj) {
            if (obj == this) return true;
            if (obj == null || obj.getClass() != this.getClass()) return false;
            var that = (MyStruct) obj;
            return this.intVal == that.intVal &&
                    this.longVal == that.longVal &&
                    Float.floatToIntBits(this.floatVal) == Float.floatToIntBits(that.floatVal) &&
                    Double.doubleToLongBits(this.doubleVal) == Double.doubleToLongBits(that.doubleVal) &&
                    this.boolVal == that.boolVal &&
                    Objects.equals(this.strVal, that.strVal) &&
                    Arrays.equals(this.bytes, that.bytes) &&
                    Objects.equals(this.map, that.map) &&
                    Objects.equals(this.list, that.list) &&
                    Objects.equals(this.nestStruct, that.nestStruct);
        }

        @Override
        public int hashCode() {
            return Objects.hash(intVal, longVal, floatVal, doubleVal, boolVal, strVal, Arrays.hashCode(bytes), map, list, nestStruct);
        }

        @Override
        public String toString() {
            return "MyStruct[" +
                    "intVal=" + intVal + ", " +
                    "longVal=" + longVal + ", " +
                    "floatVal=" + floatVal + ", " +
                    "doubleVal=" + doubleVal + ", " +
                    "boolVal=" + boolVal + ", " +
                    "strVal=" + strVal + ", " +
                    "bytes=" + Arrays.toString(bytes) + ", " +
                    "map=" + map + ", " +
                    "list=" + list + ", " +
                    "nestStruct=" + nestStruct + ']';
        }

    }

    static class MyStructCodec implements DsonCodec<MyStruct> {

        @Override
        public boolean isWriteAsArray() {
            return false;
        }

        @Override
        public boolean autoStartEnd() {
            return true;
        }

        @Nonnull
        @Override
        public Class<MyStruct> getEncoderClass() {
            return MyStruct.class;
        }

        @Override
        public void writeObject(DsonObjectWriter writer, MyStruct instance, TypeInfo<?> typeInfo, ObjectStyle style) {
            NestStruct nestStruct = instance.nestStruct;
            writer.writeStartObject("nestStruct", nestStruct, TypeInfo.of(NestStruct.class));
            {
                writer.writeInt("intVal", nestStruct.intVal);
                writer.writeLong("longVal", nestStruct.longVal);
                writer.writeFloat("floatVal", nestStruct.floatVal, NumberStyle.SIMPLE);
                writer.writeDouble("doubleVal", nestStruct.doubleVal, NumberStyle.SIMPLE);
            }
            writer.writeEndObject();

            writer.writeInt("intVal", instance.intVal);
            writer.writeLong("longVal", instance.longVal);
            writer.writeFloat("floatVal", instance.floatVal, NumberStyle.SIMPLE);
            writer.writeDouble("doubleVal", instance.doubleVal, NumberStyle.SIMPLE);
            writer.writeBoolean("boolVal", instance.boolVal);
            writer.writeString("strVal", instance.strVal, StringStyle.AUTO);
            writer.writeBytes("bytes", instance.bytes);
            writer.writeObject("sex", instance.sex, TypeInfo.of(Sex.class));
            writer.writeObject("map", instance.map, TypeInfo.STRING_LINKED_HASHMAP, null);
            writer.writeObject("list", instance.list, TypeInfo.ARRAYLIST, null);
        }

        @SuppressWarnings("unchecked")
        @Override
        public MyStruct readObject(DsonObjectReader reader, TypeInfo<?> typeInfo, Supplier<? extends MyStruct> factory) {
            reader.readStartObject("nestStruct");
            NestStruct nestStruct = new NestStruct(
                    reader.readInt("intVal"),
                    reader.readLong("longVal"),
                    reader.readFloat("floatVal"),
                    reader.readDouble("doubleVal"));
            reader.readEndObject();

            return new MyStruct(
                    reader.readInt("intVal"),
                    reader.readLong("longVal"),
                    reader.readFloat("floatVal"),
                    reader.readDouble("doubleVal"),
                    reader.readBoolean("boolVal"),
                    reader.readString("strVal"),
                    reader.readBytes("bytes"),
                    reader.readObject("sex", TypeInfo.of(Sex.class)),
                    reader.readObject("map", TypeInfo.STRING_LINKED_HASHMAP),
                    reader.readObject("list", TypeInfo.ARRAYLIST),
                    nestStruct);
        }
    }
}