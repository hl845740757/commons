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

package cn.wjybxx.base;

/**
 * 排序类型枚举
 *
 * @author wjybxx
 * date - 2024/1/6
 */
public enum SortOrder implements EnumLite {

    /** 无序 */
    NONE(0),

    /** 升序 */
    ASCENDING(1),

    /** 降序 -- -1对序列化不是很友好，但-1表意清楚 */
    DESCENDING(-1);

    public final int number;

    SortOrder(int number) {
        this.number = number;
    }

    @Override
    public int getNumber() {
        return number;
    }

    public static SortOrder forNumber(int number) {
        return switch (number) {
            case 0 -> NONE;
            case 1 -> ASCENDING;
            case -1 -> DESCENDING;
            default -> throw new IllegalArgumentException("number: " + number);
        };
    }
}