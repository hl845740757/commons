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
import cn.wjybxx.base.EnumLiteMap;
import cn.wjybxx.base.EnumUtils;
import cn.wjybxx.base.annotation.StableName;

/**
 * 组件的状态
 *
 * @author wjybxx
 * date - 2024/6/22
 */
public enum ComponentStatus implements EnumLite {

    /**
     * 刚刚创建，尚未添加到实体
     */
    NEW(0),
    /**
     * 已添加到实体，等待启动；非脚本组件会直接进入{@link #STOPPED}的状态
     */
    READY(1),

    /**
     * 正在启动中，脚本组件在调用Start方法前进入该状态
     */
    STARTING(2),
    /**
     * 运行状态，脚本组件在调用Start成功后会进入该状态。
     */
    RUNNING(3),

    /**
     * 停止中，脚本组件在调用Stop方法前进入该状态
     */
    STOPPING(4),
    /**
     * 成功停止，非脚本组件会直接到该状态，脚本组件在调用Stop方法后会进入该状态
     */
    STOPPED(5),

    /**
     * 已从实体上删除
     */
    DESTROYED(6);

    private final int number;

    ComponentStatus(int number) {
        this.number = number;
    }

    private static final EnumLiteMap<ComponentStatus> MAPPER = EnumUtils.mapping(values());

    /** 是否尚未启动 */
    public boolean isUnstarted() {
        return number < 2;
    }

    /** 是否停止中，已停止也返回true */
    public boolean isStopping() {
        return number >= 4;
    }

    /** 是否已经停止，已销毁也返回true */
    public boolean isStopped() {
        return number >= 5;
    }

    /** 是否已经停止，已销毁也返回true */
    public boolean isDestroyed() {
        return number == 6;
    }

    @Override
    public int getNumber() {
        return number;
    }

    @StableName
    public static ComponentStatus forNumber(int number) {
        return MAPPER.checkedForNumber(number);
    }
}