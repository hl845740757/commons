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

/**
 * @author wjybxx
 * date - 2023/4/19
 */
public final class DsonNull extends DsonValue implements Comparable<DsonNull> {

    public static final DsonNull NULL = new DsonNull();
    /** 用于不存在对应key时的返回值，用于特殊情况下的测试 */
    public static final DsonNull UNDEFINE = new DsonNull();

    private DsonNull() {
    }

    @Nonnull
    @Override
    public DsonType getDsonType() {
        return DsonType.NULL;
    }

    //region equals
    @Override
    public boolean equals(final Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        return true;
    }

    @Override
    public int hashCode() {
        return 0;
    }

    @Override
    public int compareTo(DsonNull o) {
        return 0;
    }
    // endregion

    @Override
    public String toString() {
        return "DsonNull{}";
    }

}