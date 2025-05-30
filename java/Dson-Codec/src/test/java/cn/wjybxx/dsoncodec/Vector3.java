/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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
import cn.wjybxx.dson.text.ObjectStyle;

import javax.annotation.Nonnull;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2025/5/30
 */
public class Vector3 {

    private float x;
    private float y;
    private float z;

    public Vector3() {
    }

    public Vector3(float x, float y, float z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float getX() {
        return x;
    }

    public Vector3 setX(float x) {
        this.x = x;
        return this;
    }

    public float getY() {
        return y;
    }

    public Vector3 setY(float y) {
        this.y = y;
        return this;
    }

    public float getZ() {
        return z;
    }

    public Vector3 setZ(float z) {
        this.z = z;
        return this;
    }


    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Vector3 vector3 = (Vector3) o;
        return Float.compare(x, vector3.x) == 0 && Float.compare(y, vector3.y) == 0 && Float.compare(z, vector3.z) == 0;
    }

    @Override
    public int hashCode() {
        int result = Float.hashCode(x);
        result = 31 * result + Float.hashCode(y);
        result = 31 * result + Float.hashCode(z);
        return result;
    }

    @Override
    public String toString() {
        return "Vector3{" +
                "x=" + x +
                ", y=" + y +
                ", z=" + z +
                '}';
    }

    public static class Vector3Codec implements DsonCodec<Vector3> {

        @Nonnull
        @Override
        public TypeInfo getEncoderType() {
            return TypeInfo.of(Vector3.class);
        }

        @Override
        public void writeObject(DsonObjectWriter writer, Vector3 inst, TypeInfo declaredType, ObjectStyle style) {
            writer.writeFloat("x", inst.x);
            writer.writeFloat("y", inst.y);
            writer.writeFloat("z", inst.z);
        }

        @Override
        public Vector3 readObject(DsonObjectReader reader, TypeInfo declaredType, Supplier<? extends Vector3> factory) {
            Vector3 vector3 = new Vector3();
            vector3.x = reader.readFloat("x");
            vector3.y = reader.readFloat("y");
            vector3.z = reader.readFloat("z");
            return vector3;
        }
    }
}