/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree.branch;

import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskInlinable;

/**
 * 展开的switch
 * 在编辑器中，children根据坐标排序，容易变动；这里将其展开为字段，从而方便配置。
 * （这个类不是必须的，因为我们可以仅提供编辑器数据结构，在导出时转为Switch）
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class FixedSwitch<T> extends Switch<T> {

    private Task<T> branch1;
    private Task<T> branch2;
    private Task<T> branch3;
    private Task<T> branch4;
    private Task<T> branch5;

    public FixedSwitch() {
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (children.isEmpty()) {
            addChildIfNotNull(branch1);
            addChildIfNotNull(branch2);
            addChildIfNotNull(branch3);
            addChildIfNotNull(branch4);
            addChildIfNotNull(branch5);
        }
    }

    private void addChildIfNotNull(Task<T> branch) {
        if (branch != null) {
            addChild(branch);
        }
    }

    //

    public Task<T> getBranch1() {
        return branch1;
    }

    public void setBranch1(Task<T> branch1) {
        this.branch1 = branch1;
    }

    public Task<T> getBranch2() {
        return branch2;
    }

    public void setBranch2(Task<T> branch2) {
        this.branch2 = branch2;
    }

    public Task<T> getBranch3() {
        return branch3;
    }

    public void setBranch3(Task<T> branch3) {
        this.branch3 = branch3;
    }

    public Task<T> getBranch4() {
        return branch4;
    }

    public void setBranch4(Task<T> branch4) {
        this.branch4 = branch4;
    }

    public Task<T> getBranch5() {
        return branch5;
    }

    public void setBranch5(Task<T> branch5) {
        this.branch5 = branch5;
    }
}