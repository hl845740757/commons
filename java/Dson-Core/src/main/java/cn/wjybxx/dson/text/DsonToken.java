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

package cn.wjybxx.dson.text;

import java.util.Arrays;
import java.util.Objects;

/**
 * token可能记录位置更有助于排查问题
 *
 * @author wjybxx
 * date - 2023/6/2
 */
public class DsonToken {

    public final DsonTokenType type;
    public final Object value;
    public final int pos;

    /**
     * @param pos token所在的位置，-1表示动态生成的token
     */
    public DsonToken(DsonTokenType type, Object value, int pos) {
        this.type = Objects.requireNonNull(type);
        this.value = value;
        this.pos = pos;
    }

    /** 将value转换为字符串值 */
    public String stringValue() {
        return (String) value;
    }

    // region equals
    public boolean fullEquals(DsonToken dsonToken) {
        if (this == dsonToken) {
            return true;
        }
        if (pos != dsonToken.pos) return false;
        if (type != dsonToken.type) return false;
        return Objects.equals(value, dsonToken.value);
    }

    /**
     * 默认忽略pos的差异 -- 通常是由于换行符的问题。
     * 由于动态生成的token的pos都是-1，因此即使比较pos，对于动态生成的token之间也是无意义的，
     * 既然动态生成的token之间的相等性是忽略了pos的，那么正常的token也需要忽略pos。
     */
    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        DsonToken dsonToken = (DsonToken) o;

        if (type != dsonToken.type) return false;
        // value可能是字节数组...需要处理以保证测试用例通过
        if (type == DsonTokenType.BINARY) {
            byte[] src = (byte[]) value;
            byte[] dest = (byte[]) dsonToken.value;
            return Arrays.equals(src, dest);
        }
        return Objects.equals(value, dsonToken.value);
    }

    @Override
    public int hashCode() {
        // 不处理字节数组hash，是因为我们并不会将Token放入Set
        int result = type.hashCode();
        result = 31 * result + (value != null ? value.hashCode() : 0);
        return result;
    }

    // endregion

    @Override
    public String toString() {
        return "DsonToken{" +
                "type=" + type +
                ", value=" + value +
                ", pos=" + pos +
                '}';
    }
}