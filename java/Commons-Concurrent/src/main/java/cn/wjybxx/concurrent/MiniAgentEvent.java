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

/**
 * 提供最小支持的AgentEvent
 *
 * @author wjybxx
 * date - 2024/1/22
 */
public final class MiniAgentEvent implements IAgentEvent {

    private int type = TYPE_INVALID;
    private Object obj1;
    private Object obj2;
    private int options;

    @Override
    public void clean() {
        type = TYPE_INVALID;
        obj1 = null;
        obj2 = null;
        options = 0;
    }

    @Override
    public void cleanAll() {
        type = TYPE_INVALID;
        obj1 = null;
        obj2 = null;
        options = 0;
    }

    @Override
    public int getType() {
        return type;
    }

    @Override
    public void setType(int type) {
        this.type = type;
    }

    @Override
    public Object getObj2() {
        return obj2;
    }

    @Override
    public void setObj2(Object obj2) {
        this.obj2 = obj2;
    }

    @Override
    public Object getObj1() {
        return obj1;
    }

    @Override
    public void setObj1(Object obj1) {
        this.obj1 = obj1;
    }

    @Override
    public int getOptions() {
        return options;
    }

    @Override
    public void setOptions(int options) {
        this.options = options;
    }

}
