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

package cn.wjybxx.btree;

/**
 * @author wjybxx
 * date - 2025/12/6
 */
public class TimingTaskEntry<T> extends TaskEntry<T> {

    public int frameCount;
    public float time;
    public float deltaTime;

    public TimingTaskEntry() {
    }

    public TimingTaskEntry(String name, Task<T> rootTask, T blackboard) {
        super(name, rootTask, blackboard);
    }

    public TimingTaskEntry(String name, Task<T> rootTask, T blackboard, Object hostObject, ITreeLoader treeLoader) {
        super(name, rootTask, blackboard, hostObject, treeLoader);
    }
}