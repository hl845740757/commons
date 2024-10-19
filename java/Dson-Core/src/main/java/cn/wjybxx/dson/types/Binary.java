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

package cn.wjybxx.dson.types;

import java.util.Arrays;
import java.util.HexFormat;

/**
 * 你通常不应该修改data中的数据。
 * 该类难以实现不可变对象，虽然我们可以封装为ByteArray，
 * 但许多接口都是基于byte[]的，封装会导致难以与其它接口协作。
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public final class Binary {

    private final byte[] data;
    private int hash;

    private Binary(byte[] data) {
        this.data = data;
    }

    /** 创建一个拷贝 */
    public Binary deepCopy() {
        return new Binary(data.clone());
    }

    public byte byteAt(int index) {
        return data[index];
    }

    public int length() {
        return data.length;
    }

    public boolean isEmpty() {
        return data.length == 0;
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Binary that = (Binary) o;
        return Arrays.equals(data, that.data);
    }

    @Override
    public int hashCode() {
        int r = this.hash;
        if (r == 0) {
            r = Arrays.hashCode(data);
            this.hash = r;
        }
        return r;
    }

    // endregion

    @Override
    public String toString() {
        return "Binary{" +
                "data=" + HexFormat.of().formatHex(data) +
                '}';
    }

    // region

    /** 转换为字节数组 */
    public byte[] toByteArray() {
        return data.clone();
    }

    /** 转换为16进制字符串 */
    public String toHexString() {
        return HexFormat.of().formatHex(data);
    }

    /** 慎重使用该方法，可能打破不可变约束 */
    public byte[] unsafeBuffer() {
        return data;
    }

    /** 慎重使用该方法，可能打破不可变约束 */
    public static Binary unsafeWrap(byte[] bytes) {
        return new Binary(bytes);
    }

    public static Binary copyFrom(byte[] bytes) {
        return copyFrom(bytes, 0, bytes.length);
    }

    public static Binary copyFrom(byte[] src, int offset, int size) {
        byte[] copy = new byte[size];
        System.arraycopy(src, offset, copy, 0, size);
        return new Binary(copy);
    }

    public void copyTo(byte[] target, int offset) {
        System.arraycopy(data, 0, target, offset, data.length);
    }

    public void copyTo(int selfOffset, byte[] target, int offset, int size) {
        System.arraycopy(data, selfOffset, target, offset, size);
    }
    // endregion
}