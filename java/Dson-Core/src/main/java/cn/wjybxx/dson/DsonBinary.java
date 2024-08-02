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

package cn.wjybxx.dson;

import cn.wjybxx.dson.types.Binary;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * 你通常不应该修改data中的数据。
 * 该类难以实现不可变对象，虽然我们可以封装为ByteArray，
 * 但许多接口都是基于byte[]的，封装会导致难以与其它接口协作。
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonBinary extends DsonValue {

    private final Binary binary;

    public DsonBinary(Binary binary) {
        this.binary = Objects.requireNonNull(binary);
    }

    public Binary binary() {
        return binary;
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.BINARY;
    }

    /** 创建一个拷贝 */
    public DsonBinary deepCopy() {
        return new DsonBinary(binary.deepCopy());
    }

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonBinary that = (DsonBinary) o;
        return binary.equals(that.binary);
    }

    @Override
    public int hashCode() {
        return binary.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonBinary{" +
                "binary=" + binary +
                '}';
    }
}