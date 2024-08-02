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

/**
 * @author wjybxx
 * date 2023/4/4
 */
public enum DsonContextType {

    /** 顶层上下文（类数组结构） */
    TOP_LEVEL(null, null),

    /** 当前是一个普通对象结构 */
    OBJECT("{", "}"),

    /** 当前是一个数组结构 */
    ARRAY("[", "]"),

    /** 当前是一个Header结构 - 类似Object */
    HEADER("@{", "}");

    public final String startSymbol;
    public final String endSymbol;

    DsonContextType(String startSymbol, String endSymbol) {
        this.startSymbol = startSymbol;
        this.endSymbol = endSymbol;
    }

    public boolean isContainer() {
        return this == OBJECT || this == ARRAY;
    }

    public boolean isArrayLike() {
        return this == ARRAY || this == TOP_LEVEL;
    }

    public boolean isObjectLike() {
        return this == OBJECT || this == HEADER;
    }

}