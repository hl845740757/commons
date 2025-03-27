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

package cn.wjybxx.base.fx;

import cn.wjybxx.base.EnumLite;
import cn.wjybxx.base.annotation.StableName;

/**
 * 组件类型
 *
 * @author wjybxx
 * date - 2024/6/22
 */
public enum ComponentKind implements EnumLite {

    /** 数据组件 */
    DATA,

    /** 普通行为组件；即使有可以被框架调度的方法，也不会被调度 */
    BEHAVIOR,

    /** 脚本行为组件；需要被框架调度的组件；具体接口取决于具体业务 */
    SCRIPT;

    @Override
    public int getNumber() {
        return switch (this) {
            case DATA -> 0;
            case BEHAVIOR -> 1;
            case SCRIPT -> 2;
        };
    }

    @StableName(comment = "生成代码调用")
    public static ComponentKind forNumber(int number) {
        return switch (number) {
            case 0 -> DATA;
            case 1 -> BEHAVIOR;
            case 2 -> SCRIPT;
            default -> throw new IllegalArgumentException("invalid number: " + number);
        };
    }
}