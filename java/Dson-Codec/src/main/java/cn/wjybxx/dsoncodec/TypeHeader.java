/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

/**
 * @author wjybxx
 * date - 2025/7/2
 */
final class TypeHeader {

    public static final TypeHeader EMPTY = new TypeHeader(null, 0);

    /**
     * 对象头中的clsName，如果没有则为null
     */
    public final String clsName;
    /**
     * 对象头中的count，如果没有则为0 -- 方便直接初始化。
     */
    public final int count;

    public TypeHeader(String clsName, int count) {
        this.clsName = clsName;
        this.count = count;
    }

    @Override
    public String toString() {
        return "TypeHeader{" +
                "clsName='" + clsName + '\'' +
                ", count=" + count +
                '}';
    }
}