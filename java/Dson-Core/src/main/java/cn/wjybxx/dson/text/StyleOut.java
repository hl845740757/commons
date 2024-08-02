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

/**
 * @author wjybxx
 * date - 2023/7/15
 */
public class StyleOut {

    private String value;
    private boolean typed;

    public StyleOut() {
    }

    /**
     * @param value 输出结果
     * @param typed 是否需要打印类型
     */
    public StyleOut(String value, boolean typed) {
        this.value = value;
        this.typed = typed;
    }

    public StyleOut reset() {
        value = null;
        typed = false;
        return this;
    }

    //

    public String getValue() {
        return value;
    }

    public StyleOut setValue(String value) {
        this.value = value;
        return this;
    }

    public boolean isTyped() {
        return typed;
    }

    public StyleOut setTyped(boolean typed) {
        this.typed = typed;
        return this;
    }

    @Override
    public String toString() {
        return "StyleOut{" +
                "value='" + value + '\'' +
                ", typed=" + typed +
                '}';
    }
}