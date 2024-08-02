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

    private final boolean anyOf;
    public final int successRequire;
    public final boolean failFast;

    AggregateOptions(boolean anyOf, int successRequire, boolean failFast) {
        this.anyOf = anyOf;
        this.successRequire = successRequire;
        this.failFast = failFast;
    }

    public boolean isAnyOf() {
        return anyOf;
    }

    private static final AggregateOptions ANY = new AggregateOptions(true, 0, false);

    /** 任意一个完成 */
    public static AggregateOptions anyOf() {
        return ANY;
    }

    /**
     * 成功完成n个
     *
     * @param successRequire 需要成功完成的数量
     * @param failFast       是否快速失败
     */
    public static AggregateOptions selectN(int successRequire, boolean failFast) {
        if (successRequire < 0) {
            throw new IllegalArgumentException("successRequire < 0");
        }
        return new AggregateOptions(false, successRequire, failFast);
    }
}