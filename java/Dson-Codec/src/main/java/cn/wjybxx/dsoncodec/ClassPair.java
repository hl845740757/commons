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

import java.util.Objects;

/**
 * 主要用于各种缓存
 *
 * @author wjybxx
 * date - 2024/10/15
 */
public final class ClassPair {

    public final Class<?> first;
    public final Class<?> second;

    public ClassPair(Class<?> first, Class<?> second) {
        this.first = Objects.requireNonNull(first);
        this.second = Objects.requireNonNull(second);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ClassPair cacheKey = (ClassPair) o;
        return first == cacheKey.first // class 使用 ==
                && second == cacheKey.second;
    }

    @Override
    public int hashCode() {
        return 31 * first.hashCode() + second.hashCode();
    }

    @Override
    public String toString() {
        return "ClassPair{" +
                "first=" + first +
                ", second=" + second +
                '}';
    }
}
