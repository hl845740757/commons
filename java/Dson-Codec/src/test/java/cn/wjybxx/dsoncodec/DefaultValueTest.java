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

import cn.wjybxx.dson.WireType;
import cn.wjybxx.dson.text.NumberStyle;
import cn.wjybxx.dson.text.ObjectStyle;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * 测试不写入默认值情况下的解码测试。
 *
 * @author wjybxx
 * date - 2023/9/17
 */
public class DefaultValueTest {

    @Test
    void docTest() {
        ConverterOptions options = ConverterOptions.newBuilder()
                .setWriteMapAsDocument(true)
                .setAppendDef(false)
                .build();

        DsonConverter converter = new DsonConverterBuilder()
                .addTypeMeta(TypeMeta.of(Bean.class, ObjectStyle.INDENT, "Bean"))
                .addCodec(new BeanDocCodec())
                .setOptions(options)
                .build();
        Bean bean = new Bean();
        bean.iv1 = 1;
        bean.lv2 = 3;
        bean.fv2 = 5;
        bean.dv1 = 7;
        bean.bv1 = true;

        String dson = converter.writeAsDson(bean);
//        System.out.println(dson);

        Bean bean2 = converter.readFromDson(dson, TypeInfo.of(Bean.class));
        Assertions.assertEquals(bean, bean2);
    }


    public static class Bean {
        public int iv1;
        public int iv2;
        public long lv1;
        public long lv2;
        public float fv1;
        public float fv2;
        public double dv1;
        public double dv2;
        public boolean bv1;
        public boolean bv2;

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            Bean bean = (Bean) o;

            if (iv1 != bean.iv1) return false;
            if (iv2 != bean.iv2) return false;
            if (lv1 != bean.lv1) return false;
            if (lv2 != bean.lv2) return false;
            if (Float.compare(bean.fv1, fv1) != 0) return false;
            if (Float.compare(bean.fv2, fv2) != 0) return false;
            if (Double.compare(bean.dv1, dv1) != 0) return false;
            if (Double.compare(bean.dv2, dv2) != 0) return false;
            if (bv1 != bean.bv1) return false;
            return bv2 == bean.bv2;
        }

        @Override
        public int hashCode() {
            int result;
            long temp;
            result = iv1;
            result = 31 * result + iv2;
            result = 31 * result + (int) (lv1 ^ (lv1 >>> 32));
            result = 31 * result + (int) (lv2 ^ (lv2 >>> 32));
            result = 31 * result + (fv1 != +0.0f ? Float.floatToIntBits(fv1) : 0);
            result = 31 * result + (fv2 != +0.0f ? Float.floatToIntBits(fv2) : 0);
            temp = Double.doubleToLongBits(dv1);
            result = 31 * result + (int) (temp ^ (temp >>> 32));
            temp = Double.doubleToLongBits(dv2);
            result = 31 * result + (int) (temp ^ (temp >>> 32));
            result = 31 * result + (bv1 ? 1 : 0);
            result = 31 * result + (bv2 ? 1 : 0);
            return result;
        }
    }


    private static class BeanDocCodec extends AbstractDsonCodec<Bean> {

        @Override
        @Nonnull
        public TypeInfo getEncoderType() {
            return TypeInfo.of(DefaultValueTest.Bean.class);
        }

        @Override
        protected DefaultValueTest.Bean newInstance(DsonObjectReader reader) {
            return new DefaultValueTest.Bean();
        }

        @Override
        public void readFields(DsonObjectReader reader, Bean inst) {
            inst.iv1 = reader.readInt("iv1");
            inst.iv2 = reader.readInt("iv2");
            inst.lv1 = reader.readLong("lv1");
            inst.lv2 = reader.readLong("lv2");
            inst.fv1 = reader.readFloat("fv1");
            inst.fv2 = reader.readFloat("fv2");
            inst.dv1 = reader.readDouble("dv1");
            inst.dv2 = reader.readDouble("dv2");
            inst.bv1 = reader.readBoolean("bv1");
            inst.bv2 = reader.readBoolean("bv2");
        }

        @Override
        public void writeFields(DsonObjectWriter writer, Bean inst) {
            writer.writeInt("iv1", inst.iv1, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeInt("iv2", inst.iv2, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeLong("lv1", inst.lv1, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeLong("lv2", inst.lv2, WireType.VARINT, NumberStyle.SIMPLE);
            writer.writeFloat("fv1", inst.fv1, NumberStyle.SIMPLE);
            writer.writeFloat("fv2", inst.fv2, NumberStyle.SIMPLE);
            writer.writeDouble("dv1", inst.dv1, NumberStyle.SIMPLE);
            writer.writeDouble("dv2", inst.dv2, NumberStyle.SIMPLE);
            writer.writeBoolean("bv1", inst.bv1);
            writer.writeBoolean("bv2", inst.bv2);
        }

    }

}