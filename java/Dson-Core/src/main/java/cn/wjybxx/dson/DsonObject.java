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

import javax.annotation.Nonnull;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonObject<K> extends AbstractDsonObject<K> {

    private final DsonHeader<K> header;

    public DsonObject() {
        this(new LinkedHashMap<>(8), new DsonHeader<>());
    }

    public DsonObject(int expectedSize) {
        this(new LinkedHashMap<>(expectedSize), new DsonHeader<>());
    }

    public DsonObject(DsonObject<K> src) { // 需要拷贝
        this(new LinkedHashMap<>(src.valueMap), new DsonHeader<>(src.getHeader()));
    }

    private DsonObject(Map<K, DsonValue> valueMap, DsonHeader<K> header) {
        super(valueMap);
        this.header = Objects.requireNonNull(header);
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.OBJECT;
    }

    public DsonHeader<K> getHeader() {
        return header;
    }

    /** @return this */
    @Override
    public DsonObject<K> append(K key, DsonValue value) {
        put(key, value);
        return this;
    }

    @Override
    public String toString() {
        return "DsonObject{" +
                "header=" + header +
                ", valueMap=" + valueMap +
                '}';
    }
}