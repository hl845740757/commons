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

package cn.wjybxx.btree.fsm;

import cn.wjybxx.btree.Task;

/**
 * Fsm中的状态配置，运行时不可以修改。
 * 注意：切换状态前记得将{@link #props}赋值到{@link #task}
 *
 * @author wjybxx
 * date - 2025/3/16
 */
public final class FsmStateCfg<T> {

    /** 状态的名字 */
    private String name;
    /** 状态的task的guid */
    private String guid;
    /** 状态关联的属性(输入) */
    private Object props;
    /** 状态的task缓存 */
    private transient Task<T> task;

    public String getName() {
        return name;
    }

    public FsmStateCfg<T> setName(String name) {
        this.name = name;
        return this;
    }

    public String getGuid() {
        return guid;
    }

    public FsmStateCfg<T> setGuid(String guid) {
        this.guid = guid;
        return this;
    }

    public Object getProps() {
        return props;
    }

    public FsmStateCfg<T> setProps(Object props) {
        this.props = props;
        return this;
    }

    public Task<T> getTask() {
        return task;
    }

    public FsmStateCfg<T> setTask(Task<T> task) {
        this.task = task;
        return this;
    }
}