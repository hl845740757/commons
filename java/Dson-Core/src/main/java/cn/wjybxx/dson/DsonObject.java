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

/**
 * @author wjybxx
 * date - 2023/4/21
 */
public class DsonObject<K> extends AbstractDsonObject<K> {

    private final DsonHeader<K> header = new DsonHeader<>();

    public DsonObject() {
        super(0);
    }

    public DsonObject(int expectedSize) {
        super(expectedSize);
    }

    public DsonObject(DsonObject<K> src) { // 需要拷贝
        super(new LinkedHashMap<>(src.valueMap));
        header.putAll(src.header);
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