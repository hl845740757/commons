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
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.RandomAccess;

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public class DsonArray<K> extends AbstractDsonArray implements RandomAccess {

    private final DsonHeader<K> header;

    public DsonArray() {
        this(new ArrayList<>(), new DsonHeader<>());
    }

    public DsonArray(int initCapacity) {
        this(new ArrayList<>(initCapacity), new DsonHeader<>());
    }

    public DsonArray(DsonArray<K> src) { // 需要拷贝
        this(new ArrayList<>(src.values), new DsonHeader<>(src.getHeader()));
    }

    private DsonArray(List<DsonValue> values, DsonHeader<K> header) {
        super(values);
        this.header = Objects.requireNonNull(header);
    }

    @Nonnull
    @Override
    public final DsonType getDsonType() {
        return DsonType.ARRAY;
    }

    @Nonnull
    public DsonHeader<K> getHeader() {
        return header;
    }

    @Override
    public DsonArray<K> append(DsonValue dsonValue) {
        add(dsonValue);
        return this;
    }

    /**
     * 注意：对切片进行的修改是独立的，不影响原始的数据
     */
    public DsonArray<K> slice(int skip) {
        if (skip < 0) throw new IllegalArgumentException("skip cant be negative");
        if (skip >= values.size()) {
            return new DsonArray<>(0);
        }
        List<DsonValue> dsonValues = new ArrayList<>(values.subList(skip, values.size()));
        return new DsonArray<>(dsonValues, new DsonHeader<>());
    }

    public DsonArray<K> slice(int skip, int count) {
        if (skip < 0) throw new IllegalArgumentException("skip cant be negative");
        if (skip >= values.size()) {
            return new DsonArray<>(0);
        }
        int endIndex = Math.min(values.size(), skip + count);
        List<DsonValue> dsonValues = new ArrayList<>(values.subList(skip, endIndex));
        return new DsonArray<>(dsonValues, new DsonHeader<>());
    }

    @Override
    public String toString() {
        return "DsonArray{" +
                "header=" + header +
                ", values=" + values +
                '}';
    }
}