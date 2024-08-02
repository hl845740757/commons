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

import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/5/27
 */
public final class DsonElement<K> implements Map.Entry<K, DsonValue> {

    private final K name;
    private final DsonValue value;

    public DsonElement(K name, DsonValue value) {
        this.name = Objects.requireNonNull(name);
        this.value = Objects.requireNonNull(value);
    }

    public K getName() {
        return name;
    }

    @Override
    public K getKey() {
        return name;
    }

    public DsonValue getValue() {
        return value;
    }

    @Override
    public DsonValue setValue(DsonValue value) {
        throw new UnsupportedOperationException("setValue");
    }

    //

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonElement<?> that = (DsonElement<?>) o;

        if (!name.equals(that.name)) return false;
        return value.equals(that.value);
    }

    @Override
    public int hashCode() {
        int result = name.hashCode();
        result = 31 * result + value.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "DsonElement{" +
                "name=" + name +
                ", value=" + value +
                '}';
    }
}