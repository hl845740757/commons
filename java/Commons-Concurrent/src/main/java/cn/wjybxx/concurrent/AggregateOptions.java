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

package cn.wjybxx.concurrent;

import cn.wjybxx.base.annotation.Internal;

/**
 * future的聚合选项
 *
 * @author wjybxx
 * date 2023/4/12
 */
@Internal
public final class AggregateOptions {

    private final byte type;
    public final int successRequire;
    public final boolean failFast;

    AggregateOptions(byte type, int successRequire, boolean failFast) {
        this.type = type;
        this.successRequire = successRequire;
        this.failFast = failFast;
    }

    public boolean isAnyOf() {
        return type == TYPE_ANY;
    }

    public boolean isSelectAll() {
        return type == TYPE_SELECT_ALL;
    }

    public boolean isSelectMany() {
        return type == TYPE_SELECT_MANY;
    }

    private static final byte TYPE_ANY = 0;
    private static final byte TYPE_SELECT_ALL = 1;
    private static final byte TYPE_SELECT_MANY = 2;

    private static final AggregateOptions ANY = new AggregateOptions(TYPE_ANY, 0, false);
    private static final AggregateOptions SELECT_ALL = new AggregateOptions(TYPE_SELECT_ALL, 0, false);
    private static final AggregateOptions SELECT_ALL2 = new AggregateOptions(TYPE_SELECT_ALL, 0, true);

    /** 任意一个完成 */
    public static AggregateOptions anyOf() {
        return ANY;
    }

    /** 所有任务成功 */
    public static AggregateOptions selectAll(boolean failFast) {
        return failFast ? SELECT_ALL2 : SELECT_ALL;
    }

    /**
     * 成功完成n个
     *
     * @param futureCount    future数量
     * @param successRequire 需要成功完成的数量
     * @param failFast       是否快速失败
     */
    public static AggregateOptions selectN(int futureCount, int successRequire, boolean failFast) {
        if (futureCount < 0 || successRequire < 0) {
            throw new IllegalArgumentException();
        }
        return new AggregateOptions(TYPE_SELECT_MANY, successRequire, failFast);
    }
}