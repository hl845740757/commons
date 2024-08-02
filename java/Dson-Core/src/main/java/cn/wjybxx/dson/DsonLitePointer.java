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

import cn.wjybxx.dson.types.ObjectLitePtr;

import javax.annotation.Nonnull;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2024/4/26
 */
public class DsonLitePointer extends DsonValue {

    private final ObjectLitePtr value;

    public DsonLitePointer(ObjectLitePtr value) {
        this.value = Objects.requireNonNull(value);
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.LITE_POINTER;
    }

    public ObjectLitePtr getValue() {
        return value;
    }

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonLitePointer that = (DsonLitePointer) o;

        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        return value.hashCode();
    }

    // endregion

    @Override
    public String toString() {
        return "DsonLitePointer{" +
                "value=" + value +
                '}';
    }
}
